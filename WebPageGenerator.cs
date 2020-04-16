using System;
using System.Linq;
using System.Text;
using System.Xml;

namespace Termors.Serivces.HippotronicsThermoDaemon
{
    public class WebPageGenerator
    {
        public static string GenerateWebPage()
        {
            var xmlDoc = new XmlDocument();
            var htmlEl = xmlDoc.CreateElement("html");
            var headEl = xmlDoc.CreateElement("head");
            var bodyEl = xmlDoc.CreateElement("body");

            GenerateHead(xmlDoc, headEl);
            GenerateBody(xmlDoc, bodyEl);

            htmlEl.AppendChild(headEl);
            htmlEl.AppendChild(bodyEl);
            xmlDoc.AppendChild(htmlEl);

            var sb = new StringBuilder(10000);
            XmlWriterSettings wriSettings = new XmlWriterSettings { OmitXmlDeclaration = true };
            var wri = XmlWriter.Create(sb, wriSettings);

            xmlDoc.WriteTo(wri);
            wri.Flush();

            return sb.ToString();
        }

        private static void GenerateBody(XmlDocument xmlDoc, XmlElement bodyEl)
        {
            // TODO
        }


        private static XmlNode CreateLink(XmlDocument xmlDoc, string text, string href)
        {
            var aEl = xmlDoc.CreateElement("a");
            var hrefAtt = xmlDoc.CreateAttribute("href");
            hrefAtt.Value = href;

            aEl.Attributes.Append(hrefAtt);
            aEl.AppendChild(xmlDoc.CreateTextNode(text));

            return aEl;
        }

        private static void GenerateHead(XmlDocument xmlDoc, XmlElement headEl)
        {
            var titleEl = xmlDoc.CreateElement("title");
            titleEl.AppendChild(xmlDoc.CreateTextNode("Hippotronics Thermostat Control"));

            headEl.AppendChild(titleEl);
        }
    }
}
