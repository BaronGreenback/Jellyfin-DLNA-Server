using System.Collections.Generic;
using Jellyfin.Plugin.Ssdp.Model;

namespace Jellyfin.Plugin.DlnaServer.ConnectionManager
{
    /// <summary>
    /// Defines the <see cref="ServiceActionListBuilder" />.
    /// </summary>
    public static class ServiceActionListBuilder
    {
        /// <summary>
        /// Returns an enumerable of the ConnectionManagar:1 DLNA actions.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{ServiceAction}"/>.</returns>
        public static IEnumerable<ServiceAction> GetActions()
        {
            var list = new List<ServiceAction>
            {
                GetCurrentConnectionInfo(),
                GetProtocolInfo(),
                GetCurrentConnectionIDs(),
                ConnectionComplete(),
                PrepareForConnection()
            };

            return list;
        }

        /// <summary>
        /// Returns the action details for "PrepareForConnection".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction PrepareForConnection()
        {
            var action = new ServiceAction("PrepareForConnection");
            action.ArgumentList.Add(new Argument("RemoteProtocolInfo", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_ProtocolInfo));
            action.ArgumentList.Add(new Argument("PeerConnectionManager", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_ConnectionManager));
            action.ArgumentList.Add(new Argument("PeerConnectionID", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_ConnectionID));
            action.ArgumentList.Add(new Argument("Direction", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_Direction));
            action.ArgumentList.Add(new Argument("ConnectionID", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_ConnectionID));
            action.ArgumentList.Add(new Argument("AVTransportID", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_AVTransportID));
            action.ArgumentList.Add(new Argument("RcsID", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_RcsID));
            return action;
        }

        /// <summary>
        /// Returns the action details for "GetCurrentConnectionInfo".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetCurrentConnectionInfo()
        {
            var action = new ServiceAction("GetCurrentConnectionInfo");
            action.ArgumentList.Add(new Argument("ConnectionID", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_ConnectionID));
            action.ArgumentList.Add(new Argument("RcsID", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_RcsID));
            action.ArgumentList.Add(new Argument("AVTransportID", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_AVTransportID));
            action.ArgumentList.Add(new Argument("ProtocolInfo", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_ProtocolInfo));
            action.ArgumentList.Add(new Argument("PeerConnectionManager", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_ConnectionManager));
            action.ArgumentList.Add(new Argument("PeerConnectionID", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_ConnectionID));
            action.ArgumentList.Add(new Argument("Direction", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_Direction));
            action.ArgumentList.Add(new Argument("Status", ArgumentDirection.Out, StateVariableType.A_ARG_TYPE_ConnectionStatus));
            return action;
        }

        /// <summary>
        /// Returns the action details for "GetProtocolInfo".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetProtocolInfo()
        {
            var action = new ServiceAction("GetProtocolInfo");
            action.ArgumentList.Add(new Argument("Source", ArgumentDirection.Out, StateVariableType.SourceProtocolInfo));
            action.ArgumentList.Add(new Argument("Sink", ArgumentDirection.Out, StateVariableType.SinkProtocolInfo));
            return action;
        }

        /// <summary>
        /// Returns the action details for "GetCurrentConnectionIDs".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetCurrentConnectionIDs()
        {
            var action = new ServiceAction("GetCurrentConnectionIDs");
            action.ArgumentList.Add(new Argument("ConnectionIDs", ArgumentDirection.Out, StateVariableType.CurrentConnectionIDs));
            return action;
        }

        /// <summary>
        /// Returns the action details for "ConnectionComplete".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction ConnectionComplete()
        {
            var action = new ServiceAction("ConnectionComplete");
            action.ArgumentList.Add(new Argument("ConnectionID", ArgumentDirection.In, StateVariableType.A_ARG_TYPE_ConnectionID));
            return action;
        }
    }
}
