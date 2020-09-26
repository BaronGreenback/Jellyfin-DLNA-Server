using System.Collections.Generic;
using Jellyfin.Plugin.Ssdp.Model;

namespace Jellyfin.Plugin.DlnaServer.ContentDirectory
{
    /// <summary>
    /// Defines the <see cref="ServiceActionListBuilder" />.
    /// </summary>
    public static class ServiceActionListBuilder
    {
        /// <summary>
        /// Returns a list of services that this instance provides.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{ServiceAction}"/>.</returns>
        public static IEnumerable<ServiceAction> GetActions()
        {
            return new[]
            {
                GetSearchCapabilitiesAction(),
                GetSortCapabilitiesAction(),
                GetGetSystemUpdateIdAction(),
                GetBrowseAction(),
                GetSearchAction(),
                GetX_GetFeatureListAction(),
                GetXSetBookmarkAction(),
                GetBrowseByLetterAction()
            };
        }

        /// <summary>
        /// Returns the action details for "GetSystemUpdateID".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetGetSystemUpdateIdAction()
        {
            var action = new ServiceAction("GetSystemUpdateID");
            action.ArgumentList.Add(new Argument("Id", ArgumentDirection.Out, StateVariableType.SystemUpdateID));
            return action;
        }

        /// <summary>
        /// Returns the action details for "GetSearchCapabilities".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetSearchCapabilitiesAction()
        {
            var action = new ServiceAction("GetSearchCapabilities");
            action.ArgumentList.Add(new Argument("SearchCaps", ArgumentDirection.Out, StateVariableType.SearchCapabilities));
            return action;
        }

        /// <summary>
        /// Returns the action details for "GetSortCapabilities".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetSortCapabilitiesAction()
        {
            var action = new ServiceAction("GetSortCapabilities");
            action.ArgumentList.Add(new Argument("SortCaps", ArgumentDirection.Out, StateVariableType.SortCapabilities));
            return action;
        }

        /// <summary>
        /// Returns the action details for "X_GetFeatureList".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetX_GetFeatureListAction()
        {
            var action = new ServiceAction("X_GetFeatureList");
            action.ArgumentList.Add(new Argument("FeatureList", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_Featurelist));
            return action;
        }

        /// <summary>
        /// Returns the action details for "Search".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetSearchAction()
        {
            var action = new ServiceAction("Search");
            action.ArgumentList.Add(new Argument("ContainerID", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_ObjectID));
            action.ArgumentList.Add(new Argument("SearchCriteria", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_SearchCriteria));
            action.ArgumentList.Add(new Argument("Filter", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_Filter));
            action.ArgumentList.Add(new Argument("StartingIndex", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_Index));
            action.ArgumentList.Add(new Argument("RequestedCount", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_Count));
            action.ArgumentList.Add(new Argument("SortCriteria", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_SortCriteria));
            action.ArgumentList.Add(new Argument("Result", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_Result));
            action.ArgumentList.Add(new Argument("NumberReturned", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_Count));
            action.ArgumentList.Add(new Argument("TotalMatches", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_Count));
            action.ArgumentList.Add(new Argument("UpdateID", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_UpdateID));
            return action;
        }

        /// <summary>
        /// Returns the action details for "Browse".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetBrowseAction()
        {
            var action = new ServiceAction("Browse");
            action.ArgumentList.Add(new Argument("ObjectID", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_ObjectID));
            action.ArgumentList.Add(new Argument("BrowseFlag", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_BrowseFlag));
            action.ArgumentList.Add(new Argument("Filter", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_Filter));
            action.ArgumentList.Add(new Argument("StartingIndex", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_Index));
            action.ArgumentList.Add(new Argument("RequestedCount", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_Count));
            action.ArgumentList.Add(new Argument("SortCriteria", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_SortCriteria));
            action.ArgumentList.Add(new Argument("Result", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_Result));
            action.ArgumentList.Add(new Argument("NumberReturned", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_Count));
            action.ArgumentList.Add(new Argument("TotalMatches", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_Count));
            action.ArgumentList.Add(new Argument("UpdateID", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_UpdateID));
            return action;
        }

        /// <summary>
        /// Returns the action details for "X_BrowseByLetter".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetBrowseByLetterAction()
        {
            var action = new ServiceAction("X_BrowseByLetter");
            action.ArgumentList.Add(new Argument("ObjectID", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_ObjectID));
            action.ArgumentList.Add(new Argument("BrowseFlag", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_BrowseFlag));
            action.ArgumentList.Add(new Argument("Filter", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_Filter));
            action.ArgumentList.Add(new Argument("StartingLetter", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_BrowseLetter));
            action.ArgumentList.Add(new Argument("RequestedCount", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_Count));
            action.ArgumentList.Add(new Argument("SortCriteria", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_SortCriteria));
            action.ArgumentList.Add(new Argument("Result", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_Result));
            action.ArgumentList.Add(new Argument("NumberReturned", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_Count));
            action.ArgumentList.Add(new Argument("TotalMatches", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_Count));
            action.ArgumentList.Add(new Argument("UpdateID", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_UpdateID));
            action.ArgumentList.Add(new Argument("StartingIndex", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_Index));
            return action;
        }

        /// <summary>
        /// Returns the action details for "X_SetBookmark".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetXSetBookmarkAction()
        {
            var action = new ServiceAction("X_SetBookmark");
            action.ArgumentList.Add(new Argument("CategoryType", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_CategoryType));
            action.ArgumentList.Add(new Argument("RID", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_RID));
            action.ArgumentList.Add(new Argument("ObjectID", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_ObjectID));
            action.ArgumentList.Add(new Argument("PosSecond", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_PosSec));
            return action;
        }
    }
}
