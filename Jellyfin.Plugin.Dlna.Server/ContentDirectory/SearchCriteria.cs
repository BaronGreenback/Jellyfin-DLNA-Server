using System;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.Dlna.Server.ContentDirectory
{
    /// <summary>
    /// Defines the <see cref="SearchCriteria" />.
    /// </summary>
    internal class SearchCriteria
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchCriteria"/> class.
        /// </summary>
        /// <param name="search">The search string.</param>
        public SearchCriteria(string search)
        {
            if (search.Length == 0)
            {
                throw new ArgumentException("String can't be empty.", nameof(search));
            }

            SearchType = SearchType.Unknown;

            string[] factors = RegexSplit(search, "(and|or)");
            foreach (string factor in factors)
            {
                string[] subFactors = RegexSplit(factor.Trim().Trim('(').Trim(')').Trim(), "\\s", 3);

                if (subFactors.Length != 3)
                {
                    continue;
                }

                if (!string.Equals("upnp:class", subFactors[0], StringComparison.OrdinalIgnoreCase)
                    || (!string.Equals("=", subFactors[1], StringComparison.Ordinal)
                        && !string.Equals("derivedfrom", subFactors[1], StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (string.Equals("\"object.item.imageItem\"", subFactors[2], StringComparison.Ordinal)
                    || string.Equals("\"object.item.imageItem.photo\"", subFactors[2], StringComparison.OrdinalIgnoreCase))
                {
                    SearchType = SearchType.Image;
                    return;
                }
                else if (string.Equals("\"object.item.videoItem\"", subFactors[2], StringComparison.OrdinalIgnoreCase))
                {
                    SearchType = SearchType.Video;
                    return;
                }
                else if (string.Equals("\"object.container.playlistContainer\"", subFactors[2], StringComparison.OrdinalIgnoreCase))
                {
                    SearchType = SearchType.Playlist;
                    return;
                }
                else if (string.Equals("\"object.container.album.musicAlbum\"", subFactors[2], StringComparison.OrdinalIgnoreCase))
                {
                    SearchType = SearchType.MusicAlbum;
                    return;
                }
            }
        }

        /// <summary>
        /// Gets the Search type.
        /// </summary>
        public SearchType SearchType { get; }

        /// <summary>
        /// Splits the specified string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="term">The term.</param>
        /// <param name="limit">The limit.</param>
        /// <returns>System.String[].</returns>
        private static string[] RegexSplit(string str, string term, int limit)
        {
            return new Regex(term).Split(str, limit);
        }

        /// <summary>
        /// Splits the specified string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="term">The term.</param>
        /// <returns>System.String[].</returns>
        private static string[] RegexSplit(string str, string term)
        {
            return Regex.Split(str, term, RegexOptions.IgnoreCase);
        }
    }
}
