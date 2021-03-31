using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Jellyfin.Plugin.Dlna.Didl;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Server.Service
{
    /// <summary>
    /// Defines the <see cref="BaseControlHandler" />.
    /// </summary>
    public abstract class BaseControlHandler
    {
        private const string NsSoapEnv = "http://schemas.xmlsoap.org/soap/envelope/";

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseControlHandler"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        protected BaseControlHandler(ILogger logger) => Logger = logger;

        /// <summary>
        /// Gets the Logger.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Processes a control request.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="requireParams"><c>True</c> if parameters should be parsed into a dictionary.</param>
        /// <returns>The <see cref="Task{ControlResponse}"/>.</returns>
        public async Task<ControlResponse> ProcessControlRequestAsync(HttpRequest request, bool requireParams)
        {
            try
            {
#pragma warning disable CA1062 // Validate arguments of public methods : Called from API.
                if (DlnaServerPlugin.Instance!.Configuration.EnableDebugLog)
                {
                    LogRequest(request, requireParams);
                }

                var response = await ProcessControlRequestInternalAsync(request, requireParams).ConfigureAwait(false);
#pragma warning restore CA1062 // Validate arguments of public methods
                if (DlnaServerPlugin.Instance!.Configuration.EnableDebugLog)
                {
                    Logger.LogInformation("Control response. {Xml}", HttpUtility.HtmlDecode(response.Xml));
                }

                return response;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Logger.LogError(ex, "Error processing control request");

                return ControlErrorHandler.GetResponse(ex);
            }
        }

        /// <summary>
        /// Writes the result to the stream.
        /// </summary>
        /// <param name="methodName">Method name.</param>
        /// <param name="methodParams">Method parameters.</param>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/> instance.</param>
        protected abstract void WriteResult(string methodName, ControlRequestInfo methodParams, XmlWriter xmlWriter);

        /// <summary>
        /// Parses the Request.
        /// </summary>
        /// <param name="reader">The <see cref="XmlReader"/>.</param>
        /// <param name="requireParams"><c>True</c> if parameters should be parsed into a dictionary.</param>
        /// <returns>The <see cref="Task{ControlRequestInfo}"/>.</returns>
        private static async Task<ControlRequestInfo> ParseRequestAsync(XmlReader reader, bool requireParams)
        {
            await reader.MoveToContentAsync().ConfigureAwait(false);
            await reader.ReadAsync().ConfigureAwait(false);

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.LocalName)
                    {
                        case "Body":
                            {
                                if (!reader.IsEmptyElement)
                                {
                                    using var subReader = reader.ReadSubtree();
                                    return await ParseBodyTagAsync(subReader, requireParams).ConfigureAwait(false);
                                }

                                await reader.ReadAsync().ConfigureAwait(false);

                                break;
                            }

                        default:
                            {
                                await reader.SkipAsync().ConfigureAwait(false);
                                break;
                            }
                    }
                }
                else
                {
                    await reader.ReadAsync().ConfigureAwait(false);
                }
            }

            throw new EndOfStreamException("Stream ended but no body tag found.");
        }

        private static async Task<ControlRequestInfo> ParseBodyTagAsync(XmlReader reader, bool requireParams)
        {
            string? namespaceUri = null, localName = null;

            await reader.MoveToContentAsync().ConfigureAwait(false);
            await reader.ReadAsync().ConfigureAwait(false);

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    localName = reader.LocalName;
                    namespaceUri = reader.NamespaceURI;

                    if (!reader.IsEmptyElement)
                    {
                        var result = new ControlRequestInfo(localName, namespaceUri);
                        using var subReader = reader.ReadSubtree();
                        await ParseFirstBodyChildAsync(subReader, requireParams ? result.Headers : null).ConfigureAwait(false);
                        return result;
                    }

                    await reader.ReadAsync().ConfigureAwait(false);
                }
                else
                {
                    await reader.ReadAsync().ConfigureAwait(false);
                }
            }

            if (localName != null && namespaceUri != null)
            {
                return new ControlRequestInfo(localName, namespaceUri);
            }

            throw new EndOfStreamException("Stream ended but no control found.");
        }

        private static async Task ParseFirstBodyChildAsync(XmlReader reader, IDictionary<string, string>? headers)
        {
            await reader.MoveToContentAsync().ConfigureAwait(false);
            await reader.ReadAsync().ConfigureAwait(false);

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (headers != null)
                    {
                        headers[reader.LocalName] = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    }
                }
                else
                {
                    await reader.ReadAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task<ControlResponse> ProcessControlRequestInternalAsync(HttpRequest request, bool parseHeaders)
        {
            var streamReader = new StreamReader(request.Body, Encoding.UTF8);
            var readerSettings = new XmlReaderSettings
            {
                ValidationType = ValidationType.None,
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                Async = true
            };

            using var reader = XmlReader.Create(streamReader, readerSettings);
            var requestInfo = await ParseRequestAsync(reader, parseHeaders).ConfigureAwait(false);

            Logger.LogDebug("Received control request {Name}", requestInfo.LocalName);

            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                CloseOutput = false,
                CheckCharacters = false
            };

            var builder = new Utf8StringWriter();

            using var writer = XmlWriter.Create(builder, settings);
            writer.WriteStartDocument(true);
            writer.WriteStartElement("s", "Envelope", NsSoapEnv);
            writer.WriteAttributeString(string.Empty, "encodingStyle", NsSoapEnv, "http://schemas.xmlsoap.org/soap/encoding/");
            writer.WriteStartElement("s", "Body", NsSoapEnv);
            writer.WriteStartElement("u", requestInfo.LocalName + "Response", requestInfo.NamespaceUri);
            WriteResult(requestInfo.LocalName, requestInfo, writer);
            writer.WriteFullEndElement();
            writer.WriteFullEndElement();
            writer.WriteFullEndElement();
            writer.WriteEndDocument();
            writer.Flush();

            var xml = builder.ToString().Replace("xmlns:m=", "xmlns:u=", StringComparison.Ordinal); // TODO: Fix need for this.

            return new ControlResponse(xml);
        }

        /// <summary>
        /// Logs debug information.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/> instance.</param>
        /// <param name="requireParams">If <c>True</c> the lazy create request.Headers isn't to be referenced.</param>
        private void LogRequest(HttpRequest request, bool requireParams)
        {
            var sb = new StringBuilder(1024);
            sb.Append("Request from: ");
            sb.AppendLine((request.HttpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback).ToString());

            if (requireParams)
            {
                foreach (var (key, value) in request.Headers)
                {
                    sb.Append(key);
                    sb.Append('=');
                    sb.AppendLine(value);
                }
            }

            Logger.LogInformation("Control request. Headers: {Headers:l}", sb.ToString());
        }

        /// <summary>
        /// Defines the <see cref="ControlRequestInfo" />.
        /// </summary>
        protected class ControlRequestInfo
        {
            private Dictionary<string, string>? _headers;

            /// <summary>
            /// Initializes a new instance of the <see cref="ControlRequestInfo"/> class.
            /// </summary>
            /// <param name="localName">The local name.</param>
            /// <param name="namespaceUri">The namespace Uri.</param>
            public ControlRequestInfo(string localName, string namespaceUri)
            {
                LocalName = localName;
                NamespaceUri = namespaceUri;
            }

            /// <summary>
            /// Gets the XML Local Name.
            /// </summary>
            public string LocalName { get; }

            /// <summary>
            /// Gets the Namespace URI.
            /// </summary>
            public string NamespaceUri { get; }

            /// <summary>
            /// Gets the Headers.
            /// </summary>
            public Dictionary<string, string> Headers
            {
                get
                {
                    _headers ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    return _headers;
                }
            }
        }
    }
}
