#pragma warning disable CA1822 // Mark members as static
using System;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.Ssdp;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Dlna.Server.Configuration
{
    /// <summary>
    /// Defines the <see cref="DlnaServerConfiguration" />.
    /// </summary>
    public class DlnaServerConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether detailed dlna server logs are sent to the console/log.
        /// If the setting "Dlna.Server": "Debug" must be set in logging.default.json for this property to work.
        /// </summary>
        public bool EnableDebugLog { get; set; }

        /// <summary>
        /// Gets or sets the default user account that the dlna server uses. The value can be overwritten at the per profile level.
        /// </summary>
        public string? DefaultUserId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the dlna server's identifier, used in url generation, should change between restarts.
        /// </summary>
        public bool ChangeIdOnStartup { get; set; } = true;

        /// <summary>
        /// Gets or sets the frequency at which SSDP alive notifications are transmitted.
        /// </summary>
        public int AliveMessageIntervalSeconds { get; set; } = 1800;

        /// <summary>
        /// Gets or sets a custom name for the Dlna Server.
        /// </summary>
        public string? DlnaServerName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether windows explorer support is enabled.
        /// </summary>
        public bool EnableWindowsExplorerSupport { get; set; } = true;

        /// <summary>
        /// Gets or sets the interface addresses which the DLNA server will be assigned.
        /// </summary>
        /// <remarks>
        /// If not defined, all internally bound interfaces defined in JF will be used.
        /// Interfaces specified must exist, and cannot be different to those specified in <seealso cref="Networking.Configuration.NetworkConfiguration.LocalNetworkAddresses"/>.
        /// </remarks>
        public string[] BindAddresses { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets a value indicating whether detailed SSDP logs are sent to the console/log.
        /// "Emby.Dlna": "Debug" must be set in logging.default.json for this property to have any effect.
        /// </summary>
        /// <remarks>This setting is shared across the dlna plugins.</remarks>
        [XmlIgnoreAttribute]
        public bool EnableSsdpTracing
        {
            get => SsdpConfig.EnableSsdpTracing;
            set => SsdpConfig.EnableSsdpTracing = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether an IP address is to be used to filter the detailed ssdp logs that are being sent to the console/log.
        /// If the setting "Emby.Dlna": "Debug" must be set in logging.default.json for this property to work.
        /// </summary>
        /// <remarks>This setting is shared across the dlna plugins.</remarks>
        [XmlIgnoreAttribute]
        public string SsdpTracingFilter
        {
            get => SsdpConfig.SsdpTracingFilter;
            set => SsdpConfig.SsdpTracingFilter = value;
        }

        /// <summary>
        /// Gets or sets the range of UDP ports to use in communications as well as 1900.
        /// </summary>
        /// <remarks>This setting is shared across the dlna plugins.</remarks>
        [XmlIgnoreAttribute]
        public string UdpPortRange
        {
            get => SsdpConfig.UdpPortRange;
            set => SsdpConfig.UdpPortRange = value;
        }

        /// <summary>
        /// Gets or sets the USERAGENT that is sent to devices.
        /// </summary>
        /// <remarks>This setting is shared across the dlna plugins.</remarks>
        [XmlIgnoreAttribute]
        [JsonIgnore]
        public string UserAgent
        {
            get => SsdpConfig.UserAgent;
            set => SsdpConfig.UserAgent = value;
        }

        /// <summary>
        /// Gets or sets the Dlna version that the SSDP server supports.
        /// </summary>
        /// <remarks>This setting is shared across the dlna plugins.</remarks>
        [XmlIgnore]
        [JsonIgnore]
        public DlnaVersion DlnaVersion
        {
            get => SsdpConfig.DlnaVersion;
            set => SsdpConfig.DlnaVersion = value;
        }

        private SsdpConfiguration SsdpConfig => SsdpServer.GetInstance().Configuration;
    }
}
