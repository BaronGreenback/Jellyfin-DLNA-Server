using System;
using Jellyfin.Plugin.Dlna.Didl;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.Dlna.Server.ContentDirectory
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
        internal ServerItem(BaseItem item)
        {
            Item = item;

            if (item is IItemByName && item is not Folder)
            {
                StubType = Didl.StubType.Folder;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerItem"/> class.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="stubType">The <see cref="StubType"/>.</param>
        internal ServerItem(BaseItem item, StubType? stubType)
        {
            Item = item;
            StubType = stubType;
        }

        /// <summary>
        /// Gets the underlying base item.
        /// </summary>
        internal BaseItem Item { get; }

        /// <summary>
        /// Gets the DLNA item type.
        /// </summary>
        internal StubType? StubType { get; init; }

        /// <summary>
        /// Deconstructs the class.
        /// </summary>
        /// <param name="serverItem">Retrieves the <see cref="BaseItem"/>.</param>
        /// <param name="stubType">Retrieves the <see cref="StubType"/>.</param>
        internal void Deconstruct(out BaseItem serverItem, out StubType? stubType)
        {
            serverItem = Item;
            stubType = StubType;
        }
    }
}
