using System.Collections.Generic;
using Jellyfin.Plugin.Ssdp.Model;

namespace Jellyfin.Plugin.DlnaServer.MediaReceiverRegistrar
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
                GetIsValidated(),
                GetIsAuthorized(),
                GetRegisterDevice(),
                GetGetAuthorizationDeniedUpdateId(),
                GetGetAuthorizationGrantedUpdateId(),
                GetGetValidationRevokedUpdateId(),
                GetGetValidationSucceededUpdateId()
            };
        }

        /// <summary>
        /// Returns the action details for "IsValidated".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetIsValidated()
        {
            var action = new ServiceAction("IsValidated");
            action.ArgumentList.Add(new Argument("DeviceID", ArgumentDirection.In));
            action.ArgumentList.Add(new Argument("Result", ArgumentDirection.Out));
            return action;
        }

        /// <summary>
        /// Returns the action details for "IsAuthorized".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetIsAuthorized()
        {
            var action = new ServiceAction("IsAuthorized");

            action.ArgumentList.Add(new Argument("DeviceID", ArgumentDirection.In));
            action.ArgumentList.Add(new Argument("Result", ArgumentDirection.Out));
            return action;
        }

        /// <summary>
        /// Returns the action details for "RegisterDevice".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetRegisterDevice()
        {
            var action = new ServiceAction("RegisterDevice");
            action.ArgumentList.Add(new Argument("RegistrationReqMsg", ArgumentDirection.In));
            action.ArgumentList.Add(new Argument("RegistrationRespMsg", ArgumentDirection.Out));
            return action;
        }

        /// <summary>
        /// Returns the action details for "GetValidationSucceededUpdateID".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetGetValidationSucceededUpdateId()
        {
            var action = new ServiceAction("GetValidationSucceededUpdateID");
            action.ArgumentList.Add(new Argument("ValidationSucceededUpdateID", ArgumentDirection.Out));
            return action;
        }

        /// <summary>
        /// Returns the action details for "GetGetAuthorizationDeniedUpdateID".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetGetAuthorizationDeniedUpdateId()
        {
            var action = new ServiceAction("GetAuthorizationDeniedUpdateID");
            action.ArgumentList.Add(new Argument("AuthorizationDeniedUpdateID", ArgumentDirection.Out));
            return action;
        }

        /// <summary>
        /// Returns the action details for "GetValidationRevokedUpdateID".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetGetValidationRevokedUpdateId()
        {
            var action = new ServiceAction("GetValidationRevokedUpdateID");
            action.ArgumentList.Add(new Argument("ValidationRevokedUpdateID", ArgumentDirection.Out));
            return action;
        }

        /// <summary>
        /// Returns the action details for "GetAuthorizationGrantedUpdateID".
        /// </summary>
        /// <returns>The <see cref="ServiceAction"/>.</returns>
        private static ServiceAction GetGetAuthorizationGrantedUpdateId()
        {
            var action = new ServiceAction("GetAuthorizationGrantedUpdateID");
            action.ArgumentList.Add(new Argument("AuthorizationGrantedUpdateID", ArgumentDirection.Out));
            return action;
        }
    }
}
