using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace RzDb.CodeGen
{
    class ConsoleHarness
    {
        static void Main(string[] args)
        {
            var outputPath = Path.GetTempPath() + "RzDbCodeGen\\";
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);
            // Code that goes into RzDb.CodeGenerations.tt file starts here --- 

            string ConnectionString = ConfigurationManager.ConnectionStrings["RzDBEntities"].ConnectionString;
            new RzDbGenDemoGenerator(ConnectionString, outputPath).ProcessTemplate("RzDbEntities");
			
            // End of the Code that goes into RzDb.CodeGenerations.tt 
            Process.Start(outputPath);
        }
    }
}
