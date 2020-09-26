using System.Collections.Generic;

namespace Jellyfin.Plugin.DlnaServer.Service
{
    /// <summary>
    /// Defines the <see cref="ControlResponse" />.
    /// </summary>
    public class ControlResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlResponse"/> class.
        /// </summary>
        /// <param name="xml">The xml to assign.</param>
        public ControlResponse(string xml)
        {
            Xml = xml;
            Headers = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets the Headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

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
