using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RzDb.CodeGen
{
    public class ProjectWildcardPath
    {
        public bool ModifyClassPath(string ProjectFile, string WildcardPath)
        {
            try
            {
                FileStream fs = new FileStream(ProjectFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var xmldoc = new XmlDocument();
                xmldoc.Load(fs);
                if (WildcardPath.Contains('*'))
                {
                    var PathForWildcard = WildcardPath.Split('*')[0];
                    XmlNamespaceManager mgr = new XmlNamespaceManager(xmldoc.NameTable);
                    mgr.AddNamespace("x", "http://schemas.microsoft.com/developer/msbuild/2003");

                    //First check and see if wildcard exists
                    var nodesWc = xmldoc.SelectNodes(@"//x:Compile[@Include='" + WildcardPath + @"']", mgr);
                    if (nodesWc.Count==0)
                    {
                        var nodes = xmldoc.SelectNodes(@"//x:Compile[starts-with(@Include, '" + PathForWildcard + @"')]", mgr);
                        for (int i = nodes.Count - 1; i >= 0; i--)
                        {
                            nodes[i].ParentNode.RemoveChild(nodes[i]);
                        }
                        XmlDocumentFragment docFrag = xmldoc.CreateDocumentFragment();

                        //Set the contents of the document fragment.
                        
                        docFrag.InnerXml = @"<ItemGroup><Compile Include=""" + WildcardPath + @""" /></ItemGroup>";

                        //Add the children of the document fragment to the
                        //original document.
                        xmldoc.DocumentElement.AppendChild(docFrag);
                        var sXML = xmldoc.OuterXml.Replace(@" xmlns=""""", "");
                        xmldoc.LoadXml(sXML);
                        xmldoc.Save(ProjectFile);
                    } 
                }
                return true;
            }
            catch //(Exception ex)
            {
                //throw ex;
            }
            return false;
        }
    }
}
