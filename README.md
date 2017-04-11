# RazorEngine Database Code Generator

This project addresses, what I consider, a major shortcomming of T4 templates.   T4 templates are great, but they have the following issues:
	
	1. T4 templates are notoriously hard to debug
	2. No Intellisense support
 	3. The template representation looks quite different from what is generated
	4. The meta-data for edmx is difficult to work with (relationships are in a completely different location than entity,  finding keys is cumbersome)
	5. T4 POCO code generation cannot handle many to many relationships or tables with 2 or more foriegn keys to one table 
	6. Dealing with multiple file output is not intuitive
	
This project attempts to solve this by reading the edmx file generated from an ADO.NET Entity object and create a simple representation of the schema.  It then passes 
this simplified representation of the schema to a cshtml template for code generation.   

## Getting Started

Simply clone the respository https://github.com/rvegajr/rzdb-code-gen and run the application.  

### Prerequisites

You will need Visual Studio 2015+ (Community Edition will be fine).  
VS2017 Debugging Issue:  For some odd reason,  breakpoints do not stop in VS2017,  but it debugs fine on 2015.  Templates are still generated as expected,  we just can't step through the cshtml.

### Installing

Open the app and start debugging.   The application, after it is completed, will open a file in explorer with the results of the code generation,

## Concepts
```HTML
Given that these are cshtml templates, they are paricularly sensitive to html tags.  To deal with this, the application uses a <t> tag to note when template text starts and ends. 
It also uses the standard "@" and "{}" razor syntax to interpest code control statements.  
* <t></t> - is used to deal with marking the beginning and ending of text (these will be removed on template rendering)
* <t/> or <t /> are used as ways to seperate variables from text (useful for those situations where you have a variable name but no space by the template text, these will be removed on template rendering)
* $OUTPUT_PATH$ - The Output that is passed to the template when .ProcessTemplate() is executed
* ##FILE= - After code generation, the application will find this sequence of characters and parse the file based on this name. Since this happens after rendering, you can use schema objects the affect the name  
```
## How to create your own template

Lets create a template!
* In the solution explorer, select the project 'RzDbCodeGen' and create a folder off the project root called "EdmxGen.PropertyDump"
* Create a new class in this newly created folder and call it "EdmxGen.PropertyDumpGenerator.cs",  paste the following code 
```
using RzDb.CodeGen;
public class EdmxGenPropertyDumpGenerator : EdmxCodeGenBase
{
    public EdmxGenPropertyDumpGenerator(string edmxPath, string outputPath) : base(edmxPath, @"EdmxGen.PropertyDump\EdmxGen.PropertyDumpTemplate.cshtml", outputPath)
    {

    }
}
```
* Create another new class in this newly created folder and call it "EdmxGen.PropertyDumpTemplate.cshtml",  paste the following code 
```
@using System.Collections.Generic
@using RzDb.CodeGen
@{ SchemaData _Model = (SchemaData)Model; }
@foreach (string key in _Model.Entities.Keys)

{<t>##FILE=$OUTPUT_PATH$Entity_@key<t/>.cs
Key is @key @foreach (KeyValuePair<string, Property> item in _Model[key].Properties)
{
    <t>
        -PropertyName: @item.Value.Name  @(item.Value.IsKey ? "Is Key!!" : "")
        @foreach (Relationship relate in item.Value.RelatedTo)
        {
            <t>-   Relation:  @relate.FromFieldName  to @relate.ToTableName<t />.@relate.ToFieldName as @relate.Type</t>
        }
    </t>}
</t>}
```
* VS2015 ONLY - set a breakpoint in line 4 in EdmxGen.PropertyDumpTemplate.cshtml  (skip all breakpoint steps if you are using VS2017)
* Go To <App Root>Console.cs,  edit line 16 and comment it out
```
            // Code that goes into RzDb.CodeGenerations.tt file starts here --- 

            new EdmxGenDemoGenerator(edmxFile, outputPath).ProcessTemplate();

            // End of the Code that goes into RzDb.CodeGenerations.tt 
```
To -> 
```
            // Code that goes into RzDb.CodeGenerations.tt file starts here --- 

            //new EdmxGenDemoGenerator(edmxFile, outputPath).ProcessTemplate();
            new EdmxGenPropertyDumpGenerator(edmxFile, outputPath).ProcessTemplate();

            // End of the Code that goes into RzDb.CodeGenerations.tt 
```
* Start debugging - it should stop in the template.  Stop debugging. 
* Go to file EdmxGen.PropertyDump\EdmxGen.PropertyDumpTemplate.cshtml,  lets add a data type dump, go to line 11.. change it from
```
                -PropertyName: @item.Value.Name  @(item.Value.IsKey ? "Is Key!!" : "")  
```
To (which will dump sql data type, .net data type and max length) -> 
```
                -PropertyName: @item.Value.Name  @(item.Value.IsKey ? "Is Key!!" : "")  SQLDataType: @item.Value.Type   .NETDataType: @item.Value.Type.ToNetType()   Len: @item.Value.MaxLength
```
* Clear out the pevious break point if applicable,  start the app.  it should open explorer with the generated code.

## Adding this to the build process

The app contains a t4 template that is designed to load the assembly and execute the code generation.  This .tt template will be added to the same path as the data generation path,  so whenever the edmx file is generated,  it should kick off the code generation project. Note that you will need to set the Project Type to "Class Library" instead of console app when you include it in your host project.

## Built With

* [RazorEngine](https://antaris.github.io/RazorEngine/) - Template engine that uses cshtml for code engine
* [EntityFramework 6](https://www.nuget.org/packages/EntityFramework/) - Need the class libraries that reads edmx files

## Authors

* **Ricky Vega** - *Initial work* - [RickyVega](https://github.com/rvegajr)

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details
