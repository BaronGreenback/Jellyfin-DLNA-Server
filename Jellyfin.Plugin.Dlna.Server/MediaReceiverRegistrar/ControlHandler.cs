using System;
using System.Collections.Generic;
using System.Xml;
using Jellyfin.Plugin.Dlna.Server.Service;
using MediaBrowser.Common.Extensions;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Server.MediaReceiverRegistrar
{
    /// <summary>
    /// Defines the <see cref="ControlHandler" />.
    /// </summary>
    public class ControlHandler : BaseControlHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlHandler"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> for use with the <see cref="ControlHandler"/> instance.</param>
        public ControlHandler(ILogger logger)
            : base(logger)
        {
        }

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "XmlWriter is always set.")]
        protected override void WriteResult(string methodName, ControlRequestInfo methodParams, XmlWriter xmlWriter)
        {
            if (string.Equals(methodName, "IsAuthorized", StringComparison.Ordinal))
            {
                HandleIsAuthorized(xmlWriter);
                return;
            }

            if (string.Equals(methodName, "IsValidated", StringComparison.Ordinal))
            {
                HandleIsValidated(xmlWriter);
                return;
            }

            if (string.Equals(methodName, "RegisterDevice", StringComparison.Ordinal))
            {
                HandleRegisterDevice(xmlWriter);
                return;
            }

            throw new ResourceNotFoundException("Unexpected control request name: " + methodName);
        }

        /// <summary>
        /// Records that the handle is authorized in the xml stream.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private static void HandleIsAuthorized(XmlWriter xmlWriter)
            => xmlWriter.WriteElementString("Result", "1");

        /// <summary>
        /// Records that the handle is authorized in the xml stream.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private static void HandleRegisterDevice(XmlWriter xmlWriter)
            => xmlWriter.WriteElementString("Result", "1");

        /// <summary>
        /// Records that the handle is validated in the xml stream.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private static void HandleIsValidated(XmlWriter xmlWriter)
            => xmlWriter.WriteElementString("Result", "1");
    }
}
