using System.Collections.Generic;
using Jellyfin.Plugin.DlnaServer.Service;
using Jellyfin.Plugin.Ssdp.Model;

namespace Jellyfin.Plugin.DlnaServer.ConnectionManager
{
    /// <summary>
    /// Defines the <see cref="ConnectionManagerXmlBuilder" />.
    /// </summary>
    public static class ConnectionManagerXmlBuilder
    {
        /// <summary>
        /// Gets the ConnectionManager:1 service template.
        /// See http://upnp.org/specs/av/UPnP-av-ConnectionManager-v1-Service.pdf.
        /// </summary>
        /// <returns>An XML description of this service.</returns>
        public static string GetXml()
        {
            return ServiceXmlBuilder.GetXml(ServiceActionListBuilder.GetActions(), GetStateVariables());
        }

        /// <summary>
        /// Get the list of state variables for this invocation.
        /// </summary>
        /// <returns>The <see cref="IEnumerable{StateVariable}"/>.</returns>
        private static IEnumerable<StateVariable> GetStateVariables()
        {
            var list = new List<StateVariable>
            {
                new StateVariable(StateVariableType.SourceProtocolInfo, DataType.Dtstring, true),
                new StateVariable(StateVariableType.SinkProtocolInfo, DataType.Dtstring, true),
                new StateVariable(StateVariableType.CurrentConnectionIDs, DataType.Dtstring, true),
                new StateVariable(StateVariableType.A_ARG_TYPE_ConnectionStatus, DataType.Dtstring, false)
                {
                    AllowedValues = new[]
                    {
                        "OK",
                        "ContentFormatMismatch",
                        "InsufficientBandwidth",
                        "UnreliableChannel",
                        "Unknown"
                    }
                },
                new StateVariable(StateVariableType.A_ARG_TYPE_ConnectionManager, DataType.Dtstring, false),
                new StateVariable(StateVariableType.A_ARG_TYPE_Direction, DataType.Dtstring, false)
                {
                    AllowedValues = new[]
                    {
                        "Output",
                        "Input"
                    }
                },
                new StateVariable(StateVariableType.A_ARG_TYPE_ProtocolInfo, DataType.Dtstring, false),
                new StateVariable(StateVariableType.A_ARG_TYPE_ConnectionID, DataType.Dtui4, false),
                new StateVariable(StateVariableType.A_ARG_TYPE_AVTransportID, DataType.Dtui4, false),
                new StateVariable(StateVariableType.A_ARG_TYPE_RcsID, DataType.Dtui4, false)
            };

            return list;
        }
    }
}
