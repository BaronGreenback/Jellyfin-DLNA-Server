using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Jellyfin.Networking.Configuration;
using Jellyfin.Networking.Manager;
using Jellyfin.Plugin.DlnaServer.ConnectionManager;
using Jellyfin.Plugin.DlnaServer.ContentDirectory;
using Jellyfin.Plugin.DlnaServer.MediaReceiverRegistrar;
using Jellyfin.Plugin.Ssdp;
using Jellyfin.Plugin.Ssdp.Devices;
using Jellyfin.Plugin.Ssdp.Didl;
using Jellyfin.Plugin.Ssdp.Model;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.DlnaServer
{
    /// <summary>
    /// Defines the <see cref="DlnaServerManager"/> class.
    /// </summary>
    public class DlnaServerManager : IDlnaServerManager, IDisposable
    {
        private readonly ILogger<DlnaServerManager> _logger;
        private readonly INetworkManager _networkManager;
        private readonly IServerApplicationHost _appHost;
        private readonly NetworkConfiguration _netConfig;
        private readonly SsdpServerPublisher _publisher;
        private readonly string _serverId;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DlnaServerManager"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance.</param>
        /// <param name="appHost">The <see cref="IServerApplicationHost"/> instance.</param>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> instance.</param>
        /// <param name="libraryManager">The <see cref="ILibraryManager"/> instance.</param>
        /// <param name="userManager">The <see cref="IUserManager"/> instance.</param>
        /// <param name="dlnaManager">The <see cref="IDlnaManager"/> instance.</param>
        /// <param name="imageProcessor">The <see cref="IImageProcessor"/> instance.</param>
        /// <param name="userDataManager">The <see cref="IUserDataManager"/> instance.</param>
        /// <param name="localizationManager">The <see cref="ILocalizationManager"/> instance.</param>
        /// <param name="mediaSourceManager">The <see cref="IMediaSourceManager"/> instance.</param>
        /// <param name="mediaEncoder">The <see cref="IMediaEncoder"/> instance.</param>
        /// <param name="networkManager">The <see cref="INetworkManager"/> instance.</param>
        /// <param name="userViewManager">The <see cref="IUserViewManager"/> instance.</param>
        /// <param name="tvSeriesManager">The <see cref="ITVSeriesManager"/> instance.</param>
        /// <param name="configuration">The <see cref="IServerConfigurationManager"/> instance.</param>
        public DlnaServerManager(
            ILogger<DlnaServerManager> logger,
            IServerApplicationHost appHost,
            IHttpClientFactory httpClientFactory,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IDlnaManager dlnaManager,
            IImageProcessor imageProcessor,
            IUserDataManager userDataManager,
            ILocalizationManager localizationManager,
            IMediaSourceManager mediaSourceManager,
            IMediaEncoder mediaEncoder,
            INetworkManager networkManager,
            IUserViewManager userViewManager,
            ITVSeriesManager tvSeriesManager,
            IServerConfigurationManager configuration)
        {
            _networkManager = networkManager;
            _appHost = appHost;

            _logger = logger;
            _netConfig = (NetworkConfiguration)(configuration?.GetConfiguration("network") ?? throw new ArgumentNullException(nameof(configuration)));

            var config = DlnaServerPlugin.Instance!.Configuration;
            _logger.LogDebug("DLNA Server : Starting Content Directory service.");
            ContentDirectory = new ContentDirectoryService(
                dlnaManager,
                userDataManager,
                imageProcessor,
                libraryManager,
                config,
                userManager,
                _logger,
                httpClientFactory,
                localizationManager,
                mediaSourceManager,
                userViewManager,
                mediaEncoder,
                tvSeriesManager);

            _serverId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            _logger.LogDebug("DLNA Server : Starting Connection Manager service.");
            ConnectionManager = new ConnectionManagerService(
                dlnaManager,
                config,
                _logger,
                httpClientFactory);

            if (config.EnableMSMediaReceiverRegistrar)
            {
                _logger.LogDebug("DLNA Server : Starting Media Receiver Registrar service.");
                MediaReceiverRegistrar = new MediaReceiverRegistrarService(
                    _logger,
                    httpClientFactory,
                    config);
            }

            _logger.LogDebug("DLNA Server : Starting DLNA advertisements.");
            _publisher = new SsdpServerPublisher(configuration, logger, _networkManager);

            _logger.LogInformation("DLNA Server registered under service id {ServerId}", _serverId);
        }

        /// <summary>
        /// Gets the server identification, which changes on each restart.
        /// </summary>
        public string ServerId => _serverId;

        /// <summary>
        /// Gets the DLNA server' ContentDirectory instance.
        /// </summary>
        public IContentDirectory ContentDirectory { get; }

        /// <summary>
        /// Gets the DLNA server' ConnectionManager instance.
        /// </summary>
        public IConnectionManager ConnectionManager { get; }

        /// <summary>
        /// Gets the DLNA server's MediaReceiverRegistrar instance.
        /// </summary>
        public IMediaReceiverRegistrar? MediaReceiverRegistrar { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Builds the xml containing the server description.
        /// </summary>
        /// <param name="headers">The <see cref="IHeaderDictionary"/>.</param>
        /// <param name="serverId">The server UUID.</param>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <returns>The XML description.</returns>
        public string GetServerDescriptionXml(IHeaderDictionary headers, string serverId, HttpRequest request)
        {
            var config = DlnaServerPlugin.Instance!.Configuration;
            if (string.IsNullOrEmpty(config.DlnaServerName))
            {
                config.DlnaServerName = "Jellyfin - " + _appHost.FriendlyName;
            }

            return new DescriptionXmlBuilder(
                config.EnableMSMediaReceiverRegistrar,
                serverId,
                _appHost,
                _appHost.GetSmartApiUrl(request),
                config.DlnaServerName).ToString();
        }

        /// <summary>
        /// Registers SSDP endpoints on the internal interfaces and advertises its availability.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task Start()
        {
            const string FullService = "urn:schemas-upnp-org:device:MediaServer:1";

            var udn = CreateUuid(_serverId);
            var descriptorUri = "/dlna/" + udn + "/description.xml";

            var bindAddresses = NetworkManager.CreateCollection(
                _networkManager.GetInternalBindAddresses()
                .Where(i => i.AddressFamily == AddressFamily.InterNetwork
                    || (i.AddressFamily == AddressFamily.InterNetworkV6 && i.Address.ScopeId != 0)));

            if (bindAddresses.Count == 0)
            {
                // No interfaces returned, so use loopback.
                bindAddresses = _networkManager.GetLoopbacks();
            }

            foreach (IPNetAddress address in bindAddresses)
            {
                // Limit to LAN addresses only
                if (!_networkManager.IsInLocalNetwork(address))
                {
                    continue;
                }

                _logger.LogInformation("Registering publisher for {Service} on {Address}", FullService, address);

                UriBuilder uri;
                uri = new UriBuilder(_appHost.GetSmartApiUrl(address.Address) + descriptorUri);

                if (SsdpServer.DlnaVersion != DlnaVersion.Version2)
                {
                    if (_appHost.PublishedServerUrl == null)
                    {
                        // DLNA will only work over http, so we must reset to http:// : {port}.
                        uri.Scheme = "http";
                        uri.Port = _netConfig.HttpServerPortNumber;
                    }
                }

                SsdpRootDevice device = new SsdpRootDevice(
                    TimeSpan.FromSeconds(1800),
                    uri.Uri,
                    address,
                    "Jellyfin",
                    "Jellyfin",
                    "Jellyfin Server " + _appHost.ApplicationVersionString,
                    udn);

                SetProperies(device, FullService);

                await _publisher.AddDevice(device).ConfigureAwait(false);

                var embeddedDevices = new[]
                {
                    "urn:schemas-upnp-org:service:ContentDirectory:1",
                    "urn:schemas-upnp-org:service:ConnectionManager:1"
                };

                foreach (var subDevice in embeddedDevices)
                {
                    var embeddedDevice = new SsdpEmbeddedDevice(
                        device.FriendlyName,
                        device.Manufacturer,
                        device.ModelName,
                        udn);
                    SetProperies(embeddedDevice, subDevice);
                    device.AddDevice(embeddedDevice);
                }
            }
        }

        /// <summary>
        /// Override this method and dispose any objects you own the lifetime of if disposing is true.
        /// </summary>
        /// <param name="disposing">True if managed objects should be disposed, if false, only unmanaged resources should be released.</param>
        protected virtual void Dispose(bool disposing)
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

        /// <summary>
        /// Creates a UUID.
        /// </summary>
        /// <param name="text">The source text.</param>
        /// <returns>A string containing an UUID.</returns>
        private static string CreateUuid(string text)
        {
            if (!Guid.TryParse(text, out var guid))
            {
                guid = text.GetMD5();
            }

            return guid.ToString("N", CultureInfo.InvariantCulture);
        }

        private static void SetProperies(SsdpDevice device, string fullDeviceType)
        {
            var service = fullDeviceType.Replace("urn:", string.Empty, StringComparison.OrdinalIgnoreCase).Replace(":1", string.Empty, StringComparison.OrdinalIgnoreCase);

            var serviceParts = service.Split(':');

            var deviceTypeNamespace = serviceParts[0].Replace('.', '-');

            device.DeviceTypeNamespace = deviceTypeNamespace;
            device.DeviceClass = serviceParts[1];
            device.DeviceType = serviceParts[2];
        }
    }
}
