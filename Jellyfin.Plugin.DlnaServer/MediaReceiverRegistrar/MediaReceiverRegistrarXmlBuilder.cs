using System.Collections.Generic;
using Jellyfin.Plugin.DlnaServer.Service;
using Jellyfin.Plugin.Ssdp.Model;

namespace Jellyfin.Plugin.DlnaServer.MediaReceiverRegistrar
{
    /// <summary>
    /// Defines the <see cref="MediaReceiverRegistrarXmlBuilder" />.
    /// See https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-drmnd/5d37515e-7a63-4709-8258-8fd4e0ed4482.
    /// </summary>
    public static class MediaReceiverRegistrarXmlBuilder
    {
        /// <summary>
        /// Retrieves an XML description of the X_MS_MediaReceiverRegistrar.
        /// </summary>
        /// <returns>An XML representation of this service.</returns>
        public static string GetXml()
        {
            return ServiceXmlBuilder.GetXml(ServiceActionListBuilder.GetActions(), GetStateVariables());
        }

        /// <summary>
        /// The a list of all the state variables for this invocation.
        /// </summary>
        /// <returns>The <see cref="IEnumerable{StateVariable}"/>.</returns>
        private static IEnumerable<StateVariable> GetStateVariables()
        {
            var list = new List<StateVariable>
            {
                new (StateVariableType.AuthorizationGrantedUpdateID, DataType.Dtui4, true),
                new (StateVariableType.A_ARG_TYPE_DeviceID, DataType.Dtstring, false),
                new (StateVariableType.AuthorizationDeniedUpdateID, DataType.Dtui4, true),
                new (StateVariableType.ValidationSucceededUpdateID, DataType.Dtui4, true),
                new (StateVariableType.A_ARG_TYPE_RegistrationRespMsg, DataType.Dtbin_base64, false),
                new (StateVariableType.A_ARG_TYPE_RegistrationReqMsg, DataType.Dtbin_base64, false),
                new (StateVariableType.ValidationRevokedUpdateID, DataType.Dtui4, true),
                new (StateVariableType.A_ARG_TYPE_Result, DataType.Dtint, false)
            };

            return list;
        }
    }
}
