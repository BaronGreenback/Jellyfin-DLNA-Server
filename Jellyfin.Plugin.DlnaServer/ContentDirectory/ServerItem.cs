using Jellyfin.Plugin.Ssdp.Didl;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.DlnaServer.ContentDirectory
{
    /// <summary>
    /// Defines the <see cref="ServerItem" />.
    /// </summary>
    internal class ServerItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerItem"/> class.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        public ServerItem(BaseItem item)
        {
            Item = item;

            if (item is IItemByName && !(item is Folder))
            {
                StubType = Ssdp.Didl.StubType.Folder;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerItem"/> class.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="stubType">The <see cref="StubType"/>.</param>
        public ServerItem(BaseItem item, StubType stubType)
        {
            Item = item;
            StubType = stubType;
        }

        /// <summary>
        /// Gets the underlying base item.
        /// </summary>
        public BaseItem Item { get; }

        /// <summary>
        /// Gets or sets the DLNA item type.
        /// </summary>
        public StubType? StubType { get; set; }
    }
}
