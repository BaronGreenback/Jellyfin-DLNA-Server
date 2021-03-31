using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.Server.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Server.MediaReceiverRegistrar
{
    /// <summary>
    /// Defines the <see cref="MediaReceiverRegistrarService" />.
    /// </summary>
    public class MediaReceiverRegistrarService : IUpnpService
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaReceiverRegistrarService"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> for use with the <see cref="MediaReceiverRegistrarService"/> instance.</param>
        public MediaReceiverRegistrarService(ILogger logger) => _logger = logger;

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Params from API")]
        public async Task<string> GetServiceXml(HttpRequest request, HttpResponse response)
        {
            var resourceName = GetType().Namespace + ".MediaReceiverRegistrar.xml";
            var stream = typeof(DlnaServerManager).Assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new NullReferenceException(nameof(stream));
            }

            using var stringStream = new StreamReader(stream);
            var reply = await stringStream.ReadToEndAsync().ConfigureAwait(false);

            if (DlnaServerPlugin.Instance!.Configuration.EnableDebugLog)
            {
                _logger.LogDebug("->{Address}: {Reply}", request.HttpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback, reply);
            }

            response.Headers.Add("Cache-Control", "public, max-age=86400");
            // response.Headers.Add("Content-Length", reply.Length.ToString());
            return reply;
        }

        /// <inheritdoc />
        public Task<ControlResponse> ProcessControlRequestAsync(HttpRequest request)
        {
            return new ControlHandler(_logger).ProcessControlRequestAsync(request, false);
        }
    }
}
