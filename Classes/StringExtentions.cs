using System;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;
using System.Linq;
//Thanks https://www.codeproject.com/tips/1081932/tosingular-toplural-string-extensions
namespace RzDb.CodeGen
{
    public static class StringExtensions
    {
        private static readonly PluralizationService service =
            PluralizationService.CreateService(CultureInfo.GetCultureInfo("en-US"));

        private static Dictionary<string, string> sQLDataTypeToDotNetDataType = new Dictionary<string, string>();
        private static Dictionary<string, string> sQLDataTypeToJsDataType = new Dictionary<string, string>();

        static StringExtensions()
        {
            var mapping = (ICustomPluralizationMapping)service;
            mapping.AddWord("Cactus", "Cacti");
            mapping.AddWord("cactus", "cacti");
            mapping.AddWord("Die", "Dice");
            mapping.AddWord("die", "dice");
            mapping.AddWord("Equipment", "Equipment");
            mapping.AddWord("equipment", "equipment");
            mapping.AddWord("Money", "Money");
            mapping.AddWord("money", "money");
            mapping.AddWord("Nucleus", "Nuclei");
            mapping.AddWord("nucleus", "nuclei");
            mapping.AddWord("Quiz", "Quizzes");
            mapping.AddWord("quiz", "quizzes");
            mapping.AddWord("Shoe", "Shoes");
            mapping.AddWord("shoe", "shoes");
            mapping.AddWord("Syllabus", "Syllabi");
            mapping.AddWord("syllabus", "syllabi");
            mapping.AddWord("Testis", "Testes");
            mapping.AddWord("testis", "testes");
            mapping.AddWord("Virus", "Viruses");
            mapping.AddWord("virus", "viruses");
            mapping.AddWord("Water", "Water");
            mapping.AddWord("water", "water");
            mapping.AddWord("Lease", "Leases");
            mapping.AddWord("lease", "leases");
            mapping.AddWord("IncreaseDecrease", "IncreaseDecreases");
            mapping.AddWord("increaseDecrease", "increaseDecreases");
            mapping.AddWord("ScenarioCase", "ScenarioCases");
            mapping.AddWord("scenarioCase", "scenarioCases");
            mapping.AddWord("OpStatus", "OpStatuses");
            mapping.AddWord("opStatus", "opStatuses");
            mapping.AddWord("ConstructionStatus", "ConstructionStatuses");
            mapping.AddWord("constructionStatus", "constructionStatuses");
            if (sQLDataTypeToJsDataType.Count == 0)
            {
                sQLDataTypeToJsDataType.Add("bigint", "number");
                sQLDataTypeToJsDataType.Add("binary", "object");
                sQLDataTypeToJsDataType.Add("bit", "boolean");
                sQLDataTypeToJsDataType.Add("char", "string");
                sQLDataTypeToJsDataType.Add("date", "Date");
                sQLDataTypeToJsDataType.Add("datetime", "Date");
                sQLDataTypeToJsDataType.Add("datetime2", "Date");
                sQLDataTypeToJsDataType.Add("datetimeoffset", "Date");
                sQLDataTypeToJsDataType.Add("decimal", "number");
                sQLDataTypeToJsDataType.Add("varbinary", "object");
                sQLDataTypeToJsDataType.Add("float", "number");
                sQLDataTypeToJsDataType.Add("image", "object");
                sQLDataTypeToJsDataType.Add("int", "number");
                sQLDataTypeToJsDataType.Add("money", "number");
                sQLDataTypeToJsDataType.Add("nchar", "string");
                sQLDataTypeToJsDataType.Add("ntext", "string");
                sQLDataTypeToJsDataType.Add("numeric", "number");
                sQLDataTypeToJsDataType.Add("nvarchar", "string");
                sQLDataTypeToJsDataType.Add("real", "number");
                sQLDataTypeToJsDataType.Add("rowversion", "object");
                sQLDataTypeToJsDataType.Add("smalldatetime", "Date");
                sQLDataTypeToJsDataType.Add("smallint", "number");
                sQLDataTypeToJsDataType.Add("smallmoney", "number");
                sQLDataTypeToJsDataType.Add("sql_variant", "object");
                sQLDataTypeToJsDataType.Add("text", "string");
                sQLDataTypeToJsDataType.Add("time", "Date");
                sQLDataTypeToJsDataType.Add("timestamp", "Date");
                sQLDataTypeToJsDataType.Add("tinyint", "number");
                sQLDataTypeToJsDataType.Add("uniqueidentifier", "string");
                sQLDataTypeToJsDataType.Add("varchar", "string");
                sQLDataTypeToJsDataType.Add("varchar(max)", "string");
                sQLDataTypeToJsDataType.Add("nvarchar(max)", "string");
                sQLDataTypeToJsDataType.Add("varbinary(max)", "object");
                sQLDataTypeToJsDataType.Add("xml", "string");
                sQLDataTypeToJsDataType.Add("geometry", "object");
                sQLDataTypeToJsDataType.Add("geography", "object");
            }
            if (sQLDataTypeToDotNetDataType.Count == 0)
            {
                sQLDataTypeToDotNetDataType.Add("bigint", "System.Int64");
                sQLDataTypeToDotNetDataType.Add("binary", "Byte[]");
                sQLDataTypeToDotNetDataType.Add("bit", "bool");
                sQLDataTypeToDotNetDataType.Add("char", "string");
                sQLDataTypeToDotNetDataType.Add("date", "DateTime");
                sQLDataTypeToDotNetDataType.Add("datetime", "DateTime");
                sQLDataTypeToDotNetDataType.Add("datetime2", "DateTime");
                sQLDataTypeToDotNetDataType.Add("datetimeoffset", "DateTimeOffset");
                sQLDataTypeToDotNetDataType.Add("decimal", "decimal");
                sQLDataTypeToDotNetDataType.Add("varbinary", "Byte[]");
                sQLDataTypeToDotNetDataType.Add("float", "double");
                sQLDataTypeToDotNetDataType.Add("image", "Byte[]");
                sQLDataTypeToDotNetDataType.Add("int", "int");
                sQLDataTypeToDotNetDataType.Add("money", "decimal");
                sQLDataTypeToDotNetDataType.Add("nchar", "string");
                sQLDataTypeToDotNetDataType.Add("ntext", "string");
                sQLDataTypeToDotNetDataType.Add("numeric", "decimal");
                sQLDataTypeToDotNetDataType.Add("nvarchar", "string");
                sQLDataTypeToDotNetDataType.Add("real", "double");
                sQLDataTypeToDotNetDataType.Add("rowversion", "Byte[]");
                sQLDataTypeToDotNetDataType.Add("smalldatetime", "DateTime");
                sQLDataTypeToDotNetDataType.Add("smallint", "short");
                sQLDataTypeToDotNetDataType.Add("smallmoney", "decimal");
                sQLDataTypeToDotNetDataType.Add("sql_variant", "object");
                sQLDataTypeToDotNetDataType.Add("text", "string");
                sQLDataTypeToDotNetDataType.Add("time", "TimeSpan");
                sQLDataTypeToDotNetDataType.Add("timestamp", "Byte[]");
                sQLDataTypeToDotNetDataType.Add("tinyint", "Byte");
                sQLDataTypeToDotNetDataType.Add("uniqueidentifier", "Guid");
                sQLDataTypeToDotNetDataType.Add("varchar", "string");
                sQLDataTypeToDotNetDataType.Add("varchar(max)", "string");
                sQLDataTypeToDotNetDataType.Add("nvarchar(max)", "string");
                sQLDataTypeToDotNetDataType.Add("varbinary(max)", "Byte[]");
                sQLDataTypeToDotNetDataType.Add("xml", "Xml");
                sQLDataTypeToDotNetDataType.Add("geometry", "DbGeometry");
                sQLDataTypeToDotNetDataType.Add("geography", "DbGeography");
            }

        }

