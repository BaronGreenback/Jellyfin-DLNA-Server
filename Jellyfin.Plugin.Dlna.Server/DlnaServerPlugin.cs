using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.Server.Configuration;
using Jellyfin.Plugin.Dlna.Ssdp;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Dlna.Server
{
    /// <summary>
    /// Defines the <see cref="DlnaLauncher"/> class.
    /// </summary>
    public class DlnaServerPlugin : BasePlugin<DlnaServerConfiguration>, IHasWebPages, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DlnaServerPlugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">The <see cref="IApplicationPaths"/> instance.</param>
        /// <param name="xmlSerializer">The <see cref="IXmlSerializer"/> instance.</param>
        public DlnaServerPlugin(
            IApplicationPaths applicationPaths,
            IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            ConfigurationChanging += UpdateSettings;
        }

        /// <summary>
        /// Gets the static instance.
        /// </summary>
        public static DlnaServerPlugin? Instance { get; private set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public override string Name => "DLNA Server";

        /// <summary>
        /// Gets the Id.
        /// </summary>
        public override Guid Id => Guid.Parse("49f39d7e-9075-414f-a664-6763ab3362f9");

        /// <summary>
        /// Returns an enumerable of web configuration pages.
        /// </summary>
        /// <returns>The <see cref="IEnumerable{PluginPageInfo}"/>.</returns>
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = this.Name, EnableInMainMenu = true, EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
                }
            };
        }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            ConfigurationChanging -= UpdateSettings;
            GC.SuppressFinalize(this);
        }

        private void UpdateSettings(object? sender, BasePluginConfiguration configuration)
        {
            var config = (DlnaServerConfiguration)configuration;
            config.AliveMessageIntervalSeconds = Math.Clamp(config.AliveMessageIntervalSeconds, 100, 65000);
            SsdpServer.Instance.UpdateConfiguration();
        }
    }
}
