using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RzDb.CodeGen
{
    public class SwaggerJsonUtilities
    {
        public Dictionary<string, string> AppendDefinitionsToKeep( JToken item)
        {
            Dictionary<string, string> DefinitionsToKeep = new Dictionary<string, string>();
            AppendDefinitionsToKeep(DefinitionsToKeep, item);
            return DefinitionsToKeep;
        }
        public void AppendDefinitionsToKeep(Dictionary<string, string> DefinitionsToKeep, JToken item )
        {
            foreach (JToken token in item.FindTokens("$ref"))
            {
                if (token.ToString().Contains(@"/definitions/"))
                {
                    var sDefinition = token.ToString().Replace(@"#/definitions/", "");
                    if (!DefinitionsToKeep.ContainsKey(sDefinition)) DefinitionsToKeep.Add(sDefinition, "");
                }
            }

        }
        public bool SplitSwaggerJson(string JsonFile)
        {
            try
            {
                var outputPath = Path.GetDirectoryName(JsonFile) + Path.DirectorySeparatorChar;
                var filename = Path.GetFileNameWithoutExtension(JsonFile);
                foreach (string f in Directory.EnumerateFiles(outputPath, filename + "_*.json"))
                {
                    File.Delete(f);
                }
                var jsonString = File.ReadAllText(JsonFile);
                JObject json = JObject.Parse(jsonString);
                JObject jsonModel = JObject.Parse(jsonString);
                jsonModel.Remove("paths");
                var paths = from p in json["paths"] select p;
                var cnt = 0;
                var outputCount = 1;
                JObject newjson = null;
                JObject targetPath = null;
                JObject targetDefinitions = null;
                Dictionary<string, string> DefinitionsToKeep = new Dictionary<string, string>();
                Dictionary<string, string> AdditionalDefinitionsToKeep = new Dictionary<string, string>();
                foreach (var item in paths)
                {

                    cnt++;
                    if (cnt==1)
                    {
                        newjson = JObject.Parse(jsonString);
                        targetPath = newjson["paths"] as JObject;
                        targetDefinitions = newjson["definitions"] as JObject;
                        targetPath.RemoveAll();
                        targetDefinitions.RemoveAll();
                    }
                    //Lets check and see if this contains a reference to definitions
                    AppendDefinitionsToKeep(DefinitionsToKeep, item);

                    targetPath.Add(item);
                    if (cnt==30)
                    {
                        //strip un-needed definitions
                        var definitions = from d in json["definitions"] select d;
                        //The definitions will have $refs also, so we have to grab those before we figure out which definitions to keep
                        foreach (var d in definitions)
                            if (DefinitionsToKeep.ContainsKey(d.Path.Replace("definitions.", ""))) AppendDefinitionsToKeep(DefinitionsToKeep, d);

                        foreach (var d in definitions)
                            if (DefinitionsToKeep.ContainsKey(d.Path.Replace("definitions.", ""))) targetDefinitions.Add(d);

                        File.WriteAllText(outputPath + filename + "_" + outputCount + @".json", newjson.ToString());
                        DefinitionsToKeep.Clear();
                        outputCount++;
                        cnt = 0;
                    }
                }
                if (cnt > 0)
                {
                    var definitions = from d in json["definitions"] select d;
                    foreach (var d in definitions)
                        if (DefinitionsToKeep.ContainsKey(d.Path.Replace("definitions.", ""))) AppendDefinitionsToKeep(DefinitionsToKeep, d);
                    foreach (var d in definitions)
                        if (DefinitionsToKeep.ContainsKey(d.Path.Replace("definitions.", ""))) targetDefinitions.Add(d);

                    File.WriteAllText(outputPath + filename + "_" + outputCount + @".json", newjson.ToString());
                }

                //Now go through the definitions to make sure we get all the foriegn key $ref 

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            //return false;
        }
    }

    public static class JsonExtensions
    {
        public static List<JToken> FindTokens(this JToken containerToken, string name)
        {
            List<JToken> matches = new List<JToken>();
            FindTokens(containerToken, name, matches);
            return matches;
        }

        private static void FindTokens(JToken containerToken, string name, List<JToken> matches)
        {
            if (containerToken.Type == JTokenType.Property) {
                foreach (var child in containerToken.Children())
                {
                    FindTokens(child, name, matches);
                    /*
                    if (child.Name == name)
                    {
                        matches.Add(child.Value);
                    }
                    FindTokens(child.Value, name, matches);*/
                }
            }
            else if (containerToken.Type == JTokenType.Object)
            {
                foreach (JProperty child in containerToken.Children<JProperty>())
                {
                    if (child.Name == name)
                    {
                        matches.Add(child.Value);
                    }
                    FindTokens(child.Value, name, matches);
                }
            }
            else if (containerToken.Type == JTokenType.Array)
            {
                foreach (JToken child in containerToken.Children())
                {
                    FindTokens(child, name, matches);
                }
            }
        }
    }
}
