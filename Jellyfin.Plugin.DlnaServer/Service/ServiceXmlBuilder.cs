using System;
using System.Collections.Generic;
using System.Security;
using System.Text;
using Jellyfin.Plugin.Ssdp.Didl;
using Jellyfin.Plugin.Ssdp.Model;

namespace Jellyfin.Plugin.DlnaServer.Service
{
    /// <summary>
    /// Defines the <see cref="ServiceXmlBuilder" />.
    /// </summary>
    public static class ServiceXmlBuilder
    {
        /// <summary>
        /// Returns the XML representation of this service.
        /// </summary>
        /// <param name="actions">The <see cref="IEnumerable{ServiceAction}"/>.</param>
        /// <param name="stateVariables">The <see cref="IEnumerable{StateVariable}"/>.</param>
        /// <returns>An XML representation of the given parameters.</returns>
        public static string GetXml(IEnumerable<ServiceAction> actions, IEnumerable<StateVariable> stateVariables)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            if (stateVariables == null)
            {
                throw new ArgumentNullException(nameof(stateVariables));
            }

            var builder = new StringBuilder();
            builder.Append("<?xml version=\"1.0\"?><scpd xmlns=\"urn:schemas-upnp-org:service-1-0\"><specVersion><major>1</major><minor>0</minor></specVersion>");
            AppendActionList(builder, actions);
            AppendServiceStateTable(builder, stateVariables);
            builder.Append("</scpd>");
            return builder.ToString();
        }

        /// <summary>
        /// Appends an action list to the <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/>.</param>
        /// <param name="actions">The <see cref="IEnumerable{ServiceAction}"/>.</param>
        private static void AppendActionList(StringBuilder builder, IEnumerable<ServiceAction> actions)
        {
            builder.Append("<actionList>");

            foreach (var item in actions)
            {
                builder.Append("<action><name>")
                    .Append(XmlUtilities.EncodeUrl(item.Name))
                    .Append("</name><argumentList>");

                foreach (var argument in item.ArgumentList)
                {
                    builder.Append("<argument><name>")
                        .Append(XmlUtilities.EncodeUrl(argument.Name))
                        .Append("</name>")
                        .Append("<direction>")
                        .Append(XmlUtilities.EncodeUrl(argument.Direction.ToString()))
                        .Append("</direction>")
                        .Append("<relatedStateVariable>")
                        .Append(XmlUtilities.EncodeUrl(argument.RelatedStateVariable.ToString()))
                        .Append("</relatedStateVariable></argument>");
                }

                builder.Append("</argumentList></action>");
            }

            builder.Append("</actionList>");
        }

        /// <summary>
        /// Appends a service state table to the <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/>.</param>
        /// <param name="stateVariables">The <see cref="IEnumerable{StateVariable}"/>.</param>
        private static void AppendServiceStateTable(StringBuilder builder, IEnumerable<StateVariable> stateVariables)
        {
            builder.Append("<serviceStateTable>");

            foreach (var item in stateVariables)
            {
                var sendEvents = item.SendsEvents ? "yes" : "no";
                var datatype = item.DataType.ToDlnaString();

                builder.Append("<stateVariable sendEvents=\"")
                    .Append(sendEvents)
                    .Append("\"><name>")
                    .Append(XmlUtilities.EncodeUrl(item.Name.ToString()))
                    .Append("</name><dataType>")
                    .Append(XmlUtilities.EncodeUrl(datatype))
                    .Append("</dataType>");

                if (item.AllowedValues.Count > 0)
                {
                    builder.Append("<allowedValueList>");
                    foreach (var allowedValue in item.AllowedValues)
                    {
                        builder.Append("<allowedValue>")
                            .Append(XmlUtilities.EncodeUrl(allowedValue))
                            .Append("</allowedValue>");
                    }

                    builder.Append("</allowedValueList>");
                }

                builder.Append("</stateVariable>");
            }

            builder.Append("</serviceStateTable>");
        }
    }
}
