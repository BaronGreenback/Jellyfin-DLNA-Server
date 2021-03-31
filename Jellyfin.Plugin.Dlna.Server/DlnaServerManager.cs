using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using Jellyfin.DeviceProfiles;
using Jellyfin.Networking.Configuration;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Didl;
using Jellyfin.Plugin.Dlna.Helpers;
using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.Server.ConnectionManager;
using Jellyfin.Plugin.Dlna.Server.ContentDirectory;
using Jellyfin.Plugin.Dlna.Server.Eventing;
using Jellyfin.Plugin.Dlna.Server.MediaReceiverRegistrar;
using Jellyfin.Plugin.Dlna.Server.Service;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Server
{
    /// <summary>
    /// Defines the <see cref="DlnaServerManager"/> class.
    /// </summary>
    public class DlnaServerManager : IDlnaServerManager, IDisposable, IServerEntryPoint
    {
        private readonly Guid _serverId;
        private readonly IConfigurationManager _configurationManager;
        private readonly IDeviceProfileManager _profileManager;
        private readonly ILogger<DlnaServerManager> _logger;
        private readonly INetworkManager _networkManager;
        private readonly IPNetAddress[] _bindAddresses;
        private readonly IServerApplicationHost _appHost;
        private readonly SsdpServerPublisher _publisher;
        private bool _isDisposed;
        private bool _msMediaReceiverActive;

        /// <summary>
        /// Initializes a new instance of the <see cref="DlnaServerManager"/> class.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> instance.</param>
        /// <param name="appHost">The <see cref="IServerApplicationHost"/> instance.</param>
        /// <param name="libraryManager">The <see cref="ILibraryManager"/> instance.</param>
        /// <param name="userManager">The <see cref="IUserManager"/> instance.</param>
        /// <param name="profileManager">The <see cref="IProfileManager"/> instance.</param>
        /// <param name="imageProcessor">The <see cref="IImageProcessor"/> instance.</param>
        /// <param name="userDataManager">The <see cref="IUserDataManager"/> instance.</param>
        /// <param name="localizationManager">The <see cref="ILocalizationManager"/> instance.</param>
        /// <param name="mediaSourceManager">The <see cref="IMediaSourceManager"/> instance.</param>
        /// <param name="mediaEncoder">The <see cref="IMediaEncoder"/> instance.</param>
        /// <param name="networkManager">The <see cref="INetworkManager"/> instance.</param>
        /// <param name="userViewManager">The <see cref="IUserViewManager"/> instance.</param>
        /// <param name="tvSeriesManager">The <see cref="ITVSeriesManager"/> instance.</param>
        /// <param name="configurationManager">The <see cref="IConfigurationManager"/> instance.</param>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> instance.</param>
        /// <param name="xmlSerializer">The <see cref="IXmlSerializer"/> instance.</param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable. :-> ServerString
#pragma warning disable CA1062 // Validate arguments of public methods. -> Created by DI.

        public DlnaServerManager(
            ILoggerFactory loggerFactory,
            IServerApplicationHost appHost,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IDeviceProfileManager profileManager,
            IImageProcessor imageProcessor,
            IUserDataManager userDataManager,
            ILocalizationManager localizationManager,
            IMediaSourceManager mediaSourceManager,
            IMediaEncoder mediaEncoder,
            INetworkManager networkManager,
            IUserViewManager userViewManager,
            ITVSeriesManager tvSeriesManager,
            IConfigurationManager configurationManager,
            IHttpClientFactory httpClientFactory,
            IXmlSerializer xmlSerializer)
        {
            SsdpConfiguration.JellyfinVersion = appHost.ApplicationVersionString;

            // Changing serverId will cause dlna url links to become invalid between restarts.
            ServerId = DlnaServerPlugin.Instance!.Configuration.ChangeIdOnStartup ? Guid.NewGuid() : Guid.Parse(appHost.SystemId);
            _networkManager = networkManager;
            _appHost = appHost;
            _logger = loggerFactory.CreateLogger<DlnaServerManager>();
            _configurationManager = configurationManager;
            _profileManager = profileManager;

            EventManager = new DlnaEventManager(_logger, httpClientFactory);

            // Link into the streaming API, so that headers etc can be performed.
            StreamingHelpers.StreamEvent ??= DlnaStreamHelper.StreamEventProcessor;

            _logger.LogDebug("DLNA Server : Starting Content Directory service.");
            ContentDirectory = new ContentDirectoryService(
                profileManager,
                userDataManager,
                imageProcessor,
                libraryManager,
                userManager,
                _logger,
                localizationManager,
                mediaSourceManager,
                userViewManager,
                mediaEncoder,
                tvSeriesManager);

            _logger.LogDebug("DLNA Server : Starting Connection Manager service.");
            ConnectionManager = new ConnectionManagerService(profileManager, _logger);

            var config = DlnaServerPlugin.Instance!.Configuration;
            // Get bind addresses or interfaces if not specified.
            var interfaces = config.BindAddresses;

            var bindInterfaces = _networkManager.GetInternalBindAddresses();
            if (interfaces.Length > 0)
            {
                // Select only the internal interfaces that are LAN and bound.
                var addresses = _networkManager.CreateIPCollection(interfaces, false, false);
                _bindAddresses = bindInterfaces.Where(i => addresses.Contains(i)
                    && (i.AddressFamily == AddressFamily.InterNetwork || (i.AddressFamily == AddressFamily.InterNetworkV6 && i.Address.ScopeId != 0)))
                    .ToArray();
            }
            else
            {
                _bindAddresses = bindInterfaces.Where(i => !i.IsLoopback()
                    && (i.AddressFamily == AddressFamily.InterNetwork || (i.AddressFamily == AddressFamily.InterNetworkV6 && i.Address.ScopeId != 0)))
                    .ToArray();
            }

            if (_bindAddresses.Length == 0)
            {
                // only use loop-backs if no other address available.
                _bindAddresses = bindInterfaces;
            }

            _publisher = new SsdpServerPublisher(
                configurationManager,
                _logger,
                loggerFactory,
                _networkManager,
                _bindAddresses,
                config.AliveMessageIntervalSeconds,
                config.EnableWindowsExplorerSupport);

            // Load system profiles into memory.
            ProfileHelper.ExtractSystemTemplates(_logger, _profileManager, xmlSerializer).GetAwaiter().GetResult();

            // Solves a race condition can occur when API receives input before DI initialization is complete.
            Instance = this;
        }
#pragma warning restore CS8618
#pragma warning restore CA1062

        /// <summary>
        /// Gets the static reference of the <see cref="DlnaServerManager"/> instance.
        /// </summary>
        public static DlnaServerManager? Instance { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the MsMediaReceiver is active.
        /// </summary>
        public bool MsMediaReceiverActive
        {
            get => _msMediaReceiverActive;

            private set
            {
                // Once active, always active.
                if (value)
                {
                    _logger.LogDebug("DLNA Server : Starting Media Receiver Registrar service.");
                    MediaReceiverRegistrar = (IUpnpService)new MediaReceiverRegistrarService(_logger);
                    _msMediaReceiverActive = true;
                }
            }
        }

        /// <summary>
        /// Gets the server identification, which changes on each restart.
        /// </summary>
        public Guid ServerId
        {
            get => _serverId;
            private init
            {
                _serverId = value;
                ServerString = _serverId.ToString("N", CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Gets the server identification in <see cref="ServerId"/> as a string.
        /// </summary>
        public string ServerString { get; private init; }

        /// <summary>
        /// Gets the DLNA server's ContentDirectory instance.
        /// </summary>
        public IUpnpService ContentDirectory { get; }

        /// <summary>
        /// Gets the DLNA server's ConnectionManager instance.
        /// </summary>
        public IUpnpService ConnectionManager { get; }

        /// <summary>
        /// Gets the DLNA server's MediaReceiverRegistrar instance.
        /// </summary>
        public IUpnpService? MediaReceiverRegistrar { get; private set;  }

        /// <summary>
        /// Gets the DLNA server's event manager instance.
        /// </summary>
        public IDlnaEventManager EventManager { get; }

        /// <summary>
        /// Disposes this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public string GetServerDescriptionXml(HttpRequest request, HttpResponse response)
        {
#pragma warning disable CA1062 // Validate arguments of public methods : only called from API
            var profile = _profileManager.GetOrCreateProfile(
                    request.Headers,
                    request.HttpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback);
#pragma warning restore CA1062 // Validate arguments of public methods

            var config = DlnaServerPlugin.Instance!.Configuration;
            // does the MsMediaReceiver need firing up?

            MsMediaReceiverActive = profile.EnableMSMediaReceiverRegistrar;
            var reply = new DescriptionXmlBuilder(
                profile,
                MsMediaReceiverActive,
                ServerId,
                _appHost.GetSmartApiUrl(request),
                _appHost,
                config.DlnaServerName ?? _appHost.FriendlyName,
                config.DlnaVersion).ToString();

            response?.Headers.Add("Cache-Control", "public, max-age=86400");
            return reply;
        }

        /// <summary>
        /// Registers SSDP endpoints on the internal interfaces and advertises its availability.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync()
        {
            const string FullService = "urn:schemas-upnp-org:device:MediaServer:1";
            var descriptorUri = "/dlna/" + ServerString + "/description.xml";
            var timeout = TimeSpan.FromSeconds(1800);
            var httpPort = ((NetworkConfiguration)_configurationManager.GetConfiguration("network")).HttpServerPortNumber;

            foreach (var address in _bindAddresses)
            {
                _logger.LogInformation("Registering publisher for {Service} on {Address}", FullService, address);

                var uri = new UriBuilder(_appHost.GetSmartApiUrl(address.Address) + descriptorUri);

                if ((_publisher.Server.DlnaVersion != DlnaVersion.Version2) && string.IsNullOrEmpty(_appHost.PublishedServerUrl))
                {
                    // DLNA will only work over http, so we must reset to http:// : {port}.
                    uri.Scheme = "http";
                    uri.Port = httpPort;
                }

                var device = new SsdpRootDevice(timeout, uri.Uri, address, ServerId, "MediaServer");
                device.AddService(new SsdpService(ServerId, "ContentDirectory"));
                device.AddService(new SsdpService(ServerId, "ConnectionManager"));
                await _publisher.AddDevice(device).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Override this method and dispose any objects you own the lifetime of if disposing is true.
        /// </summary>
        /// <param name="disposing">True if managed objects should be disposed, if false, only unmanaged resources should be released.</param>
        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                _publisher.Dispose();
            }

            _isDisposed = true;
        }
    }
}
