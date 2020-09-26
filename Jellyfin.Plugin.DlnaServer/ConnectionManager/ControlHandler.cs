using System;
using System.Collections.Generic;
using System.Xml;
using Jellyfin.Plugin.DlnaServer.Configuration;
using Jellyfin.Plugin.DlnaServer.Service;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Model.Dlna;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.DlnaServer.ConnectionManager
{
    /// <summary>
    /// Defines the <see cref="ControlHandler" />.
    /// </summary>
    public class ControlHandler : BaseControlHandler
    {
        private readonly DeviceProfile _profile;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlHandler"/> class.
        /// </summary>
        /// <param name="config">The <see cref="DlnaServerConfiguration"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="logger">The <see cref="ILogger"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="profile">The <see cref="DeviceProfile"/> for use with the <see cref="ControlHandler"/> instance.</param>
        public ControlHandler(DlnaServerConfiguration config, ILogger logger, DeviceProfile profile)
            : base(config, logger)
        {
            _profile = profile;
        }

        /// <inheritdoc />
        protected override void WriteResult(string methodName, IDictionary<string, string> methodParams, XmlWriter xmlWriter)
        {
            if (!string.Equals(methodName, "GetProtocolInfo", StringComparison.OrdinalIgnoreCase))
            {
                throw new ResourceNotFoundException("Unexpected control request name: " + methodName);
            }

            HandleGetProtocolInfo(xmlWriter);
        }

        /// <summary>
        /// Builds the response to the GetProtocolInfo request.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private void HandleGetProtocolInfo(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString("Source", _profile.ProtocolInfo ?? string.Empty);
            xmlWriter.WriteElementString("Sink", string.Empty);
        }
    }
}
