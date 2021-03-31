using System;

namespace Jellyfin.Plugin.Dlna.Server.Eventing
{
    /// <summary>
    /// Defines the <see cref="EventSubscription" />.
    /// </summary>
    public class EventSubscription
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventSubscription"/> class.
        /// </summary>
        /// <param name="id">The id of the event.</param>
        /// <param name="callbackurl">The callback url for the event.</param>
        /// <param name="subscriptionTime">The subscription time of the event.</param>
        /// <param name="timeout">The timeout value for the event.</param>
        /// <param name="notificationType">The notification type.</param>
        /// <param name="stateVars">State variables to return.</param>
        public EventSubscription(string id, string callbackurl, DateTime subscriptionTime, int timeout, string notificationType, string? stateVars)
        {
            Id = id;
            CallbackUrl = callbackurl;
            SubscriptionTime = subscriptionTime;
            TimeoutSeconds = timeout;
            NotificationType = notificationType;
            StateVars = stateVars?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        }

        /// <summary>
        /// Gets the event subscription Id.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the variables being watched.
        /// </summary>
        public string[] StateVars { get; }

        /// <summary>
        /// Gets the url the event is to use to notify the client.
        /// </summary>
        public string CallbackUrl { get; }

        /// <summary>
        /// Gets the type of notification.
        /// </summary>
        public string NotificationType { get; }

        /// <summary>
        /// Gets or sets the length of the subscription.
        /// </summary>
        public DateTime SubscriptionTime { get; set; }

        /// <summary>
        /// Gets or sets the timeout of the subscription.
        /// </summary>
        public int TimeoutSeconds { get; set; }

        /// <summary>
        /// Gets the number of times this event has triggered.
        /// </summary>
        public long TriggerCount { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this event is exported.
        /// </summary>
        public bool IsExpired => SubscriptionTime.AddSeconds(TimeoutSeconds) >= DateTime.UtcNow;

        /// <summary>
        /// Increments the trigger count.
        /// </summary>
        public void IncrementTriggerCount()
        {
            if (TriggerCount == long.MaxValue)
            {
                TriggerCount = 0;
            }

            TriggerCount++;
        }
    }
}
