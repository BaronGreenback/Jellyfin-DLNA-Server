using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.DlnaServer.ContentDirectory
{
    /// <summary>
    /// Defines the <see cref="SortCriteria" />.
    /// </summary>
    public class SortCriteria
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SortCriteria"/> class.
        /// </summary>
        /// <param name="value">A string representation of the order.</param>
        public SortCriteria(string value)
        {
            if (value.StartsWith("desc", System.StringComparison.OrdinalIgnoreCase))
            {
                SortOrder = SortOrder.Descending;
            }
            else
            {
                SortOrder = SortOrder.Ascending;
            }
        }

        /// <summary>
        /// Gets the SortOrder.
        /// </summary>
        public SortOrder SortOrder { get; }
    }
}
