using System;
using System.IO;
using System.Text;
using System.Xml;
using Jellyfin.Plugin.Ssdp.Didl;

namespace Jellyfin.Plugin.DlnaServer.Service
{
    /// <summary>
    /// Defines the <see cref="ControlErrorHandler"/> class.
    /// </summary>
    public static class ControlErrorHandler
    {
        private const string NsSoapEnv = "http://schemas.xmlsoap.org/soap/envelope/";

        /// <summary>
        /// Creates a control response.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> to include.</param>
        /// <returns>A <see cref="ControlResponse"/> instance.</returns>
        public static ControlResponse GetResponse(Exception ex)
        {
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
                writer.WriteStartElement("s", "Fault", NsSoapEnv);

                writer.WriteElementString("faultcode", "500");
                writer.WriteElementString("faultstring", ex?.Message);

                writer.WriteStartElement("detail");
                writer.WriteRaw("<UPnPError xmlns=\"urn:schemas-upnp-org:control-1-0\"><errorCode>401</errorCode><errorDescription>Invalid Action</errorDescription></UPnPError>");
                writer.WriteFullEndElement();

                writer.WriteFullEndElement();
                writer.WriteFullEndElement();

                writer.WriteFullEndElement();
                writer.WriteEndDocument();
            }

            return new ControlResponse(builder.ToString());
        }
    }
}
