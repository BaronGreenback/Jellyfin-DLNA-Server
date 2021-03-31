using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Dlna.Didl;
using Jellyfin.Plugin.Dlna.Server.Service;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;
using Book = MediaBrowser.Controller.Entities.Book;
using Episode = MediaBrowser.Controller.Entities.TV.Episode;
using Genre = MediaBrowser.Controller.Entities.Genre;
using Movie = MediaBrowser.Controller.Entities.Movies.Movie;
using MusicAlbum = MediaBrowser.Controller.Entities.Audio.MusicAlbum;
using Series = MediaBrowser.Controller.Entities.TV.Series;

namespace Jellyfin.Plugin.Dlna.Server.ContentDirectory
{
    /// <summary>
    /// Defines the <see cref="ControlHandler" />.
    /// </summary>
    public class ControlHandler : BaseControlHandler
    {
        private const string NsDc = "http://purl.org/dc/elements/1.1/";
        private const string NsDidl = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
        private const string NsDlna = "urn:schemas-dlna-org:metadata-1-0/";
        private const string NsUpnp = "urn:schemas-upnp-org:metadata-1-0/upnp/";

        private const int TvPageSize = 50;
        private const int ContinueWatchingPageSize = 10;
        private const int FolderPageSize = int.MaxValue;

        private readonly ILibraryManager _libraryManager;
        private readonly IUserDataManager _userDataManager;
        private readonly User _user;
        private readonly IUserViewManager _userViewManager;
        private readonly ITVSeriesManager _tvSeriesManager;
        private readonly int _systemUpdateId;
        private readonly DidlBuilder _didlBuilder;
        private readonly DeviceProfile _profile;
        private readonly ILocalizationManager _localization;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlHandler"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="libraryManager">The <see cref="ILibraryManager"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="profile">The <see cref="DeviceProfile"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="serverAddress">The server address to use in this instance> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="accessToken">The <see cref="string"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="imageProcessor">The <see cref="IImageProcessor"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="userDataManager">The <see cref="IUserDataManager"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="user">The <see cref="User"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="systemUpdateId">The system id for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="localization">The <see cref="ILocalizationManager"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="mediaSourceManager">The <see cref="IMediaSourceManager"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="userViewManager">The <see cref="IUserViewManager"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="mediaEncoder">The <see cref="IMediaEncoder"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="tvSeriesManager">The <see cref="ITVSeriesManager"/> for use with the <see cref="ControlHandler"/> instance.</param>
        public ControlHandler(
            ILogger logger,
            ILibraryManager libraryManager,
            DeviceProfile profile,
            string serverAddress,
            string? accessToken,
            IImageProcessor imageProcessor,
            IUserDataManager userDataManager,
            User user,
            int systemUpdateId,
            ILocalizationManager localization,
            IMediaSourceManager mediaSourceManager,
            IUserViewManager userViewManager,
            IMediaEncoder mediaEncoder,
            ITVSeriesManager tvSeriesManager)
            : base(logger)
        {
            _libraryManager = libraryManager;
            _userDataManager = userDataManager;
            _user = user;
            _systemUpdateId = systemUpdateId;
            _userViewManager = userViewManager;
            _tvSeriesManager = tvSeriesManager;
            _profile = profile;
            _localization = localization;

            _didlBuilder = new DidlBuilder(
                profile,
                user,
                imageProcessor,
                serverAddress,
                accessToken,
                userDataManager,
                localization,
                mediaSourceManager,
                Logger,
                mediaEncoder,
                libraryManager);
        }

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Protected method")]
        protected override void WriteResult(string methodName, ControlRequestInfo methodParams, XmlWriter xmlWriter)
        {
            if (string.Equals(methodName, "Browse", StringComparison.Ordinal))
            {
                HandleBrowse(xmlWriter, methodParams.Headers);
                return;
            }

            if (string.Equals(methodName, "GetSearchCapabilities", StringComparison.Ordinal))
            {
                HandleGetSearchCapabilities(xmlWriter);
                return;
            }

            if (string.Equals(methodName, "GetSortCapabilities", StringComparison.Ordinal))
            {
                HandleGetSortCapabilities(xmlWriter);
                return;
            }

            if (string.Equals(methodName, "GetSortExtensionCapabilities", StringComparison.Ordinal))
            {
                HandleGetSortExtensionCapabilities(xmlWriter);
                return;
            }

            if (string.Equals(methodName, "GetSystemUpdateID", StringComparison.Ordinal))
            {
                HandleGetSystemUpdateId(xmlWriter);
                return;
            }

            if (methodName.EndsWith("GetFeatureList", StringComparison.Ordinal))
            {
                HandleGetFeatureList(xmlWriter);
                return;
            }

            if (string.Equals(methodName, "X_SetBookmark", StringComparison.Ordinal))
            {
                HandleXSetBookmark(methodParams.Headers);
                return;
            }

            if (string.Equals(methodName, "Search", StringComparison.Ordinal)
                || string.Equals(methodName, "X_BrowseByLetter", StringComparison.Ordinal))
            {
                HandleSearch(xmlWriter, methodParams.Headers);
                return;
            }

            throw new ResourceNotFoundException("Unexpected control request name: " + methodName);
        }

