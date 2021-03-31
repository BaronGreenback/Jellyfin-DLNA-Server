using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Jellyfin.DeviceProfiles;
using Jellyfin.Plugin.Dlna.Server.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Server.ConnectionManager
{
    /// <summary>
    /// Defines the <see cref="ConnectionManagerService" />.
    /// </summary>
    public class ConnectionManagerService : IUpnpService
    {
        private readonly ILogger _logger;
        private readonly IDeviceProfileManager _profileManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionManagerService"/> class.
        /// </summary>
        /// <param name="profileManager">The <see cref="IProfileManager"/> for use with the <see cref="ConnectionManagerService"/> instance.</param>
        /// <param name="logger">The <see cref="ILogger"/> for use with the <see cref="ConnectionManagerService"/> instance.</param>
        public ConnectionManagerService(IDeviceProfileManager profileManager, ILogger logger)
        {
            _logger = logger;
            _profileManager = profileManager;
        }

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Passed from API.")]
        public async Task<string> GetServiceXml(HttpRequest request, HttpResponse response)
        {
            var resourceName = GetType().Namespace + ".ConnectionManager.xml";
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
#pragma warning disable CA1062 // Validate arguments of public methods
            var profile = _profileManager.GetOrCreateProfile(
                request.Headers,
                request.HttpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback);
#pragma warning restore CA1062 // Validate arguments of public methods
            return new ControlHandler(_logger, profile).ProcessControlRequestAsync(request, false);
        }
    }
}
