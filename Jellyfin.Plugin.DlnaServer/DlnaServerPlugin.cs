using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.DlnaServer.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.DlnaServer
{
    /// <summary>
    /// Defines the <see cref="DlnaServerPlugin" />.
    /// </summary>
    public class DlnaServerPlugin : BasePlugin<DlnaServerConfiguration>, IHasWebPages
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DlnaServerPlugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">The <see cref="IApplicationPaths"/>.</param>
        /// <param name="xmlSerializer">The <see cref="IXmlSerializer"/>.</param>
        public DlnaServerPlugin(
            IApplicationPaths applicationPaths,
            IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the static instance for this class.
        /// </summary>
        public static DlnaServerPlugin? Instance { get; private set; }

        /// <summary>
        /// Gets the Name.
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
                    Name = this.Name,
                    EnableInMainMenu = true,
                    EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
                }
            };
        }
    }
}
