using System.Collections.Generic;
using Jellyfin.Plugin.DlnaServer.Service;
using Jellyfin.Plugin.Ssdp.Model;

namespace Jellyfin.Plugin.DlnaServer.ContentDirectory
{
    /// <summary>
    /// Defines the <see cref="ContentDirectoryXmlBuilder" />.
    /// </summary>
    public static class ContentDirectoryXmlBuilder
    {
        /// <summary>
        /// Gets the ContentDirectory:1 service template.
        /// See http://upnp.org/specs/av/UPnP-av-ContentDirectory-v1-Service.pdf.
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
            return new List<StateVariable>
            {
                new (StateVariableType.A_ARG_TYPE_Filter, DataType.Dtstring, false),
                new (StateVariableType.A_ARG_TYPE_Filter, DataType.Dtstring, false),
                new (StateVariableType.A_ARG_TYPE_SortCriteria, DataType.Dtstring, false),
                new (StateVariableType.A_ARG_TYPE_Index, DataType.Dtui4, false),
                new (StateVariableType.A_ARG_TYPE_Count, DataType.Dtui4, false),
                new (StateVariableType.A_ARG_TYPE_UpdateID, DataType.Dtui4, false),
                new (StateVariableType.SearchCapabilities, DataType.Dtstring, false),
                new (StateVariableType.SortCapabilities, DataType.Dtstring, false),
                new (StateVariableType.SystemUpdateID, DataType.Dtui4, true),
                new (StateVariableType.A_ARG_TYPE_SearchCriteria, DataType.Dtstring, false),
                new (StateVariableType.A_ARG_TYPE_Result, DataType.Dtstring, false),
                new (StateVariableType.A_ARG_TYPE_ObjectID, DataType.Dtstring, false),
                new (StateVariableType.A_ARG_TYPE_BrowseFlag, DataType.Dtstring, false)
                {
                    AllowedValues = new[]
                    {
                        "BrowseMetadata",
                        "BrowseDirectChildren"
                    }
                },
                new (StateVariableType.A_ARG_TYPE_BrowseLetter, DataType.Dtstring, false),
                new (StateVariableType.A_ARG_TYPE_CategoryType, DataType.Dtui4, false),
                new (StateVariableType.A_ARG_TYPE_RID, DataType.Dtui4, false),
                new (StateVariableType.A_ARG_TYPE_PosSec, DataType.Dtui4, false),
                new (StateVariableType.A_ARG_TYPE_Featurelist, DataType.Dtstring, false)
            };
        }
    }
}
