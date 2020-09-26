using Jellyfin.Plugin.DlnaServer.Eventing;
using Jellyfin.Plugin.DlnaServer.Service;

namespace Jellyfin.Plugin.DlnaServer.MediaReceiverRegistrar
{
    /// <summary>
    /// Interface for <see cref="MediaReceiverRegistrarService"/> class.
    /// </summary>
    public interface IMediaReceiverRegistrar : IDlnaEventManager, IUpnpService
    {
    }
}
