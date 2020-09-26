using System.Text.Json.Serialization;
using Jellyfin.Plugin.Ssdp.Configuration;
using Jellyfin.Plugin.Ssdp.Model;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.DlnaServer.Configuration
{
    /// <summary>
    /// Defines the <see cref="DlnaServerConfiguration" />.
    /// </summary>
    public class DlnaServerConfiguration : BasePluginConfiguration
    {
        private IConfigurationManager? _config;

        /// <summary>
        /// Gets or sets a value indicating whether detailed dlna server logs are sent to the console/log.
        /// If the setting "DlnaServer": "Debug" must be set in logging.default.json for this property to work.
        /// </summary>
        public bool EnableDebugLog { get; set; }

        /// <summary>
        /// Gets or sets the default user account that the dlna server uses.
        /// </summary>
        public string? DefaultUserId { get; set; }

        /// <summary>
        /// Gets or sets the frequency at which SSDP alive notifications are transmitted.
        /// </summary>
        public int AliveMessageIntervalSeconds { get; set; } = 1800;

        /// <summary>
        /// Gets or sets a custom name for the Dlna Server.
        /// </summary>
        public string? DlnaServerName { get; set; }

        /// <summary>
        /// Gets the SSDP Configuration settings.
        /// </summary>
        [JsonIgnore]
        public SsdpConfiguration? Configuration => _config?.GetConfiguration<SsdpConfiguration>("ssdp");

        /// <summary>
        /// Gets or sets a value indicating whether detailed SSDP logs are sent to the console/log.
        /// "Emby.Dlna": "Debug" must be set in logging.default.json for this property to have any effect.
        /// </summary>
        [JsonIgnore]
        public bool EnableSsdpTracing
        {
            get
            {
                return Configuration?.EnableSsdpTracing ?? false;
            }

            set
            {
                if (Configuration != null)
                {
                    Configuration.EnableSsdpTracing = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether an IP address is to be used to filter the detailed ssdp logs that are being sent to the console/log.
        /// If the setting "Emby.Dlna": "Debug" must be set in logging.default.json for this property to work.
        /// </summary>
        [JsonIgnore]
        public string SsdpTracingFilter
        {
            get
            {
                return Configuration?.SsdpTracingFilter ?? string.Empty;
            }

            set
            {
                if (Configuration != null)
                {
                    Configuration.SsdpTracingFilter = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the range of UDP ports to use in communications as well as 1900.
        /// </summary>
        [JsonIgnore]
        public string UdpPortRange
        {
            get
            {
                return Configuration?.UdpPortRange ?? string.Empty;
            }

            set
            {
                if (Configuration != null)
                {
                    Configuration.UdpPortRange = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of times SSDP UDP messages are sent.
        /// </summary>
        [JsonIgnore]
        public int UdpSendCount
        {
            get
            {
                return Configuration?.UdpSendCount ?? 2;
            }

            set
            {
                if (Configuration != null)
                {
                    Configuration.UdpSendCount = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating the default icon width.
        /// </summary>
        public int DefaultIconWidth { get; set; } = 48;

        /// <summary>
        /// Gets or sets a value indicating the default icon height.
        /// </summary>
        public int DefaultIconHeight { get; set; } = 48;

        /// <summary>
        /// Gets or sets a value indicating whether the MSMediaReceiverRegistrar service is active.
        /// </summary>
        public bool EnableMSMediaReceiverRegistrar { get; set; }

        /// <summary>
        /// Initialises the configuration.
        /// </summary>
        /// <param name="config">The <see cref="IConfigurationManager"/>.</param>
        internal void SetConfigurationManager(IConfigurationManager config)
        {
            _config = config;
        }
    }
}
