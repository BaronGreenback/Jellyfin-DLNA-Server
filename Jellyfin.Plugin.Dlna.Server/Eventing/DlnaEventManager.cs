using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Ssdp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Server.Eventing
{
    /// <summary>
    /// Defines the <see cref="DlnaEventManager"/> class.
    /// </summary>
    internal class DlnaEventManager : IDlnaEventManager
    {
        private const int DefaultTimeout = 300;

        private readonly ConcurrentDictionary<string, EventSubscription> _subscriptions;
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CultureInfo _usCulture = new("en-US");

        /// <summary>
        /// Initializes a new instance of the <see cref="DlnaEventManager"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance.</param>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> instance.</param>
        public DlnaEventManager(ILogger logger, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _subscriptions = new(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Processes an inbound request.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/> instance.</param>
        /// <param name="response">The <see cref="HttpResponse"/> instance.</param>
        public void ProcessEventRequest(HttpRequest request, HttpResponse response)
        {
            if (request == null)
            {
                throw new NullReferenceException(nameof(request));
            }

            var subscriptionId = request.Headers["SID"];
            if (string.Equals(request.Method, "unsubscribe", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Canceling event subscription {Id}", subscriptionId);

                _subscriptions.TryRemove(subscriptionId, out _);
                return;
            }

            var notificationType = request.Headers["NT"];
            var callbackUrl = request.Headers["CALLBACK"];
            var requestedTimeoutString = request.Headers["TIMEOUT"];
            var timeout = ParseTimeout(requestedTimeoutString);
            var id = "uuid:" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            if (string.IsNullOrEmpty(notificationType))
            {
                var subscription = GetSubscription(subscriptionId, false);
                if (subscription == null)
                {
                    // invalid subscription id.
                    throw new IndexOutOfRangeException();
                }

                _logger.LogDebug("Renewing event subscription {subscription} with timeout of {Timeout}", subscriptionId, timeout);

                subscription.SubscriptionTime = DateTime.UtcNow;
            }
            else
            {
                _logger.LogDebug(
                    "Creating event subscription for {Notification} with timeout of {Timeout} to {Callback}",
                    notificationType,
                    timeout,
                    callbackUrl);

                var stateVar = request.Headers["STATEVAR"];
                _subscriptions.TryAdd(id, new EventSubscription(id, callbackUrl, DateTime.UtcNow, timeout, notificationType, stateVar));
                response.Headers["ACCEPTED-STATEVAR"] = stateVar;
            }

            response.Headers["DATE"] = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);
            response.Headers["SERVER"] = SsdpServer.GetInstance().Configuration.GetUserAgent();
            response.Headers["SID"] = id;
            response.Headers["TIMEOUT"] = string.IsNullOrEmpty(requestedTimeoutString)
                ? ("SECOND-" + timeout.ToString(_usCulture))
                : requestedTimeoutString;
        }

        /// <summary>
        /// Triggers an event.
        /// </summary>
        /// <param name="notificationType">The event notification type.</param>
        /// <param name="stateVariables">The state variables to include with the event.</param>
        /// <returns>A Task.</returns>
        public Task TriggerEvent(string notificationType, IDictionary<string, string> stateVariables)
        {
            var subs = _subscriptions.Values
                .Where(i => !i.IsExpired && string.Equals(notificationType, i.NotificationType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var tasks = subs.Select(i => TriggerEvent(i, stateVariables));

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Returns the event subscription record for the id provided.
        /// </summary>
        /// <param name="id">The id of the subscription.</param>
        /// <param name="throwOnMissing">Set to true, if an exception is to be thrown if the id cannot be located.</param>
        /// <returns>An <see cref="EventSubscription"/> containing the record, or null if not found.</returns>
        private EventSubscription? GetSubscription(string id, bool throwOnMissing)
        {
            if (!_subscriptions.TryGetValue(id, out EventSubscription? e) && throwOnMissing)
            {
                throw new ResourceNotFoundException("Event with Id " + id + " not found.");
            }

            return e;
        }

        /// <summary>
        /// Triggers an event.
        /// </summary>
        /// <param name="subscription">The <see cref="EventSubscription"/> information to use to trigger an event.</param>
        /// <param name="stateVariables">The state variables to include with the event.</param>
        /// <returns>A Task.</returns>
        private async Task TriggerEvent(EventSubscription subscription, IDictionary<string, string> stateVariables)
        {
            var builder = new StringBuilder(1024);

            builder.Append("<?xml version=\"1.0\"?><e:propertyset xmlns:e=\"urn:schemas-upnp-org:event-1-0\">");
            foreach (var key in stateVariables.Keys)
            {
                builder.Append("<e:property>")
                    .Append('<')
                    .Append(key)
                    .Append('>')
                    .Append(stateVariables[key])
                    .Append("</")
                    .Append(key)
                    .Append('>')
                    .Append("</e:property>");
            }

            builder.Append("</e:propertyset>");

            using var options = new HttpRequestMessage(new HttpMethod("NOTIFY"), subscription.CallbackUrl)
            {
                Content = new StringContent(builder.ToString(), Encoding.UTF8, MediaTypeNames.Text.Xml)
            };

            options.Headers.TryAddWithoutValidation("NT", subscription.NotificationType);
            options.Headers.TryAddWithoutValidation("NTS", "upnp:propchange");
            options.Headers.TryAddWithoutValidation("SID", subscription.Id);
            options.Headers.TryAddWithoutValidation("SEQ", subscription.TriggerCount.ToString(_usCulture));

            try
            {
                using var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                    .SendAsync(options, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                // Already logged at lower levels
            }
            finally
            {
                subscription.IncrementTriggerCount();
            }
        }

        /// <summary>
        /// Parses a SSDP formatted time string.
        /// </summary>
        /// <param name="header">String to parse.</param>
        /// <returns>The value, or null if no value found.</returns>
        private int ParseTimeout(string header)
        {
            if (string.IsNullOrEmpty(header))
            {
                return DefaultTimeout;
            }

            // Starts with SECOND-
            header = header.Split('-').Last();

            if (int.TryParse(header, NumberStyles.Integer, _usCulture, out var val))
            {
                return val;
            }

            return DefaultTimeout;
        }
    }
}
