#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1602 // Enumeration items should be documented
namespace Jellyfin.Plugin.DlnaServer.ContentDirectory
{
    /// <summary>
    /// Definition for <see cref="SearchType"/>.
    /// </summary>
    public enum SearchType
    {
        Unknown = 0,
        Audio = 1,
        Image = 2,
        Video = 3,
        Playlist = 4,
        MusicAlbum = 5
    }
}
