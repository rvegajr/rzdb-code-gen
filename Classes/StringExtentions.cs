using System;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;
//Thanks https://www.codeproject.com/tips/1081932/tosingular-toplural-string-extensions
namespace RzDb.CodeGen
{
    public static class StringExtensions
    {
        private static readonly PluralizationService service =
            PluralizationService.CreateService(CultureInfo.GetCultureInfo("en-US"));

        private static Dictionary<string, string> sQLDataTypeToDotNetDataType = new Dictionary<string, string>();
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
                sQLDataTypeToDotNetDataType.Add("xml", "Xml");
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
            return ret + ((isNullable && !ret.Equals("string")) ? "?" : "");
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
    }
}