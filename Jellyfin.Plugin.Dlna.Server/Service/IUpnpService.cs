using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.Dlna.Server.Service
{
    /// <summary>
    /// Defines the <see cref="IUpnpService" />.
    /// </summary>
    public interface IUpnpService
    {
        /// <summary>
        /// Gets the content directory as XML.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/> instance.</param>
        /// <param name="response">The <see cref="HttpResponse"/> instance.</param>
        /// <returns>The XML representation of the directory.</returns>
        Task<string> GetServiceXml(HttpRequest request, HttpResponse response);

        /// <summary>
        /// Processes the control request.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/> instance.</param>
        /// <returns>A <see cref="ControlResponse"/> instance.</returns>
        Task<ControlResponse> ProcessControlRequestAsync(HttpRequest request);
    }
}
