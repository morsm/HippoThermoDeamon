using System;
using System.Linq;
using System.Text;
using System.Xml;

namespace Termors.Serivces.HippotronicsLedDaemon
{
    public class WebPageGenerator
    {
        public static string GenerateWebPage(LampNode[] nodes)
        {
            var xmlDoc = new XmlDocument();
            var htmlEl = xmlDoc.CreateElement("html");
            var headEl = xmlDoc.CreateElement("head");
            var bodyEl = xmlDoc.CreateElement("body");

            GenerateHead(xmlDoc, headEl);
            GenerateBody(xmlDoc, bodyEl, nodes);

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

        private static void GenerateBody(XmlDocument xmlDoc, XmlElement bodyEl, LampNode[] nodes)
        {
            var tableEl = xmlDoc.CreateElement("table");
            var theadEl = xmlDoc.CreateElement("thead");
            var tbodyEl = xmlDoc.CreateElement("tbody");

            var border = xmlDoc.CreateAttribute("border");
            border.Value = "1";
            tableEl.Attributes.Append(border);
            GenerateTableHead(xmlDoc, theadEl);

            GenerateTableBody(xmlDoc, tbodyEl, nodes);

            tableEl.AppendChild(theadEl);
            tableEl.AppendChild(tbodyEl);
            bodyEl.AppendChild(tableEl);
        }

        private static void GenerateTableBody(XmlDocument xmlDoc, XmlElement tbodyEl, LampNode[] nodes)
        {
            var sortedNodes = from node in nodes orderby node.Name select node;

            foreach(var node in sortedNodes)
            {
                var tr1 = xmlDoc.CreateElement("tr");
                var tr2 = xmlDoc.CreateElement("tr");

                var tdName = xmlDoc.CreateElement("td");
                tdName.AppendChild(xmlDoc.CreateTextNode(node.Name));

                var tdOnOff = xmlDoc.CreateElement("td");
                var tdLink = xmlDoc.CreateElement("td");

                var colspan = xmlDoc.CreateAttribute("colspan");
                colspan.Value = "2";
                tdLink.Attributes.Append(colspan);

                var linkOn = CreateLink(xmlDoc, "On", node.Url + "/on.html");
                var linkOff = CreateLink(xmlDoc, "Off", node.Url + "/off.html");
                var link = CreateLink(xmlDoc, node.Url, node.Url);

                tdOnOff.AppendChild(linkOn);
                tdOnOff.AppendChild(xmlDoc.CreateTextNode(" "));
                tdOnOff.AppendChild(linkOff);

                tdLink.AppendChild(link);

                tr1.AppendChild(tdName);
                tr1.AppendChild(tdOnOff);
                tr2.AppendChild(tdLink);

                tbodyEl.AppendChild(tr1);
                tbodyEl.AppendChild(tr2);

            }

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

        private static void GenerateTableHead(XmlDocument xmlDoc, XmlElement theadEl)
        {
            var tr = xmlDoc.CreateElement("tr");
            var th1 = xmlDoc.CreateElement("th");
            var th2 = xmlDoc.CreateElement("th");
            var width = xmlDoc.CreateAttribute("width");

            width.Value = "80%";
            th1.Attributes.Append(width);
            th1.AppendChild(xmlDoc.CreateTextNode("Lamp name"));

            th2.AppendChild(xmlDoc.CreateTextNode("On/off"));

            tr.AppendChild(th1);
            tr.AppendChild(th2);
            theadEl.AppendChild(tr);
        }

        private static void GenerateHead(XmlDocument xmlDoc, XmlElement headEl)
        {
            var titleEl = xmlDoc.CreateElement("title");
            titleEl.AppendChild(xmlDoc.CreateTextNode("Hippotronics Lamp Control"));

            headEl.AppendChild(titleEl);
        }
    }
}
