using System;
using System.Collections.Generic;
using System.Xml;
using Jellyfin.Plugin.Dlna.Server.Service;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Model.Dlna;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Server.ConnectionManager
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
        /// <param name="logger">The <see cref="ILogger"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="profile">The <see cref="DeviceProfile"/> for use with the <see cref="ControlHandler"/> instance.</param>
        public ControlHandler(ILogger logger, DeviceProfile profile)
            : base(logger) =>
            _profile = profile;

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Protected method")]
        protected override void WriteResult(string methodName, ControlRequestInfo methodParams, XmlWriter xmlWriter)
        {
            if (string.Equals(methodName, "GetProtocolInfo", StringComparison.Ordinal))
            {
                HandleGetProtocolInfo(xmlWriter);
                return;
            }

            if (string.Equals(methodName, "GetCurrentConnectionIDs", StringComparison.Ordinal))
            {
                HandleGetCurrentConnectionIDs(xmlWriter);
                return;
            }

            if (string.Equals(methodName, "GetCurrentConnectionInfo", StringComparison.Ordinal))
            {
                HandleGetCurrentConnectionInfo(xmlWriter);
                return;
            }

            throw new ResourceNotFoundException("Unexpected control request name: " + methodName);
        }

        /// <summary>
        /// Returns that connection IDs are not supported in the xml stream.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private static void HandleGetCurrentConnectionIDs(XmlWriter xmlWriter)
            => xmlWriter.WriteElementString("CurrentConnectionIDs", "0");

        /// <summary>
        /// Returns that the current connection is OK.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private static void HandleGetCurrentConnectionInfo(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString("RcsID", "0");
            xmlWriter.WriteElementString("AVTransportID", "0");
            xmlWriter.WriteElementString("ProtocolInfo", string.Empty);
            xmlWriter.WriteElementString("PeerConnectionManager", string.Empty);
            xmlWriter.WriteElementString("PeerConnectionID", "0");
            xmlWriter.WriteElementString("Direction", "Input");
            xmlWriter.WriteElementString("Status", "OK");
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
