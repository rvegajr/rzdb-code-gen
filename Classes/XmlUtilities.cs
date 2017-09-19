using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RzDb.CodeGen
{
    public class XmlUtilities
    {
        public string ExtractConnectionString(string XmlConfigPath, string name)
        {
            var sConnStr = "";
            try
            {
                var xmldoc = new XmlDocument();
                xmldoc.Load(XmlConfigPath);
                var xnodes = xmldoc.SelectNodes("/configuration/connectionStrings/add[@name='" + name  + "']");
                var firstNode = xnodes[0];
                sConnStr = ((XmlElement)firstNode).Attributes["connectionString"].Value;
                return sConnStr;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
