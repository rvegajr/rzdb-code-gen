using RzDb.CodeGen;
public class EdmxGenDemoGenerator : CodeGenBase
{
    public EdmxGenDemoGenerator(string connectionString, string outputPath) : base(connectionString, @"EdmxGen.Demo\EdmxGenDemoTemplate.cshtml", outputPath)
    {
            
    }
}