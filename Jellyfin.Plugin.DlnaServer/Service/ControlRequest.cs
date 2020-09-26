using System.IO;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.DlnaServer.Service
{
    /// <summary>
    /// Defines the <see cref="ControlRequest" />.
    /// </summary>
    public class ControlRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlRequest"/> class.
        /// </summary>
        /// <param name="headers">The headers.</param>
        /// <param name="inputXml">A <see cref="Stream"/> instance.</param>
        /// <param name="requestedUrl">The requested Url.</param>
        public ControlRequest(IHeaderDictionary headers, Stream inputXml, string requestedUrl)
        {
            Headers = headers;
            RequestedUrl = requestedUrl;
            InputXml = inputXml;
        }

        /// <summary>
        /// Gets the Headers.
        /// </summary>
        public IHeaderDictionary Headers { get; }

        /// <summary>
        /// Gets the Input Xml stream.
        /// </summary>
        public Stream InputXml { get; }

        /// <summary>
        /// Gets the Requested Url.
        /// </summary>
        public string RequestedUrl { get; }
    }
}
