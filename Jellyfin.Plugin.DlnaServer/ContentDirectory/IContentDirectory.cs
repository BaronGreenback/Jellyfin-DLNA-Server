using Jellyfin.Plugin.DlnaServer.Eventing;
using Jellyfin.Plugin.DlnaServer.Service;

namespace Jellyfin.Plugin.DlnaServer.ContentDirectory
{
    /// <summary>
    /// Interface for <see cref="ContentDirectoryService"/> class.
    /// </summary>
    public interface IContentDirectory : IDlnaEventManager, IUpnpService
    {
    }
}
