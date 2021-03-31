using System;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.Server.ConnectionManager;
using Jellyfin.Plugin.Dlna.Server.ContentDirectory;
using Jellyfin.Plugin.Dlna.Server.Eventing;
using Jellyfin.Plugin.Dlna.Server.MediaReceiverRegistrar;
using Jellyfin.Plugin.Dlna.Server.Service;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.Dlna.Server
{
    /// <summary>
    /// Defines the <see cref="IDlnaServerManager" />.
    /// </summary>
    public interface IDlnaServerManager
    {
        /// <summary>
        /// Gets the server identification, which changes on each restart.
        /// </summary>
        Guid ServerId { get; }

        /// <summary>
        /// Gets the server identification in <see cref="ServerId"/> as a string.
        /// </summary>
        string ServerString { get; }

        /// <summary>
        /// Gets the DLNA server' connection manager instance.
        /// </summary>
        IUpnpService ConnectionManager { get; }

        /// <summary>
        /// Gets the DLNA server' content directory service instance.
        /// </summary>
        IUpnpService ContentDirectory { get; }

        /// <summary>
        /// Gets the DLNA server's MediaReceiverRegistrar instance.
        /// </summary>
        IUpnpService? MediaReceiverRegistrar { get; }

        /// <summary>
        /// Gets the DLNA server's event manager instance.
        /// </summary>
        IDlnaEventManager EventManager { get; }

        /// <summary>
        /// Builds the xml containing the server description.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="response">The <see cref="HttpResponse"/>.</param>
        /// <returns>The XML description.</returns>
        string GetServerDescriptionXml(HttpRequest request, HttpResponse response);

        /// <summary>
        /// The dispose method.
        /// </summary>
        void Dispose();
    }
}
