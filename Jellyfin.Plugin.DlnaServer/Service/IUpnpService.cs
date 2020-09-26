using System.Threading.Tasks;

namespace Jellyfin.Plugin.DlnaServer.Service
{
    /// <summary>
    /// Defines the <see cref="IUpnpService" />.
    /// </summary>
    public interface IUpnpService
    {
        /// <summary>
        /// Gets the content directory as XML.
        /// </summary>
        /// <returns>The XML representation of the directory.</returns>
        string GetServiceXml();

        /// <summary>
        /// Processes the control request.
        /// </summary>
        /// <param name="request">The <see cref="ControlRequest"/> instance.</param>
        /// <returns>A <see cref="ControlResponse"/> instance.</returns>
        Task<ControlResponse> ProcessControlRequestAsync(ControlRequest request);
    }
}
