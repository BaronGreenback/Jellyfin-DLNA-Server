using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.Dlna.Server.ContentDirectory
{
    /// <summary>
    /// Defines the <see cref="SortCriteria" />.
    /// </summary>
    internal class SortCriteria
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SortCriteria"/> class.
        /// </summary>
        /// <param name="value">A string representation of the order.</param>
        public SortCriteria(string value) => SortOrder = value.StartsWith("desc", System.StringComparison.OrdinalIgnoreCase) ? SortOrder.Descending : SortOrder.Ascending;

        /// <summary>
        /// Gets the SortOrder.
        /// </summary>
        public SortOrder SortOrder { get; }
    }
}
