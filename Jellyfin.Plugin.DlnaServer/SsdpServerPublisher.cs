using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.DlnaServer.Configuration;
using Jellyfin.Plugin.Ssdp;
using Jellyfin.Plugin.Ssdp.Configuration;
using Jellyfin.Plugin.Ssdp.Devices;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Plugins;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.DlnaServer
{
    /// <summary>
    /// Provides the platform independent logic for publishing SSDP devices (notifications and search responses).
    /// </summary>
    public class SsdpServerPublisher : IDisposable
    {
        private const string UpnpRootDevice = "upnp:rootdevice";
        private const string SsdpNotify = "NOTIFY * HTTP/1.1";
        private readonly ILogger _logger;
        private readonly IList<SsdpRootDevice> _devices;
        private readonly IReadOnlyList<SsdpRootDevice> _readOnlyDevices;
        private readonly INetworkManager _networkManager;
        private readonly Random _random;
        private readonly IDictionary<string, SearchRequest> _recentSearchRequests;
        private Timer? _rebroadcastAliveNotificationsTimer;
        private bool _disposed;
        private int _aliveMessageInterval = 1800;
        private string? _previousSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpServerPublisher"/> class.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfigurationManager"/> instance.</param>
        /// <param name="logger">The <see cref="ILogger"/> instance.</param>
        /// <param name="networkManager">The <see cref="INetworkManager"/> instance.</param>
        public SsdpServerPublisher(
            IConfigurationManager configuration,
            ILogger logger,
            INetworkManager networkManager)
        {
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _logger = logger;
            _devices = new List<SsdpRootDevice>();
            _readOnlyDevices = new ReadOnlyCollection<SsdpRootDevice>(_devices);
            _recentSearchRequests = new Dictionary<string, SearchRequest>(StringComparer.OrdinalIgnoreCase);
            _random = new Random();

            var config = DlnaServerPlugin.Instance!.Configuration;

            Server = SsdpServer.GetOrCreateInstance(
                configuration,
                _logger,
                networkManager.GetInternalBindAddresses(),
                networkManager);

            DlnaServerPlugin.Instance!.ConfigurationChanged += UpdateConfiguration;
            _networkManager.NetworkChanged += NetworkChanged;
        }

        /// <summary>
        /// Gets the SSDP server instance.
        /// </summary>
        public ISsdpServer Server { get; }

        /// <summary>
        /// Gets a read only list of devices being published by this instance.
        /// </summary>
        private IEnumerable<SsdpRootDevice> Devices
        {
            get
            {
                return _readOnlyDevices;
            }
        }

        /// <summary>
        /// Adds a device (and it's children) to the list of devices being published by this server,
        /// making them discoverable to SSDP clients.
        /// </summary>
        /// <param name="device">The <see cref="SsdpDevice"/> instance to add.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task AddDevice(SsdpRootDevice device)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            ThrowIfDisposed();

            // Must be first, to ensure there will be sockets available.
            Server.AddEvent("M-SEARCH", RequestReceived);

            bool wasAdded = false;
            lock (_devices)
            {
                if (!_devices.Contains(device))
                {
                    _devices.Add(device);
                    wasAdded = true;
                }
            }

            if (wasAdded)
            {
                _logger.LogInformation("DLNA server added {Device}", device);
                await SendAliveNotifications(device, true).ConfigureAwait(false);
                StartBroadcastingAliveMessages(_aliveMessageInterval);
            }
        }

        /// <summary>
        /// Disposes this object instance and all internally managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Override this method and dispose any objects you own the lifetime of if disposing is true.
        /// </summary>
        /// <param name="disposing">True if managed objects should be disposed, if false, only unmanaged resources should be released.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _disposed = true;
                _logger.LogDebug("Disposing instance.");

                _rebroadcastAliveNotificationsTimer?.Dispose();
                _rebroadcastAliveNotificationsTimer = null;

                var tasks = Devices.ToList().Select(RemoveDevice).ToArray();
                Task.WaitAll(tasks);

                // Must be last, or there won't be any sockets available.
                Server.DeleteEvent("M-SEARCH", RequestReceived);

                _networkManager.NetworkChanged -= NetworkChanged;
                DlnaServerPlugin.Instance!.ConfigurationChanged -= UpdateConfiguration;
            }

            _disposed = true;
        }

        private static string GetSettings()
        {
            var config = DlnaServerPlugin.Instance!.Configuration;
            return config.EnableSsdpTracing.ToString()
                + config.SsdpTracingFilter
                + config.UdpPortRange
                + config.EnableSsdpTracing.ToString();
        }

        private static string GetUsn(string udn, string fullDeviceType)
        {
            return $"{udn}::{fullDeviceType}";
        }

        /// <summary>
        /// Recursive SelectMany - modified from.
        /// https://stackoverflow.com/questions/13409194/is-it-possible-to-implement-a-recursive-selectmany.
        /// </summary>
        /// <typeparam name="T">Type to enumerate.</typeparam>
        /// <param name="collection">Collection to enumerate.</param>
        /// <returns>A flattened representation.</returns>
        private static IEnumerable<T> Flatten<T>(IEnumerable? collection)
        {
            if (collection == null)
            {
                yield return (T)Enumerable.Empty<T>();
            }
            else
            {
                foreach (var o in collection)
                {
                    if (o is IEnumerable enumerable and not T)
                    {
                        foreach (T t in Flatten<T>(enumerable))
                        {
                            yield return t;
                        }
                    }
                    else
                    {
                        yield return (T)o;
                    }
                }
            }
        }

        /// <summary>
        /// Removes a device (and it's children) from the list of devices being published by this server, making them undiscoverable.
        /// </summary>
        /// <param name="device">The <see cref="SsdpDevice"/> instance to add.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task RemoveDevice(SsdpRootDevice device)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            bool wasRemoved = false;
            lock (_devices)
            {
                if (_devices.Contains(device))
                {
                    _devices.Remove(device);
                    wasRemoved = true;
                }
            }

            if (wasRemoved)
            {
                _logger.LogInformation("Device {Device} removed.", device);

                await SendByeByeNotifications(device, true).ConfigureAwait(false);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Disposed");
            }
        }

        private void StartBroadcastingAliveMessages(int interval)
        {
            if (_rebroadcastAliveNotificationsTimer != null)
            {
                _rebroadcastAliveNotificationsTimer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(interval));
            }
            else
            {
                _rebroadcastAliveNotificationsTimer = new Timer(
                    state =>
                    {
                        SendAllAliveNotificationsAsync();
                    },
                    null,
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(interval));
            }
        }

        private async Task ProcessSearchRequestAsync(int maxWaitInterval, string searchTarget, SsdpEventArgs e)
        {
            if (maxWaitInterval > 120)
            {
                maxWaitInterval = _random.Next(0, 120);
            }

            // Do not block synchronously as that may tie up a thread pool thread for several seconds.
            _ = Task.Delay(_random.Next(16, maxWaitInterval * 1000));

            // Copying devices to local array here to avoid threading issues/enumerator exceptions.
            IEnumerable<SsdpDevice>? devices = null;
            lock (_devices)
            {
                if (string.Equals("ssdp:all", searchTarget, StringComparison.OrdinalIgnoreCase))
                {
                    devices = Flatten<SsdpDevice>(_devices).ToArray();
                }
                else if (string.Equals(UpnpRootDevice, searchTarget, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("pnp:rootdevice", searchTarget, StringComparison.OrdinalIgnoreCase))
                {
                    devices = _devices.ToArray();
                }
                else if (searchTarget.Trim().StartsWith("uuid:", StringComparison.OrdinalIgnoreCase))
                {
                    devices = (from device in Flatten<SsdpDevice>(_devices)
                               where string.Equals(device.Uuid, searchTarget[5..], StringComparison.OrdinalIgnoreCase)
                               select device).ToArray();
                }
                else if (searchTarget.StartsWith("urn:", StringComparison.OrdinalIgnoreCase))
                {
                    devices = (from device in Flatten<SsdpDevice>(_devices)
                               where string.Equals(device.FullDeviceType, searchTarget, StringComparison.OrdinalIgnoreCase)
                               select device).ToArray();
                }
            }

            var addr = e.ReceivedFrom.Address;

            if (Server.Tracing &&
                (Server.TracingFilter == null ||
                 Server.TracingFilter.Equals(addr) ||
                 Server.TracingFilter.Equals(e.LocalIpAddress)))
            {
                _logger.LogDebug("M-SEARCH: {Address} <- {Target}", addr, searchTarget);
            }

            if (devices != null)
            {
                var deviceList = devices.ToList();

                foreach (var device in deviceList)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    SsdpRootDevice rt = device.ToRootDevice();
                    if (rt.NetAddress.AddressFamily != e.ReceivedFrom.Address.AddressFamily)
                    {
                        continue;
                    }

                    bool isRootDevice = (device as SsdpRootDevice) != null;
                    if (isRootDevice)
                    {
                        await SendSearchResponseAsync(UpnpRootDevice, device, GetUsn(device.Udn, UpnpRootDevice), e).ConfigureAwait(false);
                    }

                    await SendSearchResponseAsync(device.Udn, device, device.Udn, e).ConfigureAwait(false);
                    await SendSearchResponseAsync(device.FullDeviceType, device, GetUsn(device.Udn, device.FullDeviceType), e).ConfigureAwait(false);
                }
            }
        }

        private async Task SendSearchResponseAsync(string searchTarget, SsdpDevice device, string uniqueServiceName, SsdpEventArgs e)
        {
            const string SsdpResponse = "HTTP/1.1 200 OK";

            var rootDevice = device.ToRootDevice();

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["EXT"] = string.Empty,
                ["DATE"] = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture),
                ["CACHE-CONTROL"] = "max-age = " + rootDevice.CacheLifetime.TotalSeconds,
                ["ST"] = searchTarget,
                ["USN"] = uniqueServiceName,
                ["LOCATION"] = rootDevice.Location.ToString(),
                ["SERVER"] = SsdpServer.HostName
            };

            SsdpServer.AddOptions(values);
            await Server.SendUnicastSsdp(values, SsdpResponse, e.LocalIpAddress, e.ReceivedFrom).ConfigureAwait(false);
        }

        private bool IsDuplicateSearchRequest(string searchTarget, EndPoint endPoint)
        {
            var isDuplicateRequest = false;

            var newRequest = new SearchRequest(endPoint, searchTarget, DateTime.UtcNow);
            lock (_recentSearchRequests)
            {
                if (_recentSearchRequests.ContainsKey(newRequest.Key))
                {
                    var lastRequest = _recentSearchRequests[newRequest.Key];
                    if (lastRequest.IsOld())
                    {
                        _recentSearchRequests[newRequest.Key] = newRequest;
                    }
                    else
                    {
                        isDuplicateRequest = true;
                    }
                }
                else
                {
                    _recentSearchRequests.Add(newRequest.Key, newRequest);
                    if (_recentSearchRequests.Count > 10)
                    {
                        CleanUpRecentSearchRequestsAsync();
                    }
                }
            }

            return isDuplicateRequest;
        }

        private void CleanUpRecentSearchRequestsAsync()
        {
            lock (_recentSearchRequests)
            {
                foreach (var requestKey in (from r in _recentSearchRequests where r.Value.IsOld() select r.Key).ToArray())
                {
                    _recentSearchRequests.Remove(requestKey);
                }
            }
        }

        /// <summary>
        /// Async timer callback that sends alive NOTIFY ssdp-all notifications.
        /// </summary>
        private async void SendAllAliveNotificationsAsync()
        {
            try
            {
                ThrowIfDisposed();

                // _logger.LogInformation("Begin sending alive notifications for all Devices");
                SsdpRootDevice[] devices;
                lock (_devices)
                {
                    devices = _devices.ToArray();
                }

                foreach (var device in devices)
                {
                    ThrowIfDisposed();
                    if (IPAddress.IsLoopback(device.Address))
                    {
                        // don't advertise loopbacks.
                        continue;
                    }

                    await SendAliveNotifications(device, true).ConfigureAwait(false);
                }

                // _logger.LogInformation("Completed transmitting alive notifications for all Devices");
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogError("Publisher stopped, exception {Message}.", ex.Message);
                Dispose();
            }
        }

        /// <summary>
        /// Advertises the device and associated services with a NOTIFY / ssdp-all.
        /// </summary>
        /// <param name="device">Device to advertise.</param>
        /// <param name="isRoot">True if this is a root device.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task SendAliveNotifications(SsdpDevice device, bool isRoot)
        {
            if (isRoot)
            {
                await SendAliveNotification(device, UpnpRootDevice, GetUsn(device.Udn, UpnpRootDevice)).ConfigureAwait(false);
                await SendAliveNotification(device, device.Udn, device.Udn).ConfigureAwait(false);
            }

            await SendAliveNotification(device, device.FullDeviceType, GetUsn(device.Udn, device.FullDeviceType)).ConfigureAwait(false);

            foreach (var childDevice in device.Devices)
            {
                await SendAliveNotifications(childDevice, false).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Advertises the device with a NOTIFY / ssdp-all. Used by SendAliveNotification.
        /// </summary>
        /// <param name="device">Device to advertise.</param>
        /// <param name="notificationType">Device type.</param>
        /// <param name="uniqueServiceName">USN.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private Task SendAliveNotification(SsdpDevice device, string notificationType, string uniqueServiceName)
        {
            var rootDevice = device.ToRootDevice();
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["DATE"] = DateTime.UtcNow.ToString("r", CultureInfo.CurrentCulture),
                ["CACHE-CONTROL"] = "max-age = " + rootDevice.CacheLifetime.TotalSeconds,
                ["LOCATION"] = rootDevice.Location.ToString(),
                ["NTS"] = "ssdp:alive",
                ["NT"] = notificationType,
                ["USN"] = uniqueServiceName,
                ["SERVER"] = SsdpServer.HostName
            };

            SsdpServer.AddOptions(values);
            return Server.SendMulticastSsdp(values, SsdpNotify, rootDevice.Address.AddressFamily);
        }

        /// <summary>
        /// Sends the ByeBye notifications.
        /// </summary>
        /// <param name="device">The device<see cref="SsdpDevice"/>.</param>
        /// <param name="isRoot">The isRoot<see cref="bool"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task SendByeByeNotifications(SsdpDevice device, bool isRoot)
        {
            if (isRoot)
            {
                await SendByeByeNotification(device, UpnpRootDevice, GetUsn(device.Udn, UpnpRootDevice)).ConfigureAwait(false);
                await SendByeByeNotification(device, device.Udn, device.Udn).ConfigureAwait(false);
            }

            await SendByeByeNotification(device, device.FullDeviceType, GetUsn(device.Udn, device.FullDeviceType)).ConfigureAwait(false);

            foreach (var childDevice in device.Devices)
            {
                await SendByeByeNotifications(childDevice, false).ConfigureAwait(false);
            }
        }

        private Task SendByeByeNotification(SsdpDevice device, string notificationType, string uniqueServiceName)
        {
            var addr = device.ToRootDevice().Address.AddressFamily;
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["DATE"] = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture),
                ["NTS"] = "ssdp:byebye",
                ["NT"] = notificationType,
                ["USN"] = uniqueServiceName,
                ["SERVER"] = SsdpServer.HostName
            };
            SsdpServer.AddOptions(values);
            return Server.SendMulticastSsdp(values, SsdpNotify, addr, _disposed ? 1 : Server.UdpSendCount);
        }

        /// <summary>
        /// Called when data is received.
        /// </summary>
        /// <param name="args">The <see cref="SsdpEventArgs"/>.</param>
        private async void RequestReceived(SsdpEventArgs args)
        {
            ThrowIfDisposed();

            if (!args.Message.TryGetValue("ST", out var searchTarget) || string.IsNullOrEmpty(searchTarget))
            {
                _logger.LogWarning("Invalid search request received from {Address}, Target is null/empty.", args.ReceivedFrom);
                return;
            }

            if (IsDuplicateSearchRequest(searchTarget, args.ReceivedFrom))
            {
                // Search Request is a duplicate, so ignore.
                return;
            }

            // Wait on random interval up to MX, as per SSDP spec.
            // Also, as per UPnP 1.1/SSDP spec ignore missing/bank MX header. If over 120, assume random value between 0 and 120.
            // Using 16 as minimum as that's often the minimum system clock frequency anyway.
            int maxWaitInterval = 1;
            if (args.Message.TryGetValue("MX", out var mx))
            {
                if (!int.TryParse(mx, out maxWaitInterval) || maxWaitInterval <= 0)
                {
                    return;
                }
            }

            await ProcessSearchRequestAsync(maxWaitInterval, searchTarget, args).ConfigureAwait(false);
        }

        private void UpdateConfiguration(object? sender, BasePluginConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var config = (DlnaServerConfiguration)configuration;

            if (!string.IsNullOrEmpty(config.DlnaServerName))
            {
                config.DlnaServerName = Regex.Replace(config.DlnaServerName, @"[^\u0000-\u007F]", string.Empty);
            }

            if (config.AliveMessageIntervalSeconds <= 0)
            {
                config.AliveMessageIntervalSeconds = 1800;
            }

            // Tracing may be set elsewhere, so we want to implement a last change wins.
            string settings = GetSettings();
            if (_previousSettings != settings)
            {
                config.SsdpTracingFilter = IPAddress.TryParse(config.SsdpTracingFilter, out var addr) ? addr.ToString() : string.Empty;
                if (config.EnableSsdpTracing)
                {
                    _logger.LogDebug("Setting SSDP tracing to : {Filter}", config.SsdpTracingFilter);
                }
                else
                {
                    _logger.LogDebug("SSDP Logging disabled.");
                }

                _previousSettings = settings;
            }

            if (_aliveMessageInterval != config.AliveMessageIntervalSeconds)
            {
                _aliveMessageInterval = config.AliveMessageIntervalSeconds;
                if (_rebroadcastAliveNotificationsTimer != null)
                {
                    StartBroadcastingAliveMessages(_aliveMessageInterval);
                }
            }

            Server.SaveConfiguration();
        }

        /// <summary>
        /// Triggered every time there is a network event.
        /// </summary>
        /// <param name="sender">NetworkManager instance.</param>
        /// <param name="e">Event argument.</param>
        private void NetworkChanged(object? sender, EventArgs e)
        {
            UpdateConfiguration(sender, DlnaServerPlugin.Instance!.Configuration);
        }

        /// <summary>
        /// Defines the <see cref="SearchRequest" />.
        /// </summary>
        private class SearchRequest
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SearchRequest"/> class.
            /// </summary>
            /// <param name="endPoint">The endPoint<see cref="EndPoint"/>.</param>
            /// <param name="searchTarget">The searchTarget<see cref="string"/>.</param>
            /// <param name="received">The received<see cref="DateTime"/>.</param>
            public SearchRequest(EndPoint endPoint, string searchTarget, DateTime received)
            {
                Key = searchTarget + ":" + endPoint;
                Received = received;
            }

            /// <summary>
            /// Gets the key.
            /// </summary>
            public string Key { get; }

            /// <summary>
            /// Gets the received date.
            /// </summary>
            private DateTime Received { get; }

            /// <summary>
            /// Return true if he entry is old.
            /// </summary>
            /// <returns>The <see cref="bool"/>.</returns>
            public bool IsOld()
            {
                return DateTime.UtcNow.Subtract(Received).TotalMilliseconds > 500;
            }
        }
    }
}
