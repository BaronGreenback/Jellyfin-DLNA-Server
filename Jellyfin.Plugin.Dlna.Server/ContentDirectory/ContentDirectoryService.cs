using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.DeviceProfiles;
using Jellyfin.Plugin.Dlna.Server.Service;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Server.ContentDirectory
{
    /// <summary>
    /// Defines the <see cref="ContentDirectoryService" />.
    /// </summary>
    public class ContentDirectoryService : IUpnpService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IImageProcessor _imageProcessor;
        private readonly IUserDataManager _userDataManager;
        private readonly IDeviceProfileManager _profileManager;
        private readonly ILogger _logger;
        private readonly IUserManager _userManager;
        private readonly ILocalizationManager _localization;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IUserViewManager _userViewManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly ITVSeriesManager _tvSeriesManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentDirectoryService"/> class.
        /// </summary>
        /// <param name="profileManager">The <see cref="IProfileManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="userDataManager">The <see cref="IUserDataManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="imageProcessor">The <see cref="IImageProcessor"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="libraryManager">The <see cref="ILibraryManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="userManager">The <see cref="IUserManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="logger">The <see cref="ILogger"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="localization">The <see cref="ILocalizationManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="mediaSourceManager">The <see cref="IMediaSourceManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="userViewManager">The <see cref="IUserViewManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="mediaEncoder">The <see cref="IMediaEncoder"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="tvSeriesManager">The <see cref="ITVSeriesManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        public ContentDirectoryService(
            IDeviceProfileManager profileManager,
            IUserDataManager userDataManager,
            IImageProcessor imageProcessor,
            ILibraryManager libraryManager,
            IUserManager userManager,
            ILogger logger,
            ILocalizationManager localization,
            IMediaSourceManager mediaSourceManager,
            IUserViewManager userViewManager,
            IMediaEncoder mediaEncoder,
            ITVSeriesManager tvSeriesManager)
        {
            _profileManager = profileManager;
            _logger = logger;
            _userDataManager = userDataManager;
            _imageProcessor = imageProcessor;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _localization = localization;
            _mediaSourceManager = mediaSourceManager;
            _userViewManager = userViewManager;
            _mediaEncoder = mediaEncoder;
            _tvSeriesManager = tvSeriesManager;
        }

        /// <summary>
        /// Gets the system id. (A unique id which changes on when our definition changes.)
        /// </summary>
        private static int SystemUpdateId
        {
            get
            {
                var now = DateTime.UtcNow;
                return now.Year + now.DayOfYear + now.Hour;
            }
        }

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Params from API")]
        public async Task<string> GetServiceXml(HttpRequest request, HttpResponse response)
        {
            var resourceName = GetType().Namespace + ".ContentDirectory.xml";
            var stream = typeof(DlnaServerManager).Assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new NullReferenceException(nameof(stream));
            }

            using var stringStream = new StreamReader(stream);
            var reply = await stringStream.ReadToEndAsync().ConfigureAwait(false);

            if (DlnaServerPlugin.Instance!.Configuration.EnableDebugLog)
            {
                _logger.LogDebug("->{Address}: {Reply}", request.HttpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback, reply);
            }

            response.Headers.Add("Cache-Control", "public, max-age=86400");
            // response.Headers.Add("Content-Length", reply.Length.ToString());
            return reply;
        }

        /// <inheritdoc />
        public Task<ControlResponse> ProcessControlRequestAsync(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var requestedUrl = $"{request.Scheme}://{request.Host}{request.Path}";
            var index = requestedUrl.IndexOf("/dlna/", StringComparison.OrdinalIgnoreCase);
            var profile = _profileManager.GetOrCreateProfile(
                request.Headers,
                request.HttpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback);
            var serverAddress = requestedUrl[..index];

            var user = GetUser(profile);
            if (user == null)
            {
                throw new ArgumentNullException($"DLNA user not defined, or unable to extract user details from {DlnaServerPlugin.Instance!.Configuration.DlnaServerName}");
            }

            return new ControlHandler(
                _logger,
                _libraryManager,
                profile,
                serverAddress,
                null,
                _imageProcessor,
                _userDataManager,
                user,
                SystemUpdateId,
                _localization,
                _mediaSourceManager,
                _userViewManager,
                _mediaEncoder,
                _tvSeriesManager)
                .ProcessControlRequestAsync(request, true);
        }

        /// <summary>
        /// Get the user to use for DLNA.
        /// </summary>
        /// <param name="profile">The <see cref="DeviceProfile"/> to use.</param>
        /// <returns>The <see cref="User"/>.</returns>
        private User? GetUser(DeviceProfile profile)
        {
            var userId = string.IsNullOrEmpty(profile.UserId) ? DlnaServerPlugin.Instance!.Configuration.DefaultUserId : profile.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                // If not set up DLNA should fail, a user shouldn't be auto select for security purposes.
                _logger.LogError("DLNA is not setup to use a user account.");
                return null;
            }

            return _userManager.GetUserById(Guid.Parse(userId));
        }
    }
}
