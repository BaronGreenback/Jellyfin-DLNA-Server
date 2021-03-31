#pragma warning disable CA5394 // Do not use insecure randomness
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
using Jellyfin.Plugin.Dlna.EventArgs;
using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.Server.Configuration;
using Jellyfin.Plugin.Dlna.Ssdp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Plugins;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Server
{
    /// <summary>
    /// Provides the platform independent logic for publishing SSDP devices (notifications and search responses).
    /// Adapted from RSSDP.
    /// </summary>
    internal class SsdpServerPublisher : IDisposable
    {
        private const string PnpRootDevice = "pnp:rootdevice";
        private const string SsdpNotify = "NOTIFY * HTTP/1.1";
        private const string UpnpRootDevice = "upnp:rootdevice";
        private readonly IDictionary<string, SearchRequest> _recentSearchRequests;
        private readonly IList<SsdpRootDevice> _devices;
        private readonly ILogger _logger;
        private readonly INetworkManager _networkManager;
        private readonly IReadOnlyList<SsdpRootDevice> _readOnlyDevices;
        private readonly Random _random;
        private bool _disposed;
        private int _aliveMessageInterval;
        private Timer? _rebroadcastAliveNotificationsTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpServerPublisher"/> class.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfigurationManager"/> instance.</param>
        /// <param name="logger">The <see cref="ILogger"/> instance.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFacory"/> instance.</param>
        /// <param name="networkManager">The <see cref="INetworkManager"/> instance.</param>
        /// <param name="interfaces">The interfaces IP's to advertise on.</param>
        /// <param name="messageInterval">The discovery message interval.</param>
        /// <param name="enableWindowsSupport">True to enable windows support.</param>
        public SsdpServerPublisher(
            IConfigurationManager configuration,
            ILogger logger,
            ILoggerFactory loggerFactory,
            INetworkManager networkManager,
            IPNetAddress[] interfaces,
            int messageInterval,
            bool enableWindowsSupport)
        {
            _aliveMessageInterval = messageInterval;
            _devices = new List<SsdpRootDevice>();
            _logger = logger;
            _networkManager = networkManager;
            _random = new Random();
            _readOnlyDevices = new ReadOnlyCollection<SsdpRootDevice>(_devices);
            _recentSearchRequests = new Dictionary<string, SearchRequest>(StringComparer.OrdinalIgnoreCase);

            Server = SsdpServer.GetOrCreateInstance(
                configuration,
                loggerFactory,
                interfaces,
                networkManager);

            DlnaServerPlugin.Instance!.ConfigurationChanging += UpdateConfiguration;
            _networkManager.NetworkChanged += NetworkChanged;
            EnableWindowsExplorerSupport = enableWindowsSupport;

            _logger.LogDebug("DLNA Server : Starting DLNA advertisements using DLNA version {Version}", Server.DlnaVersion.ToString());
        }

        /// <summary>
        /// Gets the SSDP server instance.
        /// </summary>
        public ISsdpServer Server { get; }

        /// <summary>
        /// Gets or sets a value indicating whether support for windows explorer is enabled.
        /// </summary>
        private bool EnableWindowsExplorerSupport { get; set; }

        /// <summary>
        /// Gets a read only list of devices being published by this instance.
        /// </summary>
        private IEnumerable<SsdpRootDevice> Devices => _readOnlyDevices;

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

            if (_disposed)
            {
                return;
            }

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

        private static string GetUsn(string udn, string fullDeviceType)
        {
            return $"{udn}::{fullDeviceType}";
        }

        private static IEnumerable<SsdpDevice> Flatten(IEnumerable deviceList)
        {
            foreach (SsdpDevice device in deviceList)
            {
                yield return device;

                foreach (var child in device.Services)
                {
                    yield return child;
                }
            }
        }

        /// <summary>
        /// Override this method and dispose any objects you own the lifetime of if disposing is true.
        /// </summary>
        /// <param name="disposing">True if managed objects should be disposed, if false, only unmanaged resources should be released.</param>
        private void Dispose(bool disposing)
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

                _networkManager.NetworkChanged -= NetworkChanged;
                DlnaServerPlugin.Instance!.ConfigurationChanging -= UpdateConfiguration;

                var tasks = Devices.ToList().Select(RemoveDevice).ToArray();
                Task.WaitAll(tasks);

                // Must be last, or there won't be any sockets available.
                Server.DeleteEvent("M-SEARCH", RequestReceived);
            }

            _disposed = true;
        }

        /// <summary>
        /// Removes a device (and it's children) from the list of devices being published by this server, making them indiscoverable.
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
                Server.IncreaseBootId();
                await SendByeByeNotifications(device, true).ConfigureAwait(false);
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
                    _ =>
                    {
                        _ = SendAllAliveNotificationsAsync();
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
            bool sendAll = false;
            bool rootOnly = false;
            bool uuid = false;

            lock (_devices)
            {
                if (string.Equals("ssdp:all", searchTarget, StringComparison.Ordinal))
                {
                    // Services and devices.
                    devices = Flatten(_devices).ToArray();
                    sendAll = true;
                }
                else if (string.Equals(UpnpRootDevice, searchTarget, StringComparison.Ordinal)
                    || (EnableWindowsExplorerSupport && string.Equals(PnpRootDevice, searchTarget, StringComparison.Ordinal)))
                {
                    devices = _devices.ToArray();
                    rootOnly = true;
                }
                else if (searchTarget.Trim().StartsWith("uuid:", StringComparison.Ordinal))
                {
                    devices = (from device in Flatten(_devices)
                               where string.Equals(device.Uuid, searchTarget[5..], StringComparison.Ordinal)
                               select device).ToArray();
                    uuid = true;
                }
                else if (searchTarget.StartsWith("urn:", StringComparison.Ordinal))
                {
                    devices = (from device in Flatten(_devices)
                               where string.Equals(device.FullDeviceType, searchTarget, StringComparison.Ordinal)
                               select device).ToArray();
                }
            }

            var addr = e.ReceivedFrom.Address;
            if (Server.IsTracing(addr, e.LocalIpAddress))
            {
                _logger.LogDebug("M-SEARCH: {Address} <- {Target}", addr, searchTarget);
            }

            if (devices == null)
            {
                return;
            }

            foreach (var device in devices)
            {
                if (_disposed)
                {
                    return;
                }

                SsdpRootDevice rt = device.GetRootDevice();

                if (e.Message.TryGetValue("Location", out var location) && string.Equals(rt.Location, location, StringComparison.Ordinal))
                {
                    // Message came from us - so abort transmitting any more to this device.
                    return;
                }

                if ((rt.NetAddress.AddressFamily != e.ReceivedFrom.Address.AddressFamily) || !rt.NetAddress.Contains(e.ReceivedFrom.Address))
                {
                    // only reply on interfaces in the same IP family or on the interface that the search arrived on.
                    continue;
                }

                if ((sendAll || rootOnly) && (device is SsdpRootDevice))
                {
                    await SendSearchResponseAsync(device, UpnpRootDevice, GetUsn(device.Udn, UpnpRootDevice), e).ConfigureAwait(false);
                    if (EnableWindowsExplorerSupport)
                    {
                        await SendSearchResponseAsync(device, PnpRootDevice, GetUsn(device.Udn, PnpRootDevice), e).ConfigureAwait(false);
                    }

                    if (rootOnly)
                    {
                        continue;
                    }
                }

                if (uuid || sendAll)
                {
                    await SendSearchResponseAsync(device, device.Udn, device.Udn, e).ConfigureAwait(false);
                }
                else
                {
                    await SendSearchResponseAsync(device, device.FullDeviceType, GetUsn(device.Udn, device.FullDeviceType), e).ConfigureAwait(false);
                }
            }
        }

        private async Task SendSearchResponseAsync(SsdpDevice device, string searchTarget, string uniqueServiceName, SsdpEventArgs e)
        {
            const string SsdpResponse = "HTTP/1.1 200 OK";

            var rootDevice = device.GetRootDevice();

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["EXT"] = string.Empty,
                ["DATE"] = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture),
                ["CACHE-CONTROL"] = "max-age = " + rootDevice.CacheLifetime.TotalSeconds,
                ["ST"] = searchTarget,
                ["SERVER"] = SsdpServer.GetInstance().Configuration.GetUserAgent(),
                ["USN"] = uniqueServiceName,
                ["LOCATION"] = rootDevice.Location
            };

            if (Server.DlnaVersion > DlnaVersion.Version1)
            {
                values["CONFIGID.UPNP.ORG"] = Server.ConfigId;
                values["BOOTID.UPNP.ORG"] = Server.BootId;
                values["NEXTBOOTID.UPNP.ORG"] = Server.NextBootId;
                values["SEARCHPORT.UPNP.ORG"] = Server.GetPortFor(e.LocalIpAddress).ToString();
                values["OPT"] = "\"http://schemas.upnp.org/upnp/1/0/\"; ns=01";
                values["01-NLS"] = Server.BootId;
            }

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
        private async Task SendAllAliveNotificationsAsync()
        {
            if (_disposed)
            {
                return;
            }

            SsdpRootDevice[] devices;
            lock (_devices)
            {
                devices = _devices.ToArray();
            }

            foreach (var device in devices)
            {
                await SendAliveNotifications(device, true).ConfigureAwait(false);
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
                if (EnableWindowsExplorerSupport)
                {
                    await SendAliveNotification(device, PnpRootDevice, GetUsn(device.Udn, PnpRootDevice)).ConfigureAwait(false);
                }
            }

            await SendAliveNotification(device, device.Udn, device.Udn).ConfigureAwait(false);
            await SendAliveNotification(device, device.FullDeviceType, GetUsn(device.Udn, device.FullDeviceType)).ConfigureAwait(false);

            foreach (var childDevice in device.Services)
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
            if (_disposed)
            {
                throw new ObjectDisposedException("disposed.");
            }

            var rootDevice = device.GetRootDevice();
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CACHE-CONTROL"] = "max-age=" + rootDevice.CacheLifetime.TotalSeconds,
                ["LOCATION"] = rootDevice.Location,
                ["NT"] = notificationType,
                ["NTS"] = "ssdp:alive",
                ["SERVER"] = SsdpServer.GetInstance().Configuration.GetUserAgent(),
                ["USN"] = uniqueServiceName,
                ["HOST"] = string.Empty // will be populated later.
            };

            if (Server.DlnaVersion > DlnaVersion.Version1)
            {
                values["BOOTID.UPNP.ORG"] = Server.BootId;
                values["CONFIGID.UPNP.ORG"] = Server.ConfigId;
            }

            return Server.SendMulticastSsdp(values, SsdpNotify, rootDevice.NetAddress.AddressFamily);
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

                if (EnableWindowsExplorerSupport)
                {
                    await SendByeByeNotification(device, PnpRootDevice, GetUsn(device.Udn, PnpRootDevice)).ConfigureAwait(false);
                }
            }

            await SendByeByeNotification(device, device.Udn, device.Udn).ConfigureAwait(false);
            await SendByeByeNotification(device, device.FullDeviceType, GetUsn(device.Udn, device.FullDeviceType)).ConfigureAwait(false);

            foreach (var childDevice in device.Services)
            {
                await SendByeByeNotifications(childDevice, false).ConfigureAwait(false);
            }
        }

        private Task SendByeByeNotification(SsdpDevice device, string notificationType, string uniqueServiceName)
        {
            var addr = device.GetRootDevice().NetAddress.AddressFamily;
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["NT"] = notificationType,
                ["NTS"] = "ssdp:byebye",
                ["USN"] = uniqueServiceName,
                ["HOST"] = string.Empty
            };

            if (Server.DlnaVersion >= DlnaVersion.Version1)
            {
                values["BOOTID.UPNP.ORG"] = Server.BootId;
                values["CONFIGID.UPNP.ORG"] = Server.ConfigId;
            }

            return Server.SendMulticastSsdp(values, SsdpNotify, addr, _disposed ? 1 : Server.UdpSendCount);
        }

        /// <summary>
        /// Called when data is received.
        /// </summary>
        /// <param name="args">The <see cref="SsdpEventArgs"/>.</param>
        private async void RequestReceived(SsdpEventArgs args)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("disposed.");
            }

            if (!args.Message.TryGetValue("ST", out var searchTarget) || string.IsNullOrEmpty(searchTarget))
            {
                _logger.LogWarning("Invalid search request received from {Address}, Target is null/empty.", args.ReceivedFrom);
                return;
            }

            if (IsDuplicateSearchRequest(searchTarget, args.ReceivedFrom))
            {
                // _logger.LogDebug("Ignoring duplicate request from {Address}, {Target}.", args.ReceivedFrom, searchTarget);
                // Search Request is a duplicate, so ignore.
                return;
            }

            // Wait on random interval up to MX, as per SSDP spec.
            // Also, as per UPnP 1.1/SSDP spec ignore missing/bank MX header. If over 120, assume random value between 0 and 120.
            // Using 16 as minimum as that's often the minimum system clock frequency anyway.
            // Windows Explorer is poorly behaved and doesn't supply an MX header value.
            int maxWaitInterval = 1;
            if (args.Message.TryGetValue("MX", out var mx))
            {
                if (!int.TryParse(mx, out maxWaitInterval) || maxWaitInterval <= 0)
                {
                    _logger.LogWarning("MX expired.");
                    return;
                }
            }

            await ProcessSearchRequestAsync(maxWaitInterval, searchTarget, args).ConfigureAwait(false);
        }

        /// <summary>
        /// Triggered when the configuration is updated.
        /// </summary>
        /// <param name="sender">The <see cref="DlnaServerPlugin"/> instance.</param>
        /// <param name="configuration">The updated <see cref="DlnaServerConfiguration"/> instance.</param>
        private void UpdateConfiguration(object? sender, BasePluginConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var config = (DlnaServerConfiguration)configuration;

            Server.Configuration.UdpPortRange = config.UdpPortRange;

            if (!string.IsNullOrEmpty(config.DlnaServerName))
            {
                config.DlnaServerName = Regex.Replace(config.DlnaServerName, @"[^\u0000-\u007F]", string.Empty);
            }

            EnableWindowsExplorerSupport = config.EnableWindowsExplorerSupport;

            if (config.AliveMessageIntervalSeconds <= 0)
            {
                config.AliveMessageIntervalSeconds = 1800;
            }

            _aliveMessageInterval = config.AliveMessageIntervalSeconds;
        }

        /// <summary>
        /// Event triggered every time there is a network event.
        /// </summary>
        /// <param name="sender">NetworkManager instance.</param>
        /// <param name="e">Event argument.</param>
        private void NetworkChanged(object? sender, System.EventArgs e)
        {
            UpdateConfiguration(sender, DlnaServerPlugin.Instance!.Configuration);

            if (_rebroadcastAliveNotificationsTimer != null)
            {
                StartBroadcastingAliveMessages(_aliveMessageInterval);
            }
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
