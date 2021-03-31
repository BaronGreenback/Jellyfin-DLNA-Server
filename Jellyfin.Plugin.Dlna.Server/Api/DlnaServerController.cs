using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Threading.Tasks;
using Jellyfin.Api;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Constants;
using Jellyfin.Plugin.Dlna.Server.Service;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Drawing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Dlna.Server.Api
{
    /// <summary>
    /// Defines the <see cref="DlnaServerController"/> controller.
    /// </summary>
    /// <remarks>
    /// Statics have to be used, as using DI can cause multiple instances being created at startup.
    ///
    /// On startup, the dlna server gets assigned a new GUID.
    /// </remarks>
    [Route("Dlna")]
    [Authorize(Policy = Policies.LocalNetworkAccessPolicy)]
    public class DlnaServerController : BaseJellyfinApiController
    {
        /// <summary>
        /// Gets the XML service description.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <response code="200">Description xml returned.</response>
        /// <returns>An <see cref="OkResult"/> containing the description xml.</returns>
        [HttpGet("{serverId}/description")]
        [HttpGet("{serverId}/description.xml", Name = "GetDescriptionXml_2")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces(MediaTypeNames.Text.Xml)]
        [ProducesFile(MediaTypeNames.Text.Xml)]
        public ActionResult GetDescriptionXml([FromRoute, Required] string serverId)
        {
           return string.Equals(serverId, DlnaServerManager.Instance?.ServerString, StringComparison.Ordinal)
                ? Ok(DlnaServerManager.Instance!.GetServerDescriptionXml(Request, Response))
                : NotFound();
        }

        /// <summary>
        /// Gets the DLNA content directory.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <response code="200">Dlna content directory returned.</response>
        /// <returns>An <see cref="OkResult"/> containing the dlna content directory xml.</returns>
        [HttpGet("{serverId}/ContentDirectory")]
        [HttpGet("{serverId}/ContentDirectory")]
        [HttpGet("{serverId}/ContentDirectory/ContentDirectory", Name = "GetContentDirectory_2")]
        [HttpGet("{serverId}/ContentDirectory/ContentDirectory.xml", Name = "GetContentDirectory_3")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces(MediaTypeNames.Text.Xml)]
        [ProducesFile(MediaTypeNames.Text.Xml)]
        public async Task<ActionResult> GetContentDirectory([FromRoute, Required] string serverId)
        {
            return string.Equals(serverId, DlnaServerManager.Instance?.ServerString, StringComparison.Ordinal)
                ? Ok(await DlnaServerManager.Instance!.ContentDirectory.GetServiceXml(Request, Response).ConfigureAwait(false))
                : NotFound();
        }

        /// <summary>
        /// Gets the DLNA media receiver registrar.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <returns>Dlna media receiver registrar xml.</returns>
        [HttpGet("{serverId}/MediaReceiverRegistrar")]
        [HttpGet("{serverId}/MediaReceiverRegistrar/MediaReceiverRegistrar", Name = "GetMediaReceiverRegistrar_2")]
        [HttpGet("{serverId}/MediaReceiverRegistrar/MediaReceiverRegistrar.xml", Name = "GetMediaReceiverRegistrar_3")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces(MediaTypeNames.Text.Xml)]
        [ProducesFile(MediaTypeNames.Text.Xml)]
        public async Task<ActionResult> GetMediaReceiverRegistrar([FromRoute, Required] string serverId)
        {
            return (DlnaServerManager.Instance?.MediaReceiverRegistrar != null
                && string.Equals(serverId, DlnaServerManager.Instance?.ServerString, StringComparison.Ordinal))
                ? Ok(await DlnaServerManager.Instance!.MediaReceiverRegistrar.GetServiceXml(Request, Response).ConfigureAwait(false))
                : NotFound();
        }

        /// <summary>
        /// Gets the Dlna media receiver registrar.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <returns>Dlna media receiver registrar xml.</returns>
        [HttpGet("{serverId}/ConnectionManager")]
        [HttpGet("{serverId}/ConnectionManager/ConnectionManager", Name = "GetConnectionManager_2")]
        [HttpGet("{serverId}/ConnectionManager/ConnectionManager.xml", Name = "GetConnectionManager_3")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces(MediaTypeNames.Text.Xml)]
        [ProducesFile(MediaTypeNames.Text.Xml)]
        public async Task<ActionResult> GetConnectionManager([FromRoute, Required] string serverId)
        {
            return string.Equals(serverId, DlnaServerManager.Instance?.ServerString, StringComparison.Ordinal)
                ? Ok(await DlnaServerManager.Instance!.ConnectionManager.GetServiceXml(Request, Response).ConfigureAwait(false))
                : NotFound();
        }

        /// <summary>
        /// Processes a content directory control request.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <returns>Control response.</returns>
        [HttpPost("{serverId}/ContentDirectory/Control")]
        [Produces(MediaTypeNames.Text.Xml)]
        public async Task<ActionResult<ControlResponse>> ProcessContentDirectoryControlRequest([FromRoute, Required] string serverId)
        {
            return string.Equals(serverId, DlnaServerManager.Instance?.ServerString, StringComparison.Ordinal)
                ? await DlnaServerManager.Instance!.ContentDirectory.ProcessControlRequestAsync(Request).ConfigureAwait(false)
                : NotFound();
        }

        /// <summary>
        /// Processes a connection manager control request.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <returns>Control response.</returns>
        [HttpPost("{serverId}/ConnectionManager/Control")]
        [Produces(MediaTypeNames.Text.Xml)]
        public async Task<ActionResult<ControlResponse>> ProcessConnectionManagerControlRequest([FromRoute, Required] string serverId)
        {
            return string.Equals(serverId, DlnaServerManager.Instance?.ServerString, StringComparison.Ordinal)
                ? await DlnaServerManager.Instance!.ConnectionManager.ProcessControlRequestAsync(Request).ConfigureAwait(false)
                : NotFound();
        }

        /// <summary>
        /// Processes a media receiver registrar control request.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <returns>Control response.</returns>
        [HttpPost("{serverId}/MediaReceiverRegistrar/Control")]
        [Produces(MediaTypeNames.Text.Xml)]
        public async Task<ActionResult<ControlResponse>> ProcessMediaReceiverRegistrarControlRequest([FromRoute, Required] string serverId)
        {
            return (DlnaServerManager.Instance?.MediaReceiverRegistrar != null
                && string.Equals(serverId, DlnaServerManager.Instance?.ServerString, StringComparison.Ordinal))
                ? await DlnaServerManager.Instance!.MediaReceiverRegistrar.ProcessControlRequestAsync(Request).ConfigureAwait(false)
                : NotFound();
        }

        /// <summary>
        /// Processes an event subscription request.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <returns>Event subscription response.</returns>
        [HttpSubscribe("{serverId}/MediaReceiverRegistrar/Events")]
        [HttpUnsubscribe("{serverId}/MediaReceiverRegistrar/Events")]
        [ApiExplorerSettings(IgnoreApi = true)] // Ignore in openapi docs
        [Produces(MediaTypeNames.Text.Xml)]
        public ActionResult ProcessMediaReceiverRegistrarEventRequest([FromRoute, Required] string serverId)
        {
            if (DlnaServerManager.Instance?.MediaReceiverRegistrar != null
                && string.Equals(serverId, DlnaServerManager.Instance?.ServerString, StringComparison.Ordinal))
            {
                DlnaServerManager.Instance!.EventManager.ProcessEventRequest(Request, Response);
                return Ok();
            }

            return NotFound();
        }

        /// <summary>
        /// Processes an event subscription request.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <returns>Event subscription response.</returns>
        [HttpSubscribe("{serverId}/ContentDirectory/Events")]
        [HttpUnsubscribe("{serverId}/ContentDirectory/Events")]
        [ApiExplorerSettings(IgnoreApi = true)] // Ignore in openapi docs
        [Produces(MediaTypeNames.Text.Xml)]
        public ActionResult ProcessContentDirectoryEventRequest([FromRoute, Required] string serverId)
        {
            if (string.Equals(serverId, DlnaServerManager.Instance?.ServerString, StringComparison.Ordinal))
            {
                DlnaServerManager.Instance!.EventManager.ProcessEventRequest(Request, Response);
                return Ok();
            }

            return NotFound();
        }

        /// <summary>
        /// Processes an event subscription request.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <returns>Event subscription response.</returns>
        [HttpSubscribe("{serverId}/ConnectionManager/Events")]
        [HttpUnsubscribe("{serverId}/ConnectionManager/Events")]
        [ApiExplorerSettings(IgnoreApi = true)] // Ignore in openapi docs
        [Produces(MediaTypeNames.Text.Xml)]
        public ActionResult ProcessConnectionManagerEventRequest([FromRoute, Required] string serverId)
        {
            if (string.Equals(serverId, DlnaServerManager.Instance?.ServerString, StringComparison.Ordinal))
            {
                DlnaServerManager.Instance!.EventManager.ProcessEventRequest(Request, Response);
                return Ok();
            }

            return NotFound();
        }

        /// <summary>
        /// Gets a server icon.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <param name="fileName">The icon filename.</param>
        /// <returns>Icon stream.</returns>
        [HttpGet("{serverId}/{fileName}", Name = "GetIcons")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesImageFile]
        [Produces(MediaTypeNames.Text.Xml)]
        public ActionResult GetIconId([FromRoute, Required] string serverId, [FromRoute, Required] string fileName)
        {
            if (string.IsNullOrEmpty(fileName)
                || !string.Equals(serverId, DlnaServerManager.Instance?.ServerString, StringComparison.Ordinal))
            {
                return NotFound();
            }

            int i = fileName.LastIndexOf('.');
            if (i == -1)
            {
                return NotFound();
            }

            fileName = fileName.ToLowerInvariant();
            var ext = fileName[(i + 1)..];
            if (!Enum.TryParse<ImageFormat>(ext, true, out var format))
            {
                return NotFound();
            }

            var resource = GetType().Namespace + $".Images.{fileName}";

            // Do not put this in a using, otherwise the stream will be closed in the middleware.
#pragma warning disable CA2000 // Dispose objects before losing scope
            var icon = new ImageStream
            {
                Format = format,
                Stream = typeof(DlnaServerManager).Assembly.GetManifestResourceStream(resource)
            };
#pragma warning restore CA2000 // Dispose objects before losing scope

            return File(icon.Stream, $"image/{ext}");
        }
    }
}
