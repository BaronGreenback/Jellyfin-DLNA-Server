using Jellyfin.Plugin.DlnaServer.Eventing;
using Jellyfin.Plugin.DlnaServer.Service;

namespace Jellyfin.Plugin.DlnaServer.ConnectionManager
{
    /// <summary>
    /// Interface class for <seealso cref="ConnectionManagerService"/> class.
    /// </summary>
    public interface IConnectionManager : IDlnaEventManager, IUpnpService
    {
    }
}
