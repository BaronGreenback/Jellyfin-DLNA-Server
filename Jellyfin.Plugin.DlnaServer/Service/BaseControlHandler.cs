using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Jellyfin.Plugin.DlnaServer.Configuration;
using Jellyfin.Plugin.Ssdp.Didl;
using MediaBrowser.Controller.Extensions;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.DlnaServer.Service
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
        /// <param name="config">The <see cref="DlnaServerConfiguration"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        protected BaseControlHandler(DlnaServerConfiguration config, ILogger logger)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Logger = logger;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        protected DlnaServerConfiguration Config { get; }

        /// <summary>
        /// Gets the Logger.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Processes a control request.
        /// </summary>
        /// <param name="request">The <see cref="ControlRequest"/>.</param>
        /// <returns>The <see cref="Task{ControlResponse}"/>.</returns>
        public async Task<ControlResponse> ProcessControlRequestAsync(ControlRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            try
            {
                LogRequest(request);

                var response = await ProcessControlRequestInternalAsync(request).ConfigureAwait(false);
                LogResponse(response);
                return response;
            }
            catch (Exception ex)
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
        protected abstract void WriteResult(string methodName, IDictionary<string, string> methodParams, XmlWriter xmlWriter);

        /// <summary>
        /// The ParseRequestAsync.
        /// </summary>
        /// <param name="reader">The reader<see cref="XmlReader"/>.</param>
        /// <returns>The <see cref="Task{ControlRequestInfo}"/>.</returns>
        private static async Task<ControlRequestInfo> ParseRequestAsync(XmlReader reader)
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
                                    return await ParseBodyTagAsync(subReader).ConfigureAwait(false);
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

        private static async Task<ControlRequestInfo> ParseBodyTagAsync(XmlReader reader)
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
                        await ParseFirstBodyChildAsync(subReader, result.Headers).ConfigureAwait(false);
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

        private static async Task ParseFirstBodyChildAsync(XmlReader reader, IDictionary<string, string> headers)
        {
            await reader.MoveToContentAsync().ConfigureAwait(false);
            await reader.ReadAsync().ConfigureAwait(false);

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    // TODO: Should we be doing this here, or should it be handled earlier when decoding the request?
                    headers[reader.LocalName.RemoveDiacritics()] = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                }
                else
                {
                    await reader.ReadAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task<ControlResponse> ProcessControlRequestInternalAsync(ControlRequest request)
        {
            ControlRequestInfo? requestInfo;

            using (var streamReader = new StreamReader(request.InputXml, Encoding.UTF8))
            {
                var readerSettings = new XmlReaderSettings
                {
                    ValidationType = ValidationType.None,
                    CheckCharacters = false,
                    IgnoreProcessingInstructions = true,
                    IgnoreComments = true,
                    Async = true
                };

                using var reader = XmlReader.Create(streamReader, readerSettings);
                requestInfo = await ParseRequestAsync(reader).ConfigureAwait(false);
            }

            Logger.LogDebug("Received control request {Name}", requestInfo.LocalName);

            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                CloseOutput = false
            };

            StringWriter builder = new Utf8StringWriter();

            using (var writer = XmlWriter.Create(builder, settings))
            {
                writer.WriteStartDocument(true);

                writer.WriteStartElement("s", "Envelope", NsSoapEnv);
                writer.WriteAttributeString(string.Empty, "encodingStyle", NsSoapEnv, "http://schemas.xmlsoap.org/soap/encoding/");

                writer.WriteStartElement("s", "Body", NsSoapEnv);
                writer.WriteStartElement("u", requestInfo.LocalName + "Response", requestInfo.NamespaceUri);

                WriteResult(requestInfo.LocalName, requestInfo.Headers, writer);

                writer.WriteFullEndElement();
                writer.WriteFullEndElement();

                writer.WriteFullEndElement();
                writer.WriteEndDocument();
            }

            var xml = builder.ToString().Replace("xmlns:m=", "xmlns:u=", StringComparison.Ordinal);

            var controlResponse = new ControlResponse(xml);

            controlResponse.Headers.Add("EXT", string.Empty);

            return controlResponse;
        }

        private void LogRequest(ControlRequest request)
        {
            if (!Config.EnableDebugLog)
            {
                return;
            }

            Logger.LogDebug("Control request. Headers: {@Headers}", request.Headers);
        }

        private void LogResponse(ControlResponse response)
        {
            if (!Config.EnableDebugLog)
            {
                return;
            }

            Logger.LogDebug("Control response. Headers: {@Headers}\n{Xml}", response.Headers, WebUtility.HtmlDecode(response.Xml));
        }

        /// <summary>
        /// Defines the <see cref="ControlRequestInfo" />.
        /// </summary>
        private class ControlRequestInfo
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ControlRequestInfo"/> class.
            /// </summary>
            /// <param name="localName">The local name.</param>
            /// <param name="namespaceUri">The namespace Uri.</param>
            public ControlRequestInfo(string localName, string namespaceUri)
            {
                LocalName = localName;
                NamespaceUri = namespaceUri;
                Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
            public Dictionary<string, string> Headers { get; }
        }
    }
}
