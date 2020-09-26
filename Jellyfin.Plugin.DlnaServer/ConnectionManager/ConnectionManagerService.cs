using System;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Plugin.DlnaServer.Configuration;
using Jellyfin.Plugin.DlnaServer.Service;
using MediaBrowser.Controller.Dlna;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.DlnaServer.ConnectionManager
{
    /// <summary>
    /// Defines the <see cref="ConnectionManagerService" />.
    /// </summary>
    public class ConnectionManagerService : BaseService, IConnectionManager
    {
        private readonly IDlnaManager _dlna;
        private readonly DlnaServerConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionManagerService"/> class.
        /// </summary>
        /// <param name="dlna">The <see cref="IDlnaManager"/> for use with the <see cref="ConnectionManagerService"/> instance.</param>
        /// <param name="config">The <see cref="DlnaServerConfiguration"/> for use with the <see cref="ConnectionManagerService"/> instance.</param>
        /// <param name="logger">The <see cref="ILogger"/> for use with the <see cref="ConnectionManagerService"/> instance.</param>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> for use with the <see cref="ConnectionManagerService"/> instance.</param>
        public ConnectionManagerService(
            IDlnaManager dlna,
            DlnaServerConfiguration config,
            ILogger logger,
            IHttpClientFactory httpClientFactory)
            : base(logger, httpClientFactory)
        {
            _dlna = dlna;
            _config = config;
        }

        /// <inheritdoc />
        public string GetServiceXml()
        {
            var response = ConnectionManagerXmlBuilder.GetXml();
            if (DlnaServerPlugin.Instance!.Configuration.EnableDebugLog)
            {
                Logger.LogDebug(response);
            }

            return response;
        }

        /// <inheritdoc />
        public Task<ControlResponse> ProcessControlRequestAsync(ControlRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var profile = _dlna.GetProfile(request.Headers) ?? _dlna.GetDefaultProfile();

            return new ControlHandler(_config, Logger, profile).ProcessControlRequestAsync(request);
        }
    }
}
