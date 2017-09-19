using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using RazorEngine;
using RazorEngine.Compilation.ImpromptuInterface.InvokeExt;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using RazorEngine.Text;
using RzDb.CodeGen;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;

namespace RzDb.CodeGen
{
    public abstract class CodeGenBase
    {
        public virtual string TemplatePath { get; set; } = "";
        public virtual string OutputPath { get; set; } = "";
        public virtual string ConnectionString { get; set; } = "";
        public SchemaData Schema { get; set; }
        public string[] AllowedKeys(SchemaData model)
        {
            return model.Keys.Where(k => !k.EndsWith("_Archive", StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        public CodeGenBase(string connectionString, string templatePath, string outputPath)
        {
            this.TemplatePath = templatePath;
            this.OutputPath = outputPath;
            this.ConnectionString = connectionString;
        }

        public bool ProcessTemplate(string EntityName)
        {
            this.Schema = CodeGenBase.GenerateSchemaObjects(EntityName, ConnectionString);
            return ProcessTemplate(EntityName, this.Schema);
        }
        public bool ProcessTemplate(string EntityName, SchemaData schema)
        {
            try
            {
                string assemblyBasePath = Path.GetDirectoryName(System.IO.Path.GetDirectoryName(
                            System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase)).Replace("file:\\", "");
                string FullTemplatePath = assemblyBasePath + Path.DirectorySeparatorChar + TemplatePath;
                string outputDirectory = Path.GetDirectoryName(OutputPath) + Path.DirectorySeparatorChar;
                //if (!File.Exists(EdmxPath)) throw new FileNotFoundException("EdmxPath File " + EdmxPath + " is not found");
                if (!File.Exists(FullTemplatePath)) throw new FileNotFoundException("Template File " + FullTemplatePath + " is not found");
                if (!Directory.Exists(outputDirectory)) throw new DirectoryNotFoundException("Path " + outputDirectory + " is not found");
                string result = "";
                try
                {
                    TemplateServiceConfiguration config = new TemplateServiceConfiguration();
                    config.EncodedStringFactory = new RawStringFactory(); // Raw string encoding.
                    config.Debug = true;

                    IRazorEngineService service = RazorEngineService.Create(config);
                    result = service.RunCompile(
                        new LoadedTemplateSource(File.ReadAllText(FullTemplatePath), FullTemplatePath), "templateKey",
                        typeof(SchemaData), schema);
                }
                catch (Exception exRazerEngine)
                {
                    Console.WriteLine(exRazerEngine.Message);
                    throw;
                    //throw exRazerEngine;
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
                else if (!string.IsNullOrEmpty(result))
                {
                    if (File.Exists(OutputPath)) File.Delete(OutputPath);
                    File.WriteAllText(OutputPath, result);
                }
                else
                {
                    throw new ApplicationException("The Razor Engine Produced No results for path [" + FullTemplatePath + "]");
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private static SchemaData _EdmxCache { get; set; }

        public static SchemaData GenerateSchemaObjects(string entityName, string ConnectionString)
        {
            try
            {
                SchemaData schema = new SchemaData() { Name = entityName };
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    if (connection.State == ConnectionState.Closed) connection.Open();

                    SqlCommand cmdCompat11 = new SqlCommand("DECLARE @sql NVARCHAR(1000) = 'ALTER DATABASE ' + DB_NAME() + ' SET COMPATIBILITY_LEVEL = 110'; EXECUTE sp_executesql @sql", connection);
                    cmdCompat11.CommandType = CommandType.Text;
                    cmdCompat11.ExecuteNonQuery();
                    using (SqlCommand cmd = new SqlCommand(FKSQL, connection))
                    {
                        cmd.CommandType = CommandType.Text;

                        var ds = new DataSet();
                        var adapter = new SqlDataAdapter(cmd);
                        adapter.Fill(ds);
                        var tableColumnData = ds.Tables[0];
                        var CurrentEntityName = "";
                        EntityType entityType = null;
                        PrimaryKeyProperties primaryKeyList = null; 
                        foreach (DataRow row in tableColumnData.Rows)
                        {
                            var tableName = row["TABLENAME"].ToString();
                            if (CurrentEntityName != tableName)
                            {
                                if (CurrentEntityName.Length > 0)
                                {
                                    //Temporal views are handled below
                                    if ((!entityType.HasPrimaryKeys() && (!entityType.IsTemporalView) && (entityType.TemporalType!= "HISTORY_TABLE"))) 
                                    {
                                        Console.WriteLine("Warning... '" + CurrentEntityName + "' no primary keys.. adding all as primary keys");
                                        int order = 0;
                                        foreach (var prop in entityType.Properties.Values)
                                        {
                                            order++;
                                            prop.IsKey = true;
                                            prop.KeyOrder = order;
                                            entityType.PrimaryKeys.Add(prop);
                                        }
                                    }
                                    schema.Add(CurrentEntityName, entityType);
                                }
                                entityType = new EntityType() { Name = tableName
                                    , Type = row["OBJECT_TYPE"].ToString()
                                    , Schema = row["SCHEMANAME"].ToString()
                                    , IsTemporalView = ((tableName.EndsWith("TemporalView")) && (row["OBJECT_TYPE"].ToString() == "VIEW"))
                                    , TemporalType = (row["TEMPORAL_TYPE_DESC"] == DBNull.Value ? "" : row["TEMPORAL_TYPE_DESC"].ToString())
                                };
                                primaryKeyList = new PrimaryKeyProperties(entityType);
                                entityType.PrimaryKeys = primaryKeyList;
                                CurrentEntityName = tableName;
                            }
                            Property property = new Property() { IsNullable =  (bool)row["IS_NULLABLE"] };
                            property.Name = (row["COLUMNNAME"] == DBNull.Value ? "" : row["COLUMNNAME"].ToString());
                            property.MaxLength = (int)(row["CHARACTER_MAXIMUM_LENGTH"] == DBNull.Value ? 0 : row["CHARACTER_MAXIMUM_LENGTH"]);
                            property.Precision = (int)(row["NUMERIC_PRECISION"] == DBNull.Value ? 0 : Convert.ToInt32(row["NUMERIC_PRECISION"]));
                            property.Scale = (int)(row["NUMERIC_SCALE"] == DBNull.Value ? 0 : Convert.ToInt32(row["NUMERIC_SCALE"]));
                            property.IsIdentity = (bool)(row["IS_IDENTITY"] == DBNull.Value ? false : row["IS_IDENTITY"]);
                            property.IsKey = (bool)(row["PRIMARY_KEY_ORDER"] == DBNull.Value ? false : true);
                            property.KeyOrder = (int)(row["PRIMARY_KEY_ORDER"] == DBNull.Value ? 0 : Convert.ToInt32(row["PRIMARY_KEY_ORDER"]));
                            if ((entityType.IsTemporalView) && ((property.Name == "SysEndTime") || (property.Name == "SysStartTime")))
                            {
                                property.IsKey = true;
                                property.KeyOrder = ((property.Name == "SysStartTime") ? 1 : 2);
                            }
                            property.Type = (row["DATA_TYPE"] == DBNull.Value ? "" : row["DATA_TYPE"].ToString());
                            if (property.IsKey) entityType.PrimaryKeys.Add(property);
                            entityType.Properties.Add(property.Name, property);
                        }
                        if (CurrentEntityName.Length > 0)
                        {
                            //Temporal views are handled abolve
                            if ((!entityType.HasPrimaryKeys() && (!entityType.IsTemporalView) && (entityType.TemporalType != "HISTORY_TABLE")))
                            {
                                Console.Write("Warning... '" + CurrentEntityName + "' no primary keys.. adding all as primary keys");
                                int order = 0;
                                foreach (var prop in entityType.Properties.Values)
                                {
                                    order++;
                                    prop.IsKey = true;
                                    prop.KeyOrder = order;
                                    entityType.PrimaryKeys.Add(prop);
                                }
                            }
                            schema.Add(CurrentEntityName, entityType);
                        }
                        var tableFKeyData = ds.Tables[1];
                        foreach (DataRow row in tableFKeyData.Rows)
                        {
                            var tableName = row["EntityTable"].ToString();

                            string fromEntity = (row["EntityTable"] == DBNull.Value ? "" : row["EntityTable"].ToString());
                            string fromEntityField = (row["EntityColumn"] == DBNull.Value ? "" : row["EntityColumn"].ToString());
                            string toEntity = (row["RelatedTable"] == DBNull.Value ? "" : row["RelatedTable"].ToString());
                            string toEntityField = (row["RelatedColumn"] == DBNull.Value ? "" : row["RelatedColumn"].ToString());
                            string toEntityColumnName = toEntityField.AsFormattedName();
                            string fromEntityColumnName = fromEntityField.AsFormattedName();
                            string multiplicity = (row["Multiplicity"] == DBNull.Value ? "" : row["Multiplicity"].ToString());
                            var newRel = new Relationship()
                            {
                                Name = row["FK_Name"] == DBNull.Value ? "" : row["FK_Name"].ToString(),
                                FromTableName = fromEntity,
                                FromFieldName = fromEntityField,
                                FromColumnName = fromEntityColumnName,
                                ToFieldName = toEntityField,
                                ToTableName = toEntity,
                                ToColumnName = toEntityColumnName,
                                Type = multiplicity
                            };

                            schema[tableName].Relationships.Add(newRel);
                            var fieldToMarkRelation = (tableName.Equals(newRel.FromTableName) ? newRel.FromFieldName : newRel.ToFieldName);
                            if (schema[tableName].Properties.ContainsKey(fieldToMarkRelation))
                            {
                                schema[tableName].Properties[fieldToMarkRelation].RelatedTo.Add(newRel);
                            }
                        }

                    }

                    if (schema.ContainsKey("sysdiagrams")) schema.Entities.Remove("sysdiagrams");
                    if (schema.ContainsKey("DeploymentMetadata")) schema.Entities.Remove("DeploymentMetadata");

                    //REMOVE TEMPORAL TABLES
                    foreach (var t in schema.Keys.Where(k => k.EndsWith("Temporal", StringComparison.OrdinalIgnoreCase)).ToArray()) schema.Entities.Remove(t);
                    foreach (var t in schema.Keys.Where(k => k.ToLower().Contains("sysdiagram")).ToArray()) schema.Entities.Remove(t);
                    foreach (var t in schema.Keys.Where(k => k.Contains("TemporalHistoryFor")).ToArray()) schema.Entities.Remove(t);
                    //REMOVE Redgate tables
                    foreach (var t in schema.Keys.Where(k => k.EndsWith("DeploymentMetadata", StringComparison.OrdinalIgnoreCase)).ToArray()) schema.Entities.Remove(t);
                    var temporal = schema.Keys.Where(k => k.EndsWith("_Archive", StringComparison.OrdinalIgnoreCase)).ToArray();
                    foreach (var t in temporal)
                    {
                        schema.Entities.Remove(t);
                    }

                    SqlCommand cmdCompat13 = new SqlCommand("DECLARE @sql NVARCHAR(1000) = 'ALTER DATABASE ' + DB_NAME() + ' SET COMPATIBILITY_LEVEL = 130'; EXECUTE sp_executesql @sql", connection);
                    cmdCompat13.CommandType = CommandType.Text;
                    cmdCompat13.ExecuteNonQuery();

                    return schema;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static string FKSQL = @"
SET NOCOUNT ON
/*
DROP TABLE #IDX;
DROP TABLE #COL;
DROP TABLE #FK;
*/
SELECT DISTINCT
	s.name + '.' + t.name + '.' + c.name AS 'FULL_COLUMN_NAME',
	s.name as SCHEMANAME,
	t.name AS TABLENAME,
	c.name AS COLUMNNAME,
	ic.ORDINAL_POSITION,
	c.is_nullable AS IS_NULLABLE,
	c.is_identity AS IS_IDENTITY,
	(CASE WHEN cu4pk.CONSTRAINT_NAME IS NOT NULL THEN 
		ROW_NUMBER() over(PARTITION BY cu4pk.CONSTRAINT_NAME 
						  ORDER BY ic.ORDINAL_POSITION ASC) 
	ELSE NULL
	END) AS PRIMARY_KEY_ORDER, 
	cu4pk.CONSTRAINT_NAME AS PRIMARY_KEY_CONSTRAINT_NAME,
	ic.DATA_TYPE,
	ic.CHARACTER_MAXIMUM_LENGTH,
	ic.NUMERIC_PRECISION,
	ic.NUMERIC_PRECISION_RADIX,
	ic.NUMERIC_SCALE,
	ic.DATETIME_PRECISION,
	(CASE t.[type]
		WHEN 'U' THEN 'TABLE'
		WHEN 'V' THEN 'VIEW'
		ELSE t.[type]
	END) as OBJECT_TYPE,
	(SELECT tb.temporal_type_desc FROM sys.tables tb WHERE tb.object_id = t.object_id) AS TEMPORAL_TYPE_DESC
INTO
	#COL
FROM 
/*
	sys.tables as t INNER JOIN sys.schemas as s
		ON t.schema_id = s.schema_id */
	sys.objects as t INNER JOIN sys.schemas as s 
		ON t.schema_id = s.schema_id AND t.type IN ('U', 'V') 
	INNER JOIN sys.columns as c 
		ON  c.object_id = t.object_id
	INNER JOIN INFORMATION_SCHEMA.COLUMNS ic 
		ON ic.TABLE_SCHEMA = s.name AND ic.TABLE_NAME = t.name AND ic.COLUMN_NAME = c.name
	LEFT OUTER JOIN  information_schema.table_constraints tc4pk 
		ON tc4pk.TABLE_SCHEMA = s.name AND tc4pk.TABLE_NAME = t.name AND tc4pk.constraint_type = 'PRIMARY KEY '
	LEFT OUTER JOIN  information_schema.constraint_column_usage cu4pk 
		ON cu4pk.CONSTRAINT_NAME = tc4pk.CONSTRAINT_NAME AND cu4pk.COLUMN_NAME = c.name
ORDER BY 
	s.name, t.name, ic.ORDINAL_POSITION, c.name;

select 
	SCHEMA_NAME(o.schema_id) + '.' + o.name + '.' + co.[name] AS 'FULL_COLUMN_NAME',
    i.name as IndexName, 
    o.name as TableName, 
	SCHEMA_NAME(o.schema_id) as SchemaName,
	i.is_unique,
    ic.key_ordinal as ColumnOrder,
    ic.is_included_column as IsIncluded, 
    co.[name] as ColumnName
INTO
	#IDX
from sys.indexes i 
	INNER JOIN sys.objects o 
		ON i.object_id = o.object_id
	INNER JOIN sys.index_columns ic 
		ON ic.object_id = i.object_id AND ic.index_id = i.index_id
	INNER JOIN sys.columns co 
		ON co.object_id = i.object_id AND co.column_id = ic.column_id
where 
i.[type] = 2 
--and i.is_unique = 0 
and i.is_primary_key = 0
and o.[type] IN ( 'U', 'V' )
--and ic.is_included_column = 0
order by o.[name], i.[name], ic.is_included_column, ic.key_ordinal
;

select  
  KP.TABLE_SCHEMA EntitySchema
, KP.TABLE_NAME EntityTable
, KP.COLUMN_NAME EntityColumn
, KP.TABLE_SCHEMA + '.' + KP.TABLE_NAME + '.' + KP.COLUMN_NAME AS EntityFullColumnName
, KF.TABLE_NAME RelatedTable
, KF.COLUMN_NAME RelatedColumn
, KP.TABLE_SCHEMA + '.' + KF.TABLE_NAME + '.' + KF.COLUMN_NAME AS RelatedFullColumnName
, RC.MATCH_OPTION MatchOption
, RC.CONSTRAINT_NAME FK_Name
, RC.UPDATE_RULE UpdateRule
, RC.DELETE_RULE DeleteRule
INTO
	#FK
from 
	INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC join INFORMATION_SCHEMA.KEY_COLUMN_USAGE KF 
		ON RC.CONSTRAINT_NAME = KF.CONSTRAINT_NAME
	join INFORMATION_SCHEMA.KEY_COLUMN_USAGE KP 
		ON RC.UNIQUE_CONSTRAINT_NAME = KP.CONSTRAINT_NAME
ORDER BY 
	EntitySchema, EntityTable, EntityColumn, RelatedTable, RelatedColumn;


--SELECT * FROM #IDX;
--SELECT * FROM #COL;
SELECT 
	fk.*
	, (CASE 
		WHEN entitycol.IS_NULLABLE = 0
			THEN 'One'
		WHEN entitycol.IS_NULLABLE = 1
			THEN 'ZeroOrOne'
		ELSE
			'Unknown'
	END) as RelationMultiplicityEntity
	, (CASE 
		WHEN relatedcol.IS_NULLABLE = 0
			THEN 'Many'
		ELSE
			'Many'
	END) as RelationMultiplicityRelated
	, (CASE 
		WHEN entitycol.IS_NULLABLE = 0
			THEN 'Many'
		ELSE
			'Many'
	END) as InverseRelationMultiplicityEntity
	, (CASE 
		WHEN relatedcol.IS_NULLABLE = 0
			THEN 'One'
		WHEN relatedcol.IS_NULLABLE = 1
			THEN 'ZeroOrOne'
		ELSE
			'Unknown'
	END) as InverseRelationMultiplicityRelated
INTO 
	#FKREL
FROM 
	#FK fk LEFT OUTER JOIN #COL entitycol
		ON entitycol.FULL_COLUMN_NAME = fk.EntityFullColumnName
	LEFT OUTER JOIN #IDX entityidx
		ON entityidx.FULL_COLUMN_NAME = entitycol.FULL_COLUMN_NAME
	LEFT OUTER JOIN #COL relatedcol
		ON relatedcol.FULL_COLUMN_NAME = fk.RelatedFullColumnName
	LEFT OUTER JOIN #IDX relatedidx
		ON relatedidx.FULL_COLUMN_NAME = relatedcol.FULL_COLUMN_NAME
ORDER BY
	EntitySchema, EntityTable, EntityColumn, RelatedTable, RelatedColumn
;


SELECT * FROM #COL 
--WHERE TABLENAME = 'DrillGroupsTemporal'
ORDER BY SCHEMANAME, TABLENAME, ORDINAL_POSITION;

SELECT * FROM (
SELECT f.EntitySchema AS SCHEMANAME, f.EntityTable AS TABLENAME,
	f.FK_Name
	, f.EntitySchema 
	, f.EntityTable
	, f.EntityColumn
	, f.RelatedTable
	, f.RelatedColumn
	, f.EntityFullColumnName
	, f.RelatedFullColumnName
	, (f.RelationMultiplicityEntity + ' to ' + f.RelationMultiplicityRelated) as Multiplicity
	, 1 as RelationGroupSort
FROM
	#FKREL f
UNION
SELECT frel.EntitySchema AS SCHEMANAME, frel.RelatedTable AS TABLENAME,
	frel.FK_Name
	, frel.EntitySchema 
	, frel.RelatedTable
	, frel.RelatedColumn
	, frel.EntityTable
	, frel.EntityColumn
	, frel.RelatedFullColumnName
	, frel.EntityFullColumnName
	, (frel.InverseRelationMultiplicityEntity + ' to ' + frel.InverseRelationMultiplicityRelated) as Multiplicity
	, 2 as RelationGroupSort
FROM
	#FKREL frel 
) e
WHERE 
--WHERE FK_Name = 'FK_Projects_PrimaryAssetTypes'
 NOT (	e.EntityTable = e.RelatedTable AND e.EntityColumn = e.RelatedColumn)
 
ORDER BY
	EntitySchema, EntityTable, RelationGroupSort, EntityColumn, RelatedTable, RelatedColumn

DROP TABLE #IDX;
DROP TABLE #COL;
DROP TABLE #FK;
DROP TABLE #FKREL;
";
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
