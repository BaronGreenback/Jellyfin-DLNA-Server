using MediaBrowser.Common.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.DlnaServer.Registrator
{
    /// <summary>
    /// Defines the <see cref="DlnaServerRegistrator" />.
    /// </summary>
    public class DlnaServerRegistrator : IPluginServiceRegistrator
    {
        /// <summary>
        /// Registers Plugin Services with the DI.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IDlnaServerManager, DlnaServerManager>();
        }
    }
}
