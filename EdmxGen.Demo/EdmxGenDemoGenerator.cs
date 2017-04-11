using RzDb.CodeGen;
public class EdmxGenDemoGenerator : EdmxCodeGenBase
{
    public EdmxGenDemoGenerator(string edmxPath, string outputPath) : base(edmxPath, @"EdmxGen.Demo\EdmxGenDemoTemplate.cshtml", outputPath)
    {
            
    }
}