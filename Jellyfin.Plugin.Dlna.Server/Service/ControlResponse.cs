using System.Collections.Generic;

namespace Jellyfin.Plugin.Dlna.Server.Service
{
    /// <summary>
    /// Defines the <see cref="ControlResponse" />.
    /// </summary>
    public class ControlResponse
    {
        private static readonly Dictionary<string, string> _headers = new()
        {
            { "EXT", string.Empty }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlResponse"/> class.
        /// </summary>
        /// <param name="xml">The xml to assign.</param>
        public ControlResponse(string xml) => Xml = xml;

        /// <summary>
        /// Gets the Headers.
        /// </summary>
        public static Dictionary<string, string> Headers => _headers;

        /// <summary>
        /// Gets the Xml.
        /// </summary>
        public string Xml { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Xml;
        }
    }
}