        /// <summary>
        /// Adds the "FeatureList" element to the xml document.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private static void HandleGetFeatureList(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString(
                "FeatureList",
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Features xmlns=\"urn:schemas-upnp-org:av:avs\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"urn:schemas-upnp-org:av:avs http://www.upnp.org/schemas/av/avs.xsd\"><Feature name=\"samsung.com_BASICVIEW\" version=\"1\"><container id=\"1\" type=\"object.item.imageItem\"/><container id=\"2\" type=\"object.item.audioItem\"/><container id=\"3\" type=\"object.item.videoItem\"/></Feature></Features>");
        }

        /// <summary>
        /// Adds the "SearchCaps" element to the xml document.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private static void HandleGetSearchCapabilities(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString(
                "SearchCaps",
                "res@resolution,res@size,res@duration,dc:title,dc:creator,upnp:actor,upnp:artist,upnp:genre,upnp:album,dc:date,upnp:class,@id,@refID,@protocolInfo,upnp:author,dc:description,pv:avKeywords");
        }

        /// <summary>
        /// Adds the "SortCaps" element to the xml document.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private static void HandleGetSortCapabilities(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString(
                "SortCaps",
                "res@duration,res@size,res@bitrate,dc:date,dc:title,dc:size,upnp:album,upnp:artist,upnp:albumArtist,upnp:episodeNumber,upnp:genre,upnp:originalTrackNumber,upnp:rating");
        }

        /// <summary>
        /// Adds the "SortExtensionCaps" element to the xml document.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private static void HandleGetSortExtensionCapabilities(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString(
                "SortExtensionCaps",
                "res@duration,res@size,res@bitrate,dc:date,dc:title,dc:size,upnp:album,upnp:artist,upnp:albumArtist,upnp:episodeNumber,upnp:genre,upnp:originalTrackNumber,upnp:rating");
        }

        /// <summary>
        /// Returns the value in the key of the dictionary, or defaultValue if it doesn't exist.
        /// </summary>
        /// <param name="sparams">The <see cref="IDictionary"/>.</param>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value if it's not found.</param>
        /// <returns>The key value, or the default value.</returns>
        private static string GetValueOrDefault(IDictionary<string, string> sparams, string key, string defaultValue)
        {
            return sparams.TryGetValue(key, out string? val) ? val : defaultValue;
        }

        /// <summary>
        /// Returns the child items meeting the criteria.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="search">The <see cref="SearchCriteria"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <param name="countOnly">When <c>True</c> return the count value only.</param>
        /// <returns>The <see cref="QueryResult{BaseItem}"/>.</returns>
        private static QueryResult<BaseItem> GetChildrenSorted(
            BaseItem item,
            User user,
            SearchCriteria search,
            SortCriteria sort,
            int? startIndex,
            int? limit,
            bool countOnly)
        {
            var folder = (Folder)item;

            var sortOrders = folder.IsPreSorted
                ? Array.Empty<(string, SortOrder)>()
                : new[] { (ItemSortBy.SortName, sort.SortOrder) };

            string[] mediaTypes = Array.Empty<string>();
            bool? isFolder = null;

            switch (search.SearchType)
            {
                case SearchType.Audio:
                    mediaTypes = new[] { MediaType.Audio };
                    isFolder = false;
                    break;

                case SearchType.Video:
                    mediaTypes = new[] { MediaType.Video };
                    isFolder = false;
                    break;

                case SearchType.Image:
                    mediaTypes = new[] { MediaType.Photo };
                    isFolder = false;
                    break;

                case SearchType.Playlist:
                    // items = items.OfType<MusicAlbum>();
                case SearchType.MusicAlbum:
                    // items = items.OfType<Playlist>();
                    isFolder = true;
                    break;

                case SearchType.Unknown:
                    break;

                default:
                    throw new ArgumentOutOfRangeException("Invalid Search Type", nameof(search.SearchType));
            }

            return folder.GetItems(new InternalItemsQuery
            {
                Limit = limit,
                StartIndex = startIndex,
                OrderBy = sortOrders,
                User = user,
                Recursive = true,
                IsMissing = false,
                ExcludeItemTypes = new[] { nameof(Book) },
                IsFolder = isFolder,
                MediaTypes = mediaTypes,
                DtoOptions = new(true)
            });
        }

        /// <summary>
        /// Converts a <see cref="BaseItem"/> array into a <see cref="QueryResult{ServerItem}"/>.
        /// </summary>
        /// <param name="result">An array of <see cref="BaseItem"/>.</param>
        /// <param name="countOnly">When <c>True</c> return the count value only.</param>
        /// <returns>A <see cref="QueryResult{ServerItem}"/>.</returns>
        private static QueryResult<ServerItem> ToResult(IReadOnlyCollection<BaseItem> result, bool countOnly)
        {
            return new QueryResult<ServerItem>
            {
                TotalRecordCount = result.Count,
                Items = countOnly ? null : result.Select(i => new ServerItem(i)).ToArray()
            };
        }

        /// <summary>
        /// Encapsulates a <see cref="QueryResult{BaseItem}"/> into a <see cref="QueryResult{ServerItem}"/>.
        /// </summary>
        /// <param name="result">A <see cref="QueryResult{BaseItem}"/>.</param>
        /// <param name="stubType">A <see cref="StubType"/>.</param>
        /// <param name="countOnly">When <c>True</c> return the count value only.</param>

        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private static QueryResult<ServerItem> ToResult(QueryResult<BaseItem> result, StubType? stubType, bool countOnly)
        {
            return new QueryResult<ServerItem>
            {
                TotalRecordCount = result.TotalRecordCount,
                Items = countOnly ? null : result.Items.Select(i => new ServerItem(i, i is Audio ? null : stubType)).ToArray()
            };
        }

        /// <summary>
        /// Encapsulates a <see cref="QueryResult{BaseItem}"/> into a <see cref="QueryResult{ServerItem}"/>.
        /// </summary>
        /// <param name="result">A <see cref="QueryResult{BaseItem}"/>.</param>
        /// <param name="countOnly">When <c>True</c> return the count value only.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private static QueryResult<ServerItem> ToResult(QueryResult<BaseItem> result, bool countOnly)
        {
            if (countOnly)
            {
                return new QueryResult<ServerItem>
                {
                    TotalRecordCount = result.TotalRecordCount
                };
            }

            return new QueryResult<ServerItem>
            {
                TotalRecordCount = result.TotalRecordCount,
                Items = result.Items.Select(i => new ServerItem(i, null)).ToArray()
            };
        }

        /// <summary>
        /// Sets the sorting method on a query.
        /// </summary>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="isPreSorted">True if pre-sorted.</param>
        private static void SetSorting(InternalItemsQuery query, SortCriteria? sort, bool isPreSorted)
        {
            query.OrderBy = (isPreSorted || sort == null)
                ? Array.Empty<(string, SortOrder)>()
                : new[]
                    {
                        (ItemSortBy.SortName, sort.SortOrder)
                    };
        }

        /// <summary>
        /// Adds a "XSetBookmark" element to the xml document.
        /// </summary>
        /// <param name="sparams">The <see cref="IDictionary"/>.</param>
        private void HandleXSetBookmark(IDictionary<string, string> sparams)
        {
            if (!sparams.TryGetValue("ObjectID", out var id))
            {
                return;
            }

            var serverItem = GetItemFromObjectId(id);
            var item = serverItem.Item;
            var newbookmark = int.Parse(sparams["PosSecond"], CultureInfo.InvariantCulture);
            var userdata = _userDataManager.GetUserData(_user, item);
            userdata.PlaybackPositionTicks = TimeSpan.FromSeconds(newbookmark).Ticks;
            _userDataManager.SaveUserData(
                _user,
                item,
                userdata,
                UserDataSaveReason.TogglePlayed,
                CancellationToken.None);
        }

        /// <summary>
        /// Adds the "Id" element to the xml document.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private void HandleGetSystemUpdateId(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString("Id", _systemUpdateId.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Builds the "Browse" xml response.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        /// <param name="sparams">The <see cref="IDictionary"/>.</param>
        private void HandleBrowse(XmlWriter xmlWriter, IDictionary<string, string> sparams)
        {
            if (!sparams.TryGetValue("ObjectID", out var id))
            {
                return;
            }

            (BaseItem item, StubType? stubType) = GetItemFromObjectId(id);

            var filter = FilterHelper.Filter(GetValueOrDefault(sparams, "Filter", "*"));
            var sortCriteria = new SortCriteria(GetValueOrDefault(sparams, "SortCriteria", string.Empty));

            int? requestedCount = null;
            if (sparams.ContainsKey("RequestedCount") && int.TryParse(sparams["RequestedCount"], out var requestedVal) && requestedVal > 0)
            {
                requestedCount = requestedVal;
            }

            int? start = 0;
            if (sparams.ContainsKey("StartingIndex") && int.TryParse(sparams["StartingIndex"], out var startVal) && startVal > 0)
            {
                start = startVal;
            }

            var builder = new Utf8StringWriter();
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                CloseOutput = false,
                OmitXmlDeclaration = true,
                ConformanceLevel = ConformanceLevel.Fragment,
                CheckCharacters = false
            };

            using var writer = XmlWriter.Create(builder, settings);

            writer.WriteStartElement(string.Empty, "DIDL-Lite", NsDidl);
            writer.WriteAttributeString("xmlns", "dc", null, NsDc);
            writer.WriteAttributeString("xmlns", "dlna", null, NsDlna);
            writer.WriteAttributeString("xmlns", "upnp", null, NsUpnp);
            DidlBuilder.WriteXmlRootAttributes(_profile, writer);

            int totalCount, provided;
            var flag = sparams["BrowseFlag"];
            Folder? parent = item.Parent;

            // Ensure the item's parent is the correct virtual one for the user.
            if (item.Parent?.Parent?.IsRoot ?? false)
            {
                parent = (Folder?)_libraryManager
                    .GetUserRootFolder()
                    .Children
                    .FirstOrDefault(p => ((Folder)p).Children.Any(p => p.Id.Equals(item.Id)));
            }

            if (string.Equals(flag, "BrowseMetadata", StringComparison.Ordinal))
            {
                totalCount = provided = 1;

                // if serverItem.StubType has a value, then display in a folder view.
                if (item.IsDisplayedAsFolder || stubType.HasValue)
                {
                    // use null in sortCriteria to signal that sorting isn't required.
                    var childCount = GetUserItems(item, stubType, _user, null, start, requestedCount).TotalRecordCount;
                    _didlBuilder.WriteFolderElement(writer, item, stubType, parent, childCount, filter, string.Equals(id, "0", StringComparison.Ordinal));
                }
                else
                {
                    _didlBuilder.WriteItemElement(writer, item, _user, parent, null, "test", filter);
                }
            }
            else if (string.Equals(flag, "BrowseDirectChildren", StringComparison.Ordinal))
            {
                var childrenResult = GetUserItems(item, stubType, _user, sortCriteria, start, requestedCount);
                totalCount = childrenResult.TotalRecordCount;
                provided = childrenResult.Items.Count;
                foreach ((var childItem, var displayStubType) in childrenResult.Items)
                {
                    // if serverItem.StubType has a value, then display in a folder view.
                    if (childItem.IsDisplayedAsFolder || displayStubType.HasValue)
                    {
                        var childCount = GetUserItems(childItem, displayStubType, _user, null, null, requestedCount).TotalRecordCount;
                        _didlBuilder.WriteFolderElement(writer, childItem, displayStubType, parent, childCount, filter);
                    }
                    else
                    {
                        _didlBuilder.WriteItemElement(writer, childItem, _user, parent, stubType, "test", filter);
                    }
                }
            }
            else
            {
                throw new ArgumentException("Invalid BrowseFlag value.");
            }

            writer.WriteFullEndElement();
            writer.Flush();

            try
            {
                xmlWriter.WriteElementString("Result", builder.ToString());
            }
            catch (ArgumentException ex)
            {
                Logger.LogError(ex, "Error whilst adding {Didl}", builder.ToString());
            }

            xmlWriter.WriteElementString("NumberReturned", provided.ToString(CultureInfo.InvariantCulture));
            xmlWriter.WriteElementString("TotalMatches", totalCount.ToString(CultureInfo.InvariantCulture));
            xmlWriter.WriteElementString("UpdateID", _systemUpdateId.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Builds a response to the "Search" request.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        /// <param name="sparams">The <see cref="IDictionary"/>.</param>
        private void HandleSearch(XmlWriter xmlWriter, IDictionary<string, string> sparams)
        {
            var searchCriteria = new SearchCriteria(GetValueOrDefault(sparams, "SearchCriteria", string.Empty));
            var sortCriteria = new SortCriteria(GetValueOrDefault(sparams, "SortCriteria", string.Empty));
            var filter = FilterHelper.Filter(GetValueOrDefault(sparams, "Filter", "*"));

            // sort example: dc:title, dc:date

            // Default to null instead of 0
            // Upnp inspector sends 0 as requestedCount when it wants everything
            int? requestedCount = null;
            int? start = null;

            if (sparams.ContainsKey("RequestedCount") && int.TryParse(sparams["RequestedCount"], out var requestedVal) && requestedVal > 0)
            {
                requestedCount = requestedVal;
            }

            if (sparams.ContainsKey("StartingIndex") && int.TryParse(sparams["StartingIndex"], out var startVal) && startVal > 0)
            {
                start = startVal;
            }

            using var builder = new Utf8StringWriter();
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                CloseOutput = false,
                OmitXmlDeclaration = true,
                ConformanceLevel = ConformanceLevel.Fragment,
                CheckCharacters = false
            };

            using var writer = XmlWriter.Create(builder, settings);

            writer.WriteStartElement(string.Empty, "DIDL-Lite", NsDidl);
            writer.WriteAttributeString("xmlns", "dc", null, NsDc);
            writer.WriteAttributeString("xmlns", "dlna", null, NsDlna);
            writer.WriteAttributeString("xmlns", "upnp", null, NsUpnp);

            DidlBuilder.WriteXmlRootAttributes(_profile, writer);
            var serverItem = GetItemFromObjectId(sparams["ContainerID"]);
            Folder item = (Folder)serverItem.Item;

            var childrenResult = GetChildrenSorted(item, _user, searchCriteria, sortCriteria, start, requestedCount, false);

            // Ensure the item's parent is the correct virtual one for the user.
            if (item.Parent?.Parent?.IsRoot ?? false)
            {
                item = (Folder?)_libraryManager
                    .GetUserRootFolder()
                    .Children
                    .FirstOrDefault(p => string.Equals(p.Name, item.Name, StringComparison.Ordinal)
                        && ((Folder)p).Children.Any(p => p.Id.Equals(item.Id))) ?? (Folder)serverItem.Item;
            }

            foreach (var i in childrenResult.Items)
            {
                if (i.IsDisplayedAsFolder)
                {
                    var childCount = GetChildrenSorted(i, _user, searchCriteria, sortCriteria, null, 0, true).TotalRecordCount;
                    _didlBuilder.WriteFolderElement(writer, i, null, item, childCount, filter);
                }
                else
                {
                    _didlBuilder.WriteItemElement(writer, i, _user, item, serverItem.StubType, "test", filter);
                }
            }

            writer.WriteFullEndElement();
            writer.Flush();

            try
            {
                xmlWriter.WriteElementString("Result", builder.ToString());
            }
            catch (ArgumentException ex)
            {
                Logger.LogError(ex, "Error whilst adding {Didl}", builder.ToString());
            }

            xmlWriter.WriteElementString("NumberReturned", childrenResult.Items.Count.ToString(CultureInfo.InvariantCulture));
            xmlWriter.WriteElementString("TotalMatches", childrenResult.TotalRecordCount.ToString(CultureInfo.InvariantCulture));
            xmlWriter.WriteElementString("UpdateID", _systemUpdateId.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Returns the User items meeting the criteria.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="stubType">The <see cref="StubType"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>, or <c>null</c> if count request.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetUserItems(BaseItem item, StubType? stubType, User user, SortCriteria? sort, int? startIndex, int? limit)
        {
            var countOnly = sort == null;

            if (!stubType.HasValue || stubType.Value != StubType.Folder)
            {
                switch (item)
                {
                    case MusicGenre:
                        return GetMusicGenreItems(item, stubType, item.Id, user, sort, startIndex, limit); // restrict to current library
                    case MusicArtist:
                        return GetMusicArtistItems(item, stubType, item.Id, user, sort, startIndex, limit); // restrict to current library
                    case Genre:
                        return GetGenreItems(item, stubType, item.Id, user, sort, startIndex, limit); // restrict to current library
                }

                if (item is IHasCollectionType collectionFolder)
                {
                    if (string.Equals(CollectionType.Music, collectionFolder.CollectionType, StringComparison.OrdinalIgnoreCase))
                    {
                        return GetMusicFolders(item, user, stubType, sort, startIndex, limit);
                    }

                    if (string.Equals(CollectionType.Movies, collectionFolder.CollectionType, StringComparison.OrdinalIgnoreCase))
                    {
                        return GetMovieFolders(item, user, stubType, sort, startIndex, limit);
                    }

                    if (string.Equals(CollectionType.TvShows, collectionFolder.CollectionType, StringComparison.OrdinalIgnoreCase))
                    {
                        return GetTvFolders(item, user, stubType, sort, startIndex, limit);
                    }

                    if (string.Equals(CollectionType.Folders, collectionFolder.CollectionType, StringComparison.OrdinalIgnoreCase))
                    {
                        return GetFolders(user, startIndex, limit, countOnly);
                    }

                    if (string.Equals(CollectionType.LiveTv, collectionFolder.CollectionType, StringComparison.OrdinalIgnoreCase))
                    {
                        return GetLiveTvChannels(user, sort, startIndex, limit);
                    }
                }
            }

            if (item is not Folder || (stubType.HasValue && stubType.Value != StubType.Folder))
            {
                return new QueryResult<ServerItem>
                    {
                        Items = countOnly
                            ? null
                            : (IReadOnlyList<ServerItem>)new List<ServerItem>
                            {
                                new ServerItem(item)
                            }
                    };
            }

            var query = new InternalItemsQuery(user)
            {
                Limit = limit,
                StartIndex = startIndex,
                IsVirtualItem = false,
                ExcludeItemTypes = new[] { nameof(Book) },
                IsPlaceHolder = false,
                DtoOptions = new(true),
                DisplayAlbumFolders = stubType == StubType.Folder
            };

            var folder = (Folder)item;
            if (!countOnly)
            {
                SetSorting(query, sort, folder.IsPreSorted);
            }

            return ToResult(folder.GetItems(query), stubType, countOnly);
        }

        /// <summary>
        /// Returns the Live Tv Channels meeting the criteria.
        /// </summary>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetLiveTvChannels(User user, SortCriteria? sort, int? startIndex, int? limit)
        {
            var query = new InternalItemsQuery(user)
            {
                StartIndex = startIndex,
                Limit = limit,
                IncludeItemTypes = new[] { nameof(LiveTvChannel) }
            };

            var countOnly = sort == null;
            if (!countOnly)
            {
                SetSorting(query, sort, false);
            }

            return ToResult(_libraryManager.GetItemsResult(query), countOnly);
        }

        /// <summary>
        /// Returns the music folders meeting the criteria.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="stubType">The <see cref="StubType"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicFolders(BaseItem item, User user, StubType? stubType, SortCriteria? sort, int? startIndex, int? limit)
        {
            if (stubType.HasValue)
            {
                var query = new InternalItemsQuery(user)
                {
                    StartIndex = startIndex,
                    Limit = limit
                };

                var countOnly = sort == null;
                if (!countOnly)
                {
                    SetSorting(query, sort, false);
                }

                switch (stubType.Value)
                {
                    case StubType.Latest:
                        return GetMusicLatest(item, query, countOnly);
                    case StubType.Playlists:
                        return GetItemTypes(item, query, countOnly, nameof(Playlist), false);
                    case StubType.Albums:
                        return GetItemTypes(item, query, countOnly, nameof(MusicAlbum), false);
                    case StubType.Artists:
                        return GetMusicArtists(item, query, countOnly);
                    case StubType.AlbumArtists:
                        return GetMusicAlbumArtists(item, query, countOnly);
                    case StubType.FavoriteAlbums:
                        return GetItemTypes(item, query, countOnly, nameof(MusicAlbum), true);
                    case StubType.FavoriteArtists:
                        return GetFavoriteArtists(item, query, countOnly);
                    case StubType.FavoriteSongs:
                        return GetItemTypes(item, query, countOnly, nameof(Audio), true);
                    case StubType.Songs:
                        return GetItemTypes(item, query, countOnly, nameof(Audio), false);
                    case StubType.Genres:
                        return GetMusicGenres(item, query, countOnly);
                    default:
                        Logger.LogError("GetMusicFolder received request for content type {StubType}:", stubType.Value);
                        break;
                }
            }

            var items = new[]
            {
                new ServerItem(item, StubType.Latest),
                new ServerItem(item, StubType.Playlists),
                new ServerItem(item, StubType.Albums),
                new ServerItem(item, StubType.AlbumArtists),
                new ServerItem(item, StubType.Artists),
                new ServerItem(item, StubType.Songs),
                new ServerItem(item, StubType.Genres),
                new ServerItem(item, StubType.FavoriteArtists),
                new ServerItem(item, StubType.FavoriteAlbums),
                new ServerItem(item, StubType.FavoriteSongs)
            };

            return new QueryResult<ServerItem>(limit.HasValue ? items.Take(limit.Value).ToArray() : items);
        }

        /// <summary>
        /// Returns the movie folders meeting the criteria.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="stubType">The <see cref="StubType"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMovieFolders(BaseItem item, User user, StubType? stubType, SortCriteria? sort, int? startIndex, int? limit)
        {
            if (stubType.HasValue)
            {
                var query = new InternalItemsQuery(user)
                {
                    StartIndex = startIndex,
                    Limit = limit,
                };

                var countOnly = sort == null;
                if (!countOnly)
                {
                    SetSorting(query, sort, false);
                }

                switch (stubType.Value)
                {
                    case StubType.ContinueWatching:
                        return GetMovieContinueWatching(item, query, countOnly);
                    case StubType.Latest:
                        return GetMovieLatest(item, query, countOnly);
                    case StubType.Movies:
                        return GetItemTypes(item, query, countOnly, nameof(Movie), false);
                    case StubType.Collections:
                        return GetItemTypes(null, query, countOnly, nameof(BoxSet), false);
                    case StubType.Favorites:
                        return GetItemTypes(item, query, countOnly, nameof(Movie), true);
                    case StubType.Genres:
                        return GetGenres(item, query, countOnly);
                    default:
                        Logger.LogError("GetMovieFolders received request for content type {StubType}:", stubType.Value);
                        break;
                }
            }

            var items = new[]
            {
                new ServerItem(item, StubType.ContinueWatching),
                new ServerItem(item, StubType.Latest),
                new ServerItem(item, StubType.Movies),
                new ServerItem(item, StubType.Collections),
                new ServerItem(item, StubType.Favorites),
                new ServerItem(item, StubType.Genres)
            };

            return new QueryResult<ServerItem>(limit.HasValue ? items.Take(limit.Value).ToArray() : items);
        }

        /// <summary>
        /// Returns the folders meeting the criteria.
        /// </summary>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetFolders(User user, int? startIndex, int? limit, bool countOnly)
        {
            var folders = _libraryManager.GetUserRootFolder().GetChildren(user, true)
                .OrderBy(i => i.SortName)
                .Skip(startIndex ?? 0)
                .Take(limit ?? FolderPageSize);

            if (countOnly)
            {
                return new QueryResult<ServerItem>
                {
                    TotalRecordCount = folders.Count()
                };
            }

            var list = folders
                .Select(i => new ServerItem(i, StubType.Folder))
                .ToArray();

            return new QueryResult<ServerItem>
            {
                TotalRecordCount = list.Length,
                Items = list,
            };
        }

        /// <summary>
        /// Returns the TV folders meeting the criteria.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="stubType">The <see cref="StubType"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetTvFolders(BaseItem item, User user, StubType? stubType, SortCriteria? sort, int? startIndex, int? limit)
        {
            if (stubType.HasValue)
            {
                var query = new InternalItemsQuery(user)
                {
                    StartIndex = startIndex,
                    Limit = limit,
                };

                var countOnly = sort == null;
                if (!countOnly)
                {
                    SetSorting(query, sort, false);
                }

                switch (stubType.Value)
                {
                    case StubType.ContinueWatching:
                        return GetMovieContinueWatching(item, query, countOnly);
                    case StubType.NextUp:
                        return GetNextUp(item, query, countOnly);
                    case StubType.Latest:
                        return GetTvLatest(item, query, countOnly);
                    case StubType.Series:
                        return GetItemTypes(item, query, countOnly, nameof(Series), false);
                    case StubType.FavoriteSeries:
                        return GetItemTypes(item, query, countOnly, nameof(Series), true);
                    case StubType.FavoriteEpisodes:
                        return GetItemTypes(item, query, countOnly, nameof(Episode), true);
                    case StubType.Genres:
                        return GetGenres(item, query, countOnly);
                    default:
                        Logger.LogError("GetTvFolder received request for content type {StubType}:", stubType.Value);
                        break;
                }
            }

            var items = new[]
            {
                new ServerItem(item, StubType.ContinueWatching),
                new ServerItem(item, StubType.NextUp),
                new ServerItem(item, StubType.Latest),
                new ServerItem(item, StubType.Series),
                new ServerItem(item, StubType.FavoriteSeries),
                new ServerItem(item, StubType.FavoriteEpisodes),
                new ServerItem(item, StubType.Genres)
            };

            return new QueryResult<ServerItem>(limit.HasValue ? items.Take(limit.Value).ToArray() : items);
        }

        /// <summary>
        /// Returns the Movies that are part watched that meet the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMovieContinueWatching(BaseItem parent, InternalItemsQuery query, bool countOnly)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.IsResumable = true;
            query.Limit ??= ContinueWatchingPageSize;
            query.OrderBy = new[]
            {
                (ItemSortBy.DatePlayed, SortOrder.Descending),
                (ItemSortBy.SortName, SortOrder.Ascending)
            };

            return ToResult(_libraryManager.GetItemsResult(query), countOnly);
        }

        /// <summary>
        /// Returns the folders meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetItemTypes(BaseItem? parent, InternalItemsQuery query, bool countOnly, string includeItems, bool favourite)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.IsFavorite = favourite;
            query.IncludeItemTypes = new[] { includeItems };

            return ToResult(_libraryManager.GetItemsResult(query), countOnly);
        }

        /// <summary>
        /// Returns the genres meeting the criteria.
        /// The GetGenres.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetGenres(BaseItem parent, InternalItemsQuery query, bool countOnly)
        {
            var genresResult = _libraryManager.GetGenres(new InternalItemsQuery(query.User)
            {
                AncestorIds = new[] { parent.Id },
                StartIndex = query.StartIndex,
                Limit = query.Limit
            });

            var result = new QueryResult<BaseItem>
            {
                TotalRecordCount = genresResult.TotalRecordCount,
                Items = genresResult.Items.Select(i => i.Item1).ToArray()
            };

            return ToResult(result, countOnly);
        }

        /// <summary>
        /// Returns the music genres meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicGenres(BaseItem parent, InternalItemsQuery query, bool countOnly)
        {
            var genresResult = _libraryManager.GetMusicGenres(new InternalItemsQuery(query.User)
            {
                AncestorIds = new[] { parent.Id },
                StartIndex = query.StartIndex,
                Limit = query.Limit
            });

            if (countOnly)
            {
                return new QueryResult<ServerItem>
                {
                    TotalRecordCount = genresResult.TotalRecordCount
                };
            }

            var result = new QueryResult<BaseItem>
            {
                TotalRecordCount = genresResult.TotalRecordCount,
                Items = genresResult.Items.Select(i => i.Item1).ToArray()
            };

            return ToResult(result, false);
        }

        /// <summary>
        /// Returns the music albums by artist that meet the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicAlbumArtists(BaseItem parent, InternalItemsQuery query, bool countOnly)
        {
            var artists = _libraryManager.GetAlbumArtists(new InternalItemsQuery(query.User)
            {
                AncestorIds = new[] { parent.Id },
                StartIndex = query.StartIndex,
                Limit = query.Limit
            });

            if (countOnly)
            {
                return new QueryResult<ServerItem>
                {
                    TotalRecordCount = artists.TotalRecordCount
                };
            }

            var result = new QueryResult<BaseItem>
            {
                TotalRecordCount = artists.TotalRecordCount,
                Items = artists.Items.Select(i => i.Item1).ToArray()
            };

            return ToResult(result, false);
        }

        /// <summary>
        /// Returns the music artists meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicArtists(BaseItem parent, InternalItemsQuery query, bool countOnly)
        {
            var artists = _libraryManager.GetArtists(new InternalItemsQuery(query.User)
            {
                AncestorIds = new[] { parent.Id },
                StartIndex = query.StartIndex,
                Limit = query.Limit
            });

            if (countOnly)
            {
                return new QueryResult<ServerItem>
                {
                    TotalRecordCount = artists.TotalRecordCount
                };
            }

            var result = new QueryResult<BaseItem>
            {
                TotalRecordCount = artists.TotalRecordCount,
                Items = artists.Items.Select(i => i.Item1).ToArray()
            };

            return ToResult(result, false);
        }

        /// <summary>
        /// Returns the artists tagged as favourite that meet the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetFavoriteArtists(BaseItem parent, InternalItemsQuery query, bool countOnly)
        {
            var artists = _libraryManager.GetArtists(new InternalItemsQuery(query.User)
            {
                AncestorIds = new[] { parent.Id },
                StartIndex = query.StartIndex,
                Limit = query.Limit,
                IsFavorite = true
            });

            if (countOnly)
            {
                return new QueryResult<ServerItem>
                {
                    TotalRecordCount = artists.TotalRecordCount
                };
            }

            var result = new QueryResult<BaseItem>
            {
                TotalRecordCount = artists.TotalRecordCount,
                Items = artists.Items.Select(i => i.Item1).ToArray()
            };

            return ToResult(result, false);
        }

        /// <summary>
        /// Returns the music playlists meeting the criteria.
        /// </summary>
        /// <param name="query">The query<see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicPlaylists(InternalItemsQuery query, bool countOnly)
        {
            query.Parent = null;
            query.IncludeItemTypes = new[] { nameof(Playlist) };
            query.Recursive = true;

            var result = _libraryManager.GetItemsResult(query);
            return ToResult(result, countOnly);
        }

        /// <summary>
        /// Returns the latest music meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicLatest(BaseItem parent, InternalItemsQuery query, bool countOnly)
        {
            query.OrderBy = Array.Empty<(string, SortOrder)>();

            var items = _userViewManager.GetLatestItems(
                new LatestItemsQuery
                {
                    UserId = query.User!.Id,
                    Limit = query.Limit ?? 50,
                    IncludeItemTypes = new[] { nameof(Audio) },
                    ParentId = parent.Id,
                    GroupItems = true
                },
                query.DtoOptions);

            if (countOnly)
            {
                return new QueryResult<ServerItem>
                {
                    TotalRecordCount = items.Count
                };
            }

            var list = items.Select(i => i.Item1 ?? i.Item2.FirstOrDefault())
                .Where(i => i != null)
                .ToArray();

            return ToResult(list!, false);
        }

        /// <summary>
        /// Returns the next up item meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetNextUp(BaseItem parent, InternalItemsQuery query, bool countOnly)
        {
            query.OrderBy = Array.Empty<(string, SortOrder)>();

            var result = _tvSeriesManager.GetNextUp(
                new NextUpQuery
                {
                    Limit = query.Limit,
                    StartIndex = query.StartIndex,
                    UserId = query.User?.Id ?? Guid.Empty
                },
                new[] { parent },
                query.DtoOptions);

            return ToResult(result, countOnly);
        }

        /// <summary>
        /// Returns the latest tv meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetTvLatest(BaseItem parent, InternalItemsQuery query, bool countOnly)
        {
            query.OrderBy = Array.Empty<(string, SortOrder)>();

            var items = _userViewManager.GetLatestItems(
                new LatestItemsQuery
                {
                    UserId = query.User!.Id,
                    Limit = query.Limit ?? TvPageSize,
                    IncludeItemTypes = new[] { nameof(Episode) },
                    ParentId = parent.Id,
                    GroupItems = false
                },
                query.DtoOptions);

            if (countOnly)
            {
                return new QueryResult<ServerItem>
                {
                    TotalRecordCount = items.Count
                };
            }

            var list = items
                .Select(i => i.Item1 ?? i.Item2.FirstOrDefault())
                .Where(i => i != null)
                .ToArray();

            return ToResult(list!, false);
        }

        /// <summary>
        /// Returns the latest movies meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMovieLatest(BaseItem parent, InternalItemsQuery query, bool countOnly)
        {
            query.OrderBy = Array.Empty<(string, SortOrder)>();

            var items = _userViewManager.GetLatestItems(
                new LatestItemsQuery
                {
                    UserId = query.User!.Id,
                    Limit = query.Limit ?? 50,
                    IncludeItemTypes = new[] { nameof(Movie) },
                    ParentId = parent.Id,
                    GroupItems = true
                },
                query.DtoOptions);

            if (countOnly)
            {
                return new QueryResult<ServerItem>
                {
                    TotalRecordCount = items.Count
                };
            }

            var list = items.Select(i => i.Item1 ?? i.Item2.FirstOrDefault())
                .Where(i => i != null)
                .ToArray();

            return ToResult(list!, false);
        }

        /// <summary>
        /// Returns music artist items that meet the criteria.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="stubType">The <see cref="StubType"/>.</param>
        /// <param name="parentId">The <see cref="Guid"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicArtistItems(
            BaseItem item,
            StubType? stubType,
            Guid parentId,
            User user,
            SortCriteria? sort,
            int? startIndex,
            int? limit)
        {
            var query = new InternalItemsQuery(user)
            {
                Recursive = true,
                ParentId = parentId,
                ArtistIds = new[] { item.Id },
                IncludeItemTypes = (stubType == StubType.Folder)
                    ? new[] { nameof(MusicAlbum), nameof(Folder) }
                    : new[] { nameof(MusicAlbum) },
                Limit = limit,
                StartIndex = startIndex,
                DtoOptions = new(true)
            };

            bool countOnly = sort == null;
            if (!countOnly)
            {
                SetSorting(query, sort, false);
            }

            var result = _libraryManager.GetItemsResult(query);
            return ToResult(result, stubType, countOnly);
        }

        /// <summary>
        /// Returns the genre items meeting the criteria.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="stubType">The <see cref="StubType"/>.</param>
        /// <param name="parentId">The <see cref="Guid"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetGenreItems(
            BaseItem item,
            StubType? stubType,
            Guid parentId,
            User user,
            SortCriteria? sort,
            int? startIndex,
            int? limit)
        {
            var query = new InternalItemsQuery(user)
            {
                Recursive = true,
                ParentId = parentId,
                GenreIds = new[] { item.Id },
                IncludeItemTypes = new[]
                {
                    nameof(Movie),
                    nameof(Series)
                },
                Limit = limit,
                StartIndex = startIndex,
                DtoOptions = new(true)
            };

            var countOnly = sort == null;
            if (!countOnly)
            {
                SetSorting(query, sort, false);
            }

            var result = _libraryManager.GetItemsResult(query);
            return ToResult(result, stubType, countOnly);
        }

        /// <summary>
        /// Returns the music genre items meeting the criteria.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="stubType">The <see cref="StubType"/>.</param>
        /// <param name="parentId">The <see cref="Guid"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicGenreItems(BaseItem item, StubType? stubType, Guid parentId, User user, SortCriteria? sort, int? startIndex, int? limit)
        {
            var query = new InternalItemsQuery(user)
            {
                Recursive = true,
                ParentId = parentId,
                IncludeItemTypes = (stubType == StubType.Folder)
                    ? new[] { nameof(MusicAlbum), nameof(Folder) }
                    : new[] { nameof(MusicAlbum) },
                GenreIds = new[] { item.Id },
                Limit = limit,
                StartIndex = startIndex,
                DtoOptions = new(true)
            };

            bool countOnly = sort == null;
            if (!countOnly)
            {
                SetSorting(query, sort, false);
            }

            var result = _libraryManager.GetItemsResult(query);
            return ToResult(result, stubType, countOnly);
        }

        /// <summary>
        /// Retrieves the ServerItem id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>The <see cref="ServerItem"/>.</returns>
        private ServerItem GetItemFromObjectId(string id)
        {
            if (DidlBuilder.IsIdRoot(id))
            {
                return new ServerItem(_libraryManager.GetUserRootFolder());
            }

            StubType? stubType = null;

            // After using PlayTo, MediaMonkey sends a request to the server trying to get item info
            const string ParamsSrch = "Params=";
            var paramsIndex = id.IndexOf(ParamsSrch, StringComparison.OrdinalIgnoreCase);
            if (paramsIndex != -1)
            {
                id = id[(paramsIndex + ParamsSrch.Length)..];
                var parts = id.Split(';');
                id = parts[23];
            }

            var special = id.Split('_', 2);
            if (special.Length == 2)
            {
                if (Enum.TryParse<StubType>(special[0], true, out var result))
                {
                    id = special[1];
                    stubType = result;
                }
            }

            if (Guid.TryParse(id, out var itemId))
            {
                return new ServerItem(_libraryManager.GetItemById(itemId), stubType);
            }

            Logger.LogError("Error parsing item Id: {Id}. Returning user root folder.", id);

            return new ServerItem(_libraryManager.GetUserRootFolder());
        }
    }
}
