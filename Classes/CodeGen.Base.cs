using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using RazorEngine.Text;

namespace RzDb.CodeGen
{

    public abstract class EdmxCodeGenBase
    {
        public virtual string EdmxPath { get; set; } = "";
        public virtual string TemplatePath { get; set; } = "";
        public virtual string OutputPath { get; set; } = "";

        public EdmxCodeGenBase(string edmxPath, string templatePath, string outputPath)
        {
            this.EdmxPath = edmxPath;
            this.TemplatePath = templatePath;
            this.OutputPath = outputPath;
        }

        public bool ProcessTemplate()
        {
            try
            {
                string assemblyBasePath = Path.GetDirectoryName(System.IO.Path.GetDirectoryName(
                            System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase)).Replace("file:\\", "");
                string FullTemplatePath = assemblyBasePath + Path.DirectorySeparatorChar + TemplatePath;
                string outputDirectory = Path.GetDirectoryName(OutputPath) + Path.DirectorySeparatorChar;
                var filefound = false;
                filefound = (File.Exists(EdmxPath));
                if (!filefound)
                {
                    EdmxPath = EdmxPath.Replace("file:\\", "");
                    filefound = (File.Exists(EdmxPath));
                }
                if (!filefound) throw new FileNotFoundException("EdmxPath File " + EdmxPath + " is not found");
                if (!File.Exists(FullTemplatePath)) throw new FileNotFoundException("Template File " + FullTemplatePath + " is not found");
                if (!Directory.Exists(outputDirectory)) throw new DirectoryNotFoundException("Path " + outputDirectory + " is not found");
                Tuple<MetadataWorkspace, SchemaData> metadataPayload = LoadEdmx(EdmxPath);
                SchemaData schema = metadataPayload.Item2;

                string result = "";
                try
                {
                    TemplateServiceConfiguration config = new TemplateServiceConfiguration();
                    config.EncodedStringFactory = new RawStringFactory(); // Raw string encoding.
                    config.Debug = true;

                    IRazorEngineService service = RazorEngineService.Create(config);
                    result = service.RunCompile(new LoadedTemplateSource(File.ReadAllText(FullTemplatePath), FullTemplatePath), "RzDbCodeGen", typeof(SchemaData), schema);
                }
                catch (Exception exRazerEngine)
                {
                    throw exRazerEngine;
                }
                finally
                {
                    //Clean up old RazorEngine engine paths... it may not clean up THIS one, but at least it will handle old ones
                    foreach (var directory in Directory.GetDirectories(System.IO.Path.GetTempPath(), "RazorEngine*.*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            Directory.Delete(directory, true);
                        }
                        catch
                        {

                        }
                    }
                }
                result = result.Replace("<t>", "")
                    .Replace("<t/>", "")
                    .Replace("<t />", "")
                    .Replace("</t>", "")
                    .Replace("$OUTPUT_PATH$", outputDirectory).TrimStart();
                if (result.Contains("##FILE=")) //File seperation specifier - this will split by the files specified by 
                {
                    string[] parseFiles = result.Split(new[] { @"##FILE=" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string filePart in parseFiles)
                    {
                        int nl = filePart.IndexOf('\n');
                        string newOutputFileName = filePart.Substring(0, nl).Trim();
                        if ((newOutputFileName.Length > 0) && (newOutputFileName.StartsWith(outputDirectory)))
                        {
                            if (File.Exists(newOutputFileName)) File.Delete(newOutputFileName);
                            File.WriteAllText(newOutputFileName, filePart.Substring(nl + 1));
                        }
                    }
                }
                else
                {
                    if (File.Exists(OutputPath)) File.Delete(OutputPath);
                    File.WriteAllText(OutputPath, result);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private Tuple<MetadataWorkspace, SchemaData> LoadEdmx(string path)
        {

            try
            {
                string spath = Path.GetFullPath(path);
                XElement runtime = XElement.Load(path).Elements().First(e => e.Name.LocalName == "Runtime");

                XElement csdlSchema = runtime.Elements().First(e => e.Name.LocalName == "ConceptualModels")
                    .Elements().First(e => e.Name.LocalName == "Schema")
                    .Elements().First(e => e.Name.LocalName == "EntityContainer")
                    ;
                string entityName = csdlSchema.Attributes().First(e => e.Name.LocalName == "Name").Value;

                EdmItemCollection edmCollection;
                XElement csdl = runtime.Elements().First(e => e.Name.LocalName == "ConceptualModels")
                    .Elements().First(e => e.Name.LocalName == "Schema");

                using (XmlReader reader = csdl.CreateReader())
                {
                    edmCollection = new EdmItemCollection(new[] { reader });
                }

                StoreItemCollection storeCollection;
                XElement ssdl = runtime.Elements().First(e => e.Name.LocalName == "StorageModels")
                    .Elements().First(e => e.Name.LocalName == "Schema");
                using (XmlReader reader = ssdl.CreateReader())
                {
                    storeCollection = new StoreItemCollection(new[] { reader });
                }

                SchemaData schema = new SchemaData() { Name = entityName };
                foreach (XNode nod in ssdl.Nodes())
                {
                    if (nod.NodeType == XmlNodeType.Element)
                    {
                        XElement nodEle = (XElement)nod;
                        if (nodEle.Name.LocalName == "EntityType")
                        {
                            EntityType entityType = new EntityType() { Name = nodEle.Attribute("Name").Value };
                            List<string> primaryKeyList = new List<string>();
                            foreach (XNode member in ((XElement)nod).Nodes())
                            {
                                if (member.NodeType == XmlNodeType.Element)
                                {
                                    XElement nodMember = (XElement)member;
                                    if (nodMember.Name.LocalName == "Property")
                                    {
                                        Property property = new Property() { IsNullable = true };
                                        property.Name = nodMember.Attribute("Name").Value ?? "";
                                        property.Type = nodMember.Attribute("Type").Value ?? "";
                                        if (nodMember.Attribute("MaxLength") != null)
                                            property.MaxLength = int.Parse(nodMember.Attribute("MaxLength").Value);
                                        if (nodMember.Attribute("Nullable") != null)
                                            property.IsNullable = bool.Parse(nodMember.Attribute("Nullable").Value);
                                        if (nodMember.Attribute("Precision") != null)
                                            property.Precision = int.Parse(nodMember.Attribute("Precision").Value);
                                        if (nodMember.Attribute("Scale") != null)
                                            property.Scale = int.Parse(nodMember.Attribute("Scale").Value);
                                        if ((nodMember.Attribute("StoreGeneratedPattern") != null) && (nodMember.Attribute("StoreGeneratedPattern").Equals("Identity")))
                                            property.IsIdentity = true;
                                        entityType.Properties.Add(property.Name, property);
                                    }
                                    else if (nodMember.Name.LocalName == "Key")
                                    {
                                        foreach (XNode nodKey in ((XElement)nodMember).Nodes())
                                        {
                                            XElement eleKey = (XElement)nodKey;
                                            string sKey = eleKey.Attribute("Name").Value ?? "";
                                            primaryKeyList.Add(sKey);
                                        }
                                    }

                                }
                                foreach (string primaryKeyName in primaryKeyList)
                                {
                                    if (entityType.Properties.ContainsKey(primaryKeyName))
                                    {
                                        entityType.Properties[primaryKeyName].IsKey = true;
                                        entityType.PrimaryKeys.Add(entityType.Properties[primaryKeyName]);
                                    }
                                }
                            }
                            schema.Add(entityType.Name, entityType);
                        }
                    }
                }

                StorageMappingItemCollection mappingCollection;
                XElement msl = runtime.Elements().First(e => e.Name.LocalName == "Mappings")
                    .Elements().First(e => e.Name.LocalName == "Mapping");
                using (XmlReader reader = msl.CreateReader())
                {
                    mappingCollection = new StorageMappingItemCollection(
                        edmCollection,
                        storeCollection,
                        new[] { reader });
                }
                MetadataWorkspace mdRet = new MetadataWorkspace(() => edmCollection, () => storeCollection, () => mappingCollection);
                //Resolve foriegn keys using MetadataWorkspace
                var relationshipEndMembers = new Dictionary<RelationshipEndMember, Tuple<System.Data.Entity.Core.Metadata.Edm.EntityType, NavigationProperty>>();
                var entitySets = mdRet.GetItems<EntityContainer>(DataSpace.CSpace).First().BaseEntitySets;

                foreach (var s in entitySets)
                {
                    try
                    {
                        if ((s.BuiltInTypeKind == BuiltInTypeKind.EntitySet) || (s.BuiltInTypeKind == BuiltInTypeKind.EntityType) || (s.BuiltInTypeKind == BuiltInTypeKind.EntitySetBase))
                        {
                            if (s.Name.StartsWith("DrillGroupPeer"))
                            {
                                var Name = (s.Name + " ").Trim();
                            }
                            var navProperties = ((System.Data.Entity.Core.Metadata.Edm.EntityType)s.ElementType).DeclaredNavigationProperties;
                            if ((navProperties.Count > 0) && (schema.ContainsKey(s.Name)))
                            {
                                foreach (var navProperty in navProperties)
                                {
                                    //Thank you http://stackoverflow.com/questions/5365708/ef4-get-the-linked-column-names-from-navigationproperty-of-an-edmx
                                    AssociationType association = mdRet.GetItems<AssociationType>(DataSpace.CSpace).Single(a => a.Name == navProperty.RelationshipType.Name);
                                    string fromEntity = association.ReferentialConstraints[0].FromRole.Name;
                                    string fromEntityField = association.ReferentialConstraints[0].FromProperties[0].Name;
                                    string toEntity = association.ReferentialConstraints[0].ToRole.Name;
                                    string toEntityField = association.ReferentialConstraints[0].ToProperties[0].Name;
                                    string toEntityColumnName = toEntityField.Replace("Id", "");
                                    string fromEntityColumnName = fromEntityField.Replace("Id", "");
                                    var ns = new XmlNamespaceManager(new NameTable());
                                    ns.AddNamespace("edmx", "http://schemas.microsoft.com/ado/2009/11/edm");
                                    var r = entitySets.First(a => a.Name == navProperty.RelationshipType.Name);
                                    //Figure out the real entity names based off the end keys
                                    var ends = ((System.Data.Entity.Core.Metadata.Edm.AssociationSet)r).AssociationSetEnds;
                                    if (ends != null)
                                    {
                                        var fromEnd = ends.First(e => e.Name == fromEntity);
                                        if (fromEnd != null) fromEntity = fromEnd.EntitySet.Name;
                                        var toEnd = ends.First(e => e.Name == toEntity);
                                        if (toEnd != null) toEntity = toEnd.EntitySet.Name;
                                    }

                                    var newRel = new Relationship()
                                    {
                                        Name = navProperty.RelationshipType.Name,
                                        FromTableName = fromEntity,
                                        FromFieldName = fromEntityField,
                                        ToFieldName = toEntityField,
                                        ToTableName = toEntity,
                                        ToColumnName = toEntityColumnName,
                                        Type = navProperty.FromEndMember.RelationshipMultiplicity.ToString() + " to " + navProperty.ToEndMember.RelationshipMultiplicity.ToString()
                                    };
                                    schema[s.Name].Relationships.Add(newRel);
                                    var fieldToMarkRelation = (s.Name.Equals(newRel.FromTableName) ? newRel.FromFieldName : newRel.ToFieldName);
                                    if (schema[s.Name].Properties.ContainsKey(fieldToMarkRelation))
                                    {
                                        schema[s.Name].Properties[fieldToMarkRelation].RelatedTo.Add(newRel);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception exE)
                    {
                        throw exE;
                    }
                }

                return new Tuple<MetadataWorkspace, SchemaData>(mdRet, schema);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


    }
    public class RazorEngineTemplate<T> : TemplateBase<T>
    {
        public new T Model
        {
            get { return base.Model; }
            set { base.Model = value; }
        }
    }
}
