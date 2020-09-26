using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using Jellyfin.Api;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Constants;
using Jellyfin.Plugin.DlnaServer.Eventing;
using Jellyfin.Plugin.DlnaServer.Service;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Drawing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.DlnaServer.Api
{
    /// <summary>
    /// Dlna Server Controller.
    /// </summary>
    [Route("Dlna")]
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class DlnaServerController : BaseJellyfinApiController
    {
        private readonly IDlnaServerManager _dlnaServerManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DlnaServerController"/> class.
        /// </summary>
        /// <param name="dlnaServerManager">Instance of the <see cref="IDlnaServerManager"/> interface.</param>
        public DlnaServerController(IDlnaServerManager dlnaServerManager)
        {
            _dlnaServerManager = dlnaServerManager;
        }

        /// <summary>
        /// Get Description Xml.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <response code="200">Description xml returned.</response>
        /// <returns>An <see cref="OkResult"/> containing the description xml.</returns>
        [HttpGet("{serverId}/description")]
        [HttpGet("{serverId}/description.xml", Name = "GetDescriptionXml_2")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces(MediaTypeNames.Text.Xml)]
        [ProducesFile(MediaTypeNames.Text.Xml)]
        [AllowAnonymous]
        public ActionResult GetDescriptionXml([FromRoute, Required] string serverId)
        {
            if (string.Equals(serverId, _dlnaServerManager.ServerId, StringComparison.OrdinalIgnoreCase))
            {
                return Ok(_dlnaServerManager.GetServerDescriptionXml(Request.Headers, serverId, Request));
            }

            return NotFound();
        }

        /// <summary>
        /// Gets Dlna content directory xml.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <response code="200">Dlna content directory returned.</response>
        /// <returns>An <see cref="OkResult"/> containing the dlna content directory xml.</returns>
        [HttpGet("{serverId}/ContentDirectory")]
        [HttpGet("{serverId}/ContentDirectory/ContentDirectory", Name = "GetContentDirectory_2")]
        [HttpGet("{serverId}/ContentDirectory/ContentDirectory.xml", Name = "GetContentDirectory_3")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces(MediaTypeNames.Text.Xml)]
        [ProducesFile(MediaTypeNames.Text.Xml)]
        [AllowAnonymous]
        public ActionResult GetContentDirectory([FromRoute, Required] string serverId)
        {
            if (string.Equals(serverId, _dlnaServerManager.ServerId, StringComparison.OrdinalIgnoreCase))
            {
                return Ok(_dlnaServerManager.ContentDirectory.GetServiceXml());
            }

            return NotFound();
        }

        /// <summary>
        /// Gets Dlna media receiver registrar xml.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <returns>Dlna media receiver registrar xml.</returns>
        [HttpGet("{serverId}/MediaReceiverRegistrar")]
        [HttpGet("{serverId}/MediaReceiverRegistrar/MediaReceiverRegistrar", Name = "GetMediaReceiverRegistrar_2")]
        [HttpGet("{serverId}/MediaReceiverRegistrar/MediaReceiverRegistrar.xml", Name = "GetMediaReceiverRegistrar_3")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces(MediaTypeNames.Text.Xml)]
        [ProducesFile(MediaTypeNames.Text.Xml)]
        [AllowAnonymous]
        public ActionResult GetMediaReceiverRegistrar([FromRoute, Required] string serverId)
        {
            if (string.Equals(serverId, _dlnaServerManager.ServerId, StringComparison.OrdinalIgnoreCase)
                && _dlnaServerManager.MediaReceiverRegistrar != null)
            {
                return Ok(_dlnaServerManager.MediaReceiverRegistrar.GetServiceXml());
            }

            return NotFound();
        }

        /// <summary>
        /// Gets Dlna media receiver registrar xml.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <returns>Dlna media receiver registrar xml.</returns>
        [HttpGet("{serverId}/ConnectionManager")]
        [HttpGet("{serverId}/ConnectionManager/ConnectionManager", Name = "GetConnectionManager_2")]
        [HttpGet("{serverId}/ConnectionManager/ConnectionManager.xml", Name = "GetConnectionManager_3")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces(MediaTypeNames.Text.Xml)]
        [ProducesFile(MediaTypeNames.Text.Xml)]
        [AllowAnonymous]
        public ActionResult GetConnectionManager([FromRoute, Required] string serverId)
        {
            if (!string.Equals(serverId, _dlnaServerManager.ServerId, StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            return Ok(_dlnaServerManager.ConnectionManager.GetServiceXml());
        }

        /// <summary>
        /// Process a content directory control request.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <returns>Control response.</returns>
        [HttpPost("{serverId}/ContentDirectory/Control")]
        [Produces(MediaTypeNames.Text.Xml)]
        [AllowAnonymous]
        public async Task<ActionResult<ControlResponse>> ProcessContentDirectoryControlRequest([FromRoute, Required] string serverId)
        {
            if (!string.Equals(serverId, _dlnaServerManager.ServerId, StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            return await ProcessControlRequestInternalAsync(Request.Body, _dlnaServerManager.ContentDirectory).ConfigureAwait(false);
        }

        /// <summary>
        /// Process a connection manager control request.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <returns>Control response.</returns>
        [HttpPost("{serverId}/ConnectionManager/Control")]
        [Produces(MediaTypeNames.Text.Xml)]
        [AllowAnonymous]
        public async Task<ActionResult<ControlResponse>> ProcessConnectionManagerControlRequest([FromRoute, Required] string serverId)
        {
            if (!string.Equals(serverId, _dlnaServerManager.ServerId, StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            return await ProcessControlRequestInternalAsync(Request.Body, _dlnaServerManager.ConnectionManager).ConfigureAwait(false);
        }

        /// <summary>
        /// Process a media receiver registrar control request.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <returns>Control response.</returns>
        [HttpPost("{serverId}/MediaReceiverRegistrar/Control")]
        [Produces(MediaTypeNames.Text.Xml)]
        [AllowAnonymous]
        public async Task<ActionResult<ControlResponse>> ProcessMediaReceiverRegistrarControlRequest([FromRoute, Required] string serverId)
        {
            if (!string.Equals(serverId, _dlnaServerManager.ServerId, StringComparison.OrdinalIgnoreCase)
                || _dlnaServerManager.MediaReceiverRegistrar == null)
            {
                return NotFound();
            }

            return await ProcessControlRequestInternalAsync(Request.Body, _dlnaServerManager.MediaReceiverRegistrar).ConfigureAwait(false);
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
        [AllowAnonymous]
        public ActionResult<EventSubscriptionResponse> ProcessMediaReceiverRegistrarEventRequest([FromRoute, Required] string serverId)
        {
            if (string.Equals(serverId, _dlnaServerManager.ServerId, StringComparison.OrdinalIgnoreCase)
                && _dlnaServerManager.MediaReceiverRegistrar != null)
            {
                return ProcessEventRequest(_dlnaServerManager.MediaReceiverRegistrar);
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
        [AllowAnonymous]
        public ActionResult<EventSubscriptionResponse> ProcessContentDirectoryEventRequest([FromRoute, Required] string serverId)
        {
            if (string.Equals(serverId, _dlnaServerManager.ServerId, StringComparison.OrdinalIgnoreCase))
            {
                return ProcessEventRequest(_dlnaServerManager.ContentDirectory);
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
        [AllowAnonymous]
        public ActionResult<EventSubscriptionResponse> ProcessConnectionManagerEventRequest([FromRoute, Required] string serverId)
        {
            if (string.Equals(serverId, _dlnaServerManager.ServerId, StringComparison.OrdinalIgnoreCase))
            {
                return ProcessEventRequest(_dlnaServerManager.ConnectionManager);
            }

            return NotFound();
        }

        /// <summary>
        /// Gets a server icon.
        /// </summary>
        /// <param name="serverId">Server UUID.</param>
        /// <param name="fileName">The icon filename.</param>
        /// <returns>Icon stream.</returns>
        [HttpGet("{serverId}/icons/{fileName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesImageFile]
        [Produces(MediaTypeNames.Text.Xml)]
        [AllowAnonymous]
        public ActionResult GetIconId([FromRoute, Required] string serverId, [FromRoute, Required] string fileName)
        {
            return !string.Equals(serverId, _dlnaServerManager.ServerId, StringComparison.OrdinalIgnoreCase) ? NotFound() : GetIconInternal(fileName);
        }

        /// <summary>
        /// Gets a server icon.
        /// </summary>
        /// <param name="fileName">The icon filename.</param>
        /// <returns>Icon stream.</returns>
        [HttpGet("icons/{fileName}")]
        [ProducesImageFile]
        [Produces(MediaTypeNames.Text.Xml)]
        [AllowAnonymous]
        public ActionResult GetIcon([FromRoute, Required] string fileName)
        {
            return GetIconInternal(fileName);
        }

        /// <summary>
        /// Extracts an icon from the assembly.
        /// </summary>
        /// <param name="fileName">Filename of the icon to extract.</param>
        /// <returns><see cref="ActionResult"/> of the icon.</returns>
        private ActionResult GetIconInternal(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            var format = fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                            ? ImageFormat.Png
                            : ImageFormat.Jpg;

            var resource = GetType().Namespace + ".Images." + fileName.ToLowerInvariant();

            // Do not put this in a using, otherwise the stream will be closed in the middleware.
            var icon = new ImageStream
            {
                Format = format,
                Stream = typeof(DlnaServerPlugin).Assembly.GetManifestResourceStream(resource)
            };

            var contentType = "image/" + Path.GetExtension(fileName)
                .TrimStart('.')
                .ToLowerInvariant();

            return File(icon.Stream, contentType);
        }

        private string GetAbsoluteUri()
        {
            return $"{Request.Scheme}://{Request.Host}{Request.Path}";
        }

        private Task<ControlResponse> ProcessControlRequestInternalAsync(Stream requestStream, IUpnpService service)
        {
            return service.ProcessControlRequestAsync(new ControlRequest(Request.Headers, requestStream, GetAbsoluteUri()));
        }

        private EventSubscriptionResponse ProcessEventRequest(IDlnaEventManager dlnaEventManager)
        {
            var subscriptionId = Request.Headers["SID"];
            if (!string.Equals(Request.Method, "subscribe", StringComparison.OrdinalIgnoreCase))
            {
                return dlnaEventManager.CancelEventSubscription(subscriptionId);
            }

            var notificationType = Request.Headers["NT"];
            var callback = Request.Headers["CALLBACK"];
            var timeoutString = Request.Headers["TIMEOUT"];

            if (string.IsNullOrEmpty(notificationType))
            {
                return dlnaEventManager.RenewEventSubscription(
                    subscriptionId,
                    notificationType,
                    timeoutString,
                    callback);
            }

            return dlnaEventManager.CreateEventSubscription(notificationType, timeoutString, callback);
        }
    }
}