        public static string ToSingular(this string word)
        {
            if (word == null)
                throw new ArgumentNullException("word");

            bool isUpperWord = (string.Compare(word, word.ToUpper(), false) == 0);
            if (isUpperWord)
            {
                string lowerWord = word.ToLower();
                return (service.IsSingular(lowerWord) ? lowerWord :
                    service.Singularize(lowerWord)).ToUpper();
            }

            return (service.IsSingular(word) ? word : service.Singularize(word));
        }

        public static string ToNetType(this string sqlType)
        {
            if (sqlType == null)
                throw new ArgumentNullException("sqlType");
            if (sqlType.Contains("varchar")) return "string";
            return (sQLDataTypeToDotNetDataType.ContainsKey(sqlType) ? sQLDataTypeToDotNetDataType[sqlType] : sqlType);
        }

        public static string ToNetType(this string sqlType, bool isNullable)
        {
            var ret = "";
            if (sqlType == null)
                throw new ArgumentNullException("sqlType");
            if (sqlType.Contains("varchar"))
                ret = "string";
            else
                ret = (sQLDataTypeToDotNetDataType.ContainsKey(sqlType) ? sQLDataTypeToDotNetDataType[sqlType] : sqlType);
            return ret + ((isNullable && !(ret.Equals("string") || ret.Equals("DbGeometry") || ret.Equals("DbGeography") || ret.EndsWith("[]"))) ? "?" : "");
        }


        public static string ToJsType(this string sqlType)
        {
            if (sqlType == null)
                throw new ArgumentNullException("sqlType");
            if (sqlType.Contains("varchar")) return "string";
            return (sQLDataTypeToJsDataType.ContainsKey(sqlType) ? sQLDataTypeToJsDataType[sqlType] : "object");
        }

        public static string ToJsType(this string sqlType, bool isNullable)
        {
            var ret = "object";
            if (sqlType == null)
                throw new ArgumentNullException("sqlType");
            if (sqlType.Contains("varchar"))
                ret = "string";
            else
                ret = (sQLDataTypeToJsDataType.ContainsKey(sqlType) ? sQLDataTypeToJsDataType[sqlType] : sqlType);
            return ret + (isNullable ? " | null" : "");
        }
        public static string ToPlural(this string word)
        {
            if (word == null)
                throw new ArgumentNullException("word");

            bool isUpperWord = (string.Compare(word, word.ToUpper(), false) == 0);
            if (isUpperWord)
            {
                string lowerWord = word.ToLower();
                return (service.IsPlural(lowerWord) ? lowerWord :
                    service.Pluralize(lowerWord)).ToUpper();
            }

            return (service.IsPlural(word) ? word : service.Pluralize(word));
        }

        public static string AsFormattedName(this string word)
        {
            if (word == null)
                return null;
            
            if (word.EndsWith("ID")) word = word.Substring(0, word.Length - 2);
            if (word.EndsWith("UID")) word =  word.Substring(0, word.Length - 3);
            if (word.EndsWith("Id")) word = word.Substring(0, word.Length - 2);
            return word;

        }

        public static string ToSnakeCase(this string str)
        {
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x.ToString() : x.ToString())).ToLower();
        }
    }
}