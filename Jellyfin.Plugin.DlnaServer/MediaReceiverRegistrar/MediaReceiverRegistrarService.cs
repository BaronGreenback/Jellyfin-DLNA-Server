using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Plugin.DlnaServer.Configuration;
using Jellyfin.Plugin.DlnaServer.Service;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.DlnaServer.MediaReceiverRegistrar
{
    /// <summary>
    /// Defines the <see cref="MediaReceiverRegistrarService" />.
    /// </summary>
    public class MediaReceiverRegistrarService : BaseService, IMediaReceiverRegistrar
    {
        private readonly DlnaServerConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaReceiverRegistrarService"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> for use with the <see cref="MediaReceiverRegistrarService"/> instance.</param>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> for use with the <see cref="MediaReceiverRegistrarService"/> instance.</param>
        /// <param name="config">The <see cref="DlnaServerConfiguration"/> for use with the <see cref="MediaReceiverRegistrarService"/> instance.</param>
        public MediaReceiverRegistrarService(
            ILogger logger,
            IHttpClientFactory httpClientFactory,
            DlnaServerConfiguration config)
            : base(logger, httpClientFactory)
        {
            _config = config;
        }

        /// <inheritdoc />
        public string GetServiceXml()
        {
            var response = MediaReceiverRegistrarXmlBuilder.GetXml();
            if (DlnaServerPlugin.Instance!.Configuration.EnableDebugLog)
            {
                Logger.LogDebug(response);
            }

            return response;
        }

        /// <inheritdoc />
        public Task<ControlResponse> ProcessControlRequestAsync(ControlRequest request)
        {
            return new ControlHandler(
                _config,
                Logger)
                .ProcessControlRequestAsync(request);
        }
    }
}
