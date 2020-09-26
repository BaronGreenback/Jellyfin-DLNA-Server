using System.Net.Http;
using Jellyfin.Plugin.DlnaServer.Eventing;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.DlnaServer.Service
{
    /// <summary>
    /// Defines the <see cref="BaseService" />.
    /// </summary>
    public class BaseService : IDlnaEventManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseService"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance.</param>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> instance.</param>
        protected BaseService(ILogger logger, IHttpClientFactory httpClientFactory)
        {
            Logger = logger;
            EventManager = new DlnaEventManager(logger, httpClientFactory);
        }

        /// <summary>
        /// Gets the EventManager.
        /// </summary>
        protected IDlnaEventManager EventManager { get; }

        /// <summary>
        /// Gets the Logger.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Cancels a subscription.
        /// </summary>
        /// <param name="subscriptionId">The id of the subscription.</param>
        /// <returns>A <see cref="EventSubscriptionResponse"/> instance.</returns>
        public EventSubscriptionResponse CancelEventSubscription(string subscriptionId)
        {
            return EventManager.CancelEventSubscription(subscriptionId);
        }

        /// <summary>
        /// Renews a subscription.
        /// </summary>
        /// <param name="subscriptionId">The id of the subscription.</param>
        /// <param name="notificationType">The notification type.</param>
        /// <param name="timeoutString">The timeout.</param>
        /// <param name="callbackUrl">A callback Url.</param>
        /// <returns>A <see cref="EventSubscriptionResponse"/> instance.</returns>
        public EventSubscriptionResponse RenewEventSubscription(string subscriptionId, string notificationType, string timeoutString, string callbackUrl)
        {
            return EventManager.RenewEventSubscription(subscriptionId, notificationType, timeoutString, callbackUrl);
        }

        /// <summary>
        /// Creates a subscription.
        /// </summary>
        /// <param name="notificationType">The notification type.</param>
        /// <param name="timeoutString">The timeout.</param>
        /// <param name="callbackUrl">A callback Url.</param>
        /// <returns>The <see cref="EventSubscriptionResponse"/>.</returns>
        public EventSubscriptionResponse CreateEventSubscription(string notificationType, string timeoutString, string callbackUrl)
        {
            return EventManager.CreateEventSubscription(notificationType, timeoutString, callbackUrl);
        }
    }
}
