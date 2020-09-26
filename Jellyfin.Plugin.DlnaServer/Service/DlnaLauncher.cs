using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Plugins;

namespace Jellyfin.Plugin.DlnaServer.Service
{
    /// <summary>
    /// Defines the <see cref="DlnaLauncher"/> class.
    /// </summary>
    public class DlnaLauncher : IServerEntryPoint
    {
        private readonly IDlnaServerManager _manager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DlnaLauncher"/> class.
        /// </summary>
        /// <param name="manager">A <see cref="IDlnaServerManager"/> instance.</param>
        public DlnaLauncher(IDlnaServerManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// Starts the DLNA server.
        /// </summary>
        /// <returns>.</returns>
        public async Task RunAsync()
        {
            await _manager.Start().ConfigureAwait(false);
        }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            _manager?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
