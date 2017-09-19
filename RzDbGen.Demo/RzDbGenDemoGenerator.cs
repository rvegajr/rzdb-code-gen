using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RzDb.CodeGen;
public class RzDbGenDemoGenerator : CodeGenBase
{
    public RzDbGenDemoGenerator(string connectionString, string outputPath) : base(connectionString, @"RzDbGen.Demo\RzDbGenDemoTemplate.cshtml", outputPath)
    {

    }
}