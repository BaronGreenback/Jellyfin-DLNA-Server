using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.Dlna.Server.Eventing
{
    /// <summary>
    /// Interface class for <seealso cref="DlnaEventManager"/> class.
    /// </summary>
    public interface IDlnaEventManager
    {
        /// <summary>
        /// Processes an inbound request.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/> instance.</param>
        /// <param name="response">The <see cref="HttpResponse"/> instance.</param>
        void ProcessEventRequest(HttpRequest request, HttpResponse response);
    }
}
