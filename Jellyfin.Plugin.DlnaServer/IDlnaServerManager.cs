using System.Threading.Tasks;
using Jellyfin.Plugin.DlnaServer.Configuration;
using Jellyfin.Plugin.DlnaServer.ConnectionManager;
using Jellyfin.Plugin.DlnaServer.ContentDirectory;
using Jellyfin.Plugin.DlnaServer.MediaReceiverRegistrar;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.DlnaServer
{
    /// <summary>
    /// Defines the <see cref="IDlnaServerManager" />.
    /// </summary>
    public interface IDlnaServerManager
    {
        /// <summary>
        /// Gets the server identification, which changes on each restart.
        /// </summary>
        string ServerId { get; }

        /// <summary>
        /// Gets the DLNA server' connection manager instance.
        /// </summary>
        IConnectionManager ConnectionManager { get; }

        /// <summary>
        /// Gets the DLNA server' content directory service instance.
        /// </summary>
        IContentDirectory ContentDirectory { get; }

        /// <summary>
        /// Gets the DLNA server's MediaReceiverRegistrar instance.
        /// </summary>
        IMediaReceiverRegistrar? MediaReceiverRegistrar { get; }

        /// <summary>
        /// Gets the server description XML.
        /// </summary>
        /// <param name="headers">The headers.</param>
        /// <param name="serverUuId">The server's unique identifier.</param>
        /// <param name="request">The http request instance.</param>
        /// <returns>System.String.</returns>
        string GetServerDescriptionXml(IHeaderDictionary headers, string serverUuId, HttpRequest request);

        /// <summary>
        /// Registers SSDP endpoints on the internal interfaces and advertises its availability.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task Start();

        /// <summary>
        /// The dispose method.
        /// </summary>
        void Dispose();
    }
}
