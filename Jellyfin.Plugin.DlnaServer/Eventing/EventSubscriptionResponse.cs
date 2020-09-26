using System.Collections.Generic;

namespace Jellyfin.Plugin.DlnaServer.Eventing
{
    /// <summary>
    /// Defines the <see cref="EventSubscriptionResponse" />.
    /// </summary>
    public class EventSubscriptionResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventSubscriptionResponse"/> class.
        /// </summary>
        public EventSubscriptionResponse()
        {
            // TODO: investigate why this isn't been used properly.
            Headers = new Dictionary<string, string>();
            Content = string.Empty;
            ContentType = string.Empty;
        }

        /// <summary>
        /// Gets or sets the subscription response content.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the subscription response contentType.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets the subscription response headers.
        /// </summary>
        // TODO: Headers is never used. Check why.
        public Dictionary<string, string> Headers { get; }
    }
}
