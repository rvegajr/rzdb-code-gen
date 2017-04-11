using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace RzDb.CodeGen
{
    class ConsoleHarness
    {
        static void Main(string[] args)
        {
            var edmxFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase) + @"\Resources\AW.edmx";
            var outputPath = Path.GetTempPath() + "RzDbCodeGen\\";
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);
            // Code that goes into RzDb.CodeGenerations.tt file starts here --- 

            new EdmxGenDemoGenerator(edmxFile, outputPath).ProcessTemplate();

            // End of the Code that goes into RzDb.CodeGenerations.tt 
            Process.Start(outputPath);
        }
    }
}
