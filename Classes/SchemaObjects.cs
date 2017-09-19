using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace RzDb.CodeGen
{
    public class Property
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int MaxLength { get; set; }
        public int Precision { get; set; }
        public int Scale { get; set; }
        public bool IsNullable { get; set; }
        public bool IsKey { get; set; }
        public int KeyOrder { get; set; }
        public bool IsIdentity { get; set; }

        public Relationships RelatedTo = new Relationships();
    }

    public static class TransformHelper
    {
        public static string[] AllowedKeys(SchemaData model)
        {
            return model.Keys.Where(k => !k.EndsWith("_Archive", StringComparison.OrdinalIgnoreCase)).ToArray();
        }
    }



    public enum RelationshipType
    {
        OneToMany,
        ZeroOrOneToMany,
        ManyToOne,
        ManyToZeroOrOne

    }
    public class Relationships : List<Relationship>
    {
        public List<Relationship> Fetch(RelationshipType TypeToFetch)
        {
            List<Relationship> retListRaw = new List<Relationship>();
            if (TypeToFetch == RelationshipType.OneToMany) retListRaw = this.Where(r => r.Type == "One to Many").ToList();
            else if (TypeToFetch == RelationshipType.ZeroOrOneToMany) retListRaw = this.Where(r => ((r.Type == "One to Many") || (r.Type == "ZeroOrOne to Many"))).ToList();
            else if (TypeToFetch == RelationshipType.ManyToOne) retListRaw = this.Where(r => r.Type == "Many to One").ToList();
            else if (TypeToFetch == RelationshipType.ManyToZeroOrOne) retListRaw = this.Where(r => ((r.Type == "Many to ZeroOrOne") || (r.Type == "Many to One"))).ToList();
            List<Relationship> retList = new List<Relationship>();
            if (retListRaw.Count > 0)
            {
                //this should sort equal field names before non equal field names,  this will handle relationship with duplicate names correctly
                foreach (var item in retListRaw) if (item.ToFieldName.Equals(item.FromFieldName)) retList.Add(item);
                foreach (var item in retListRaw) if (!item.ToFieldName.Equals(item.FromFieldName)) retList.Add(item);
            }

            return retList;
        }

        public static int CountItems(List<Relationship> list, string searchFor)
        {
            return CountItems(list, RelationSearchField.ToTableName, searchFor);
        }

        public static int CountItems(List<Relationship> list, RelationSearchField searchField, string searchFor)
        {
            if (searchField == RelationSearchField.ToTableName) return list.Count(r => r.ToTableName == searchFor);
            else if (searchField == RelationSearchField.ToColumnName) return list.Count(r => r.ToColumnName == searchFor);
            else if (searchField == RelationSearchField.ToFieldName) return list.Count(r => r.ToFieldName == searchFor);
            else if (searchField == RelationSearchField.FromTableName) return list.Count(r => r.FromTableName == searchFor);
            else if (searchField == RelationSearchField.FromFieldName) return list.Count(r => r.FromFieldName == searchFor);
            else if (searchField == RelationSearchField.FromColumnName) return list.Count(r => r.FromColumnName == searchFor);
            else return 0;
        }
    }
    public enum RelationSearchField
    {
        ToColumnName
        , ToTableName
        , ToFieldName
        , FromTableName
        , FromFieldName
        , FromColumnName
    }
    public class Relationship
    {
        public string Name { get; set; }
        public string FromTableName { get; set; }
        public string FromFieldName { get; set; }
        public string FromColumnName { get; set; }
        public string ToTableName { get; set; }
        public string ToFieldName { get; set; }
        public string ToColumnName { get; set; }
        public string Type { get; set; }
    }

    public class PrimaryKeyProperties : List<Property>
    {
        protected EntityType Entity;
        public PrimaryKeyProperties(EntityType Parent)
        {
            this.Entity = Parent;
        }
        /// <summary>
        /// Useful for rendering the primary keys for a comma delimited parameter list
        /// Will return the primary keys in the following format
        ///   [0]Parm1 (number), Parm2(text), Parm3 (number)
        ///   will return
        ///   int Parm1, string Parm2, int Parm3
        /// </summary>
        /// <param name="delimiter">Defaults to ","</param>
        /// <param name="elementSet">Defaults to " "</param>
        /// <returns></returns>
        public string AsParmString(string prefix, string delimiter, string elementSet)
        {
            var ret = "";
            for(int i = 0; i < this.Entity.PrimaryKeys.Count; i++)
            {
                var property = this.Entity.PrimaryKeys[i];
                ret += (i > 0 ? delimiter + @" " : @" ") + prefix + property.Type.ToNetType() + elementSet + property.Name.ToSingular();
            }
            return ret;
        }
        /// <summary>
        /// Useful for rendering the primary keys for a comma delimited parameter list
        /// Will return the primary keys in the following format
        ///   [0]Parm1 (number), Parm2(text), Parm3 (number)
        ///   will return
        ///   int Parm1, string Parm2, int Parm3
        /// </summary>
        public string AsParmString()
        {
            return this.AsParmString("[FromODataUri] ", ",", " ");
        }

        /// <summary>
        /// Useful for rendering the primary keys for a linq search query
        /// Will return the primary keys in the following format
        ///   [0]Parm1 (number), Parm2(text), Parm3 (number)
        ///   will return
        ///   t.Parm1 == Parm1 and t.Parm2 == Parm2 and t.Parm3 == Parm3
        /// </summary>
        /// <param name="prefix">Defaults to "t"</param>
        /// <param name="delimiter">Defaults to " and "</param>
        /// <param name="elementSet">Defaults to " == "</param>
        /// <returns></returns>
        public string AsLinqEquationString(string prefix, string delimiter, string elementSet)
        {
            // t.@_Model[key].PrimaryKeys[0].Name == @key.ToSingular()
            var ret = "";
            for (int i = 0; i < this.Entity.PrimaryKeys.Count; i++)
            {
                var property = this.Entity.PrimaryKeys[i];
                ret += (i > 0 ? delimiter + @" " : @" ") + prefix + (prefix.Length>0 ? "." : "") + property.Name + elementSet + property.Name.ToSingular();
            }
            return ret;
        }

        /// <summary>
        /// Useful for rendering the primary keys for a linq search query
        /// Will return the primary keys in the following format
        ///   [0]Parm1 (number), Parm2(text), Parm3 (number)
        ///   will return
        ///   t.Parm1 == Parm1 and t.Parm2 == Parm2 and t.Parm3 == Parm3
        /// </summary>
        /// <returns>t.Parm1 == Parm1 and t.Parm2 == Parm2 and t.Parm3 == Parm3</returns>
        public string AsLinqEquationString()
        {
            return AsLinqEquationString("t", " &&", "==");
        }

        public string AsODataRouteString()
        {
            var ret = "";
            if (this.Entity.PrimaryKeys.Count==0)
            {
                return "";
            }
            //To keep previous functionality,  if there is only 1 key,  it will only render the single parm name
            else if (this.Entity.PrimaryKeys.Count == 1)
            {
                var property = this.Entity.PrimaryKeys[0];
                ret = "{" + property.Name + "}";
            } else
            {
                for (int i = 0; i < this.Entity.PrimaryKeys.Count; i++)
                {
                    var property = this.Entity.PrimaryKeys[i];
                    ret += (i > 0 ? @", " : @" ") + property.Name + "={" + property.Name + "}";
                }
            }
            return "(" + ret + ")";
        }

        /// <summary>
        /// Useful for rendering the primary keys for a FindAsync
        /// Will return the primary keys in the following format
        ///   [0]Parm1 (number), Parm2(text), Parm3 (number)
        ///   will return
        ///   Parm1, Parm2, Parm3
        /// </summary>
        /// <param name="varPrefix">What the variable will be prefixed with.</param>
        public string AsCsvString(string varPrefix)
        {
            var ret = "";
            for (int i = 0; i < this.Entity.PrimaryKeys.Count; i++)
            {
                var property = this.Entity.PrimaryKeys[i];
                ret += (i > 0 ? @", " : @" ") + ((varPrefix.Length > 0) ? varPrefix + "." : "") + property.Name;
            }
            return ret;
        }
        public string AsCsvString()
        {
            return this.AsCsvString("");
        }

        /// <summary>
        /// Useful for rendering the keys for comparing if a request will be valid
        /// Will return the primary keys in the following format
        ///   [0]Parm1 (number), Parm2(text), Parm3 (number) with varPrefix=item
        ///   will return
        ///   (Parm1 == item.Parm1) && (Parm2 == item.Parm2) && (Parm3 == item.Parm3)
        /// </summary>
        /// <param name="varPrefix">What the variable will be prefixed with.</param>
        public string AsParmBooleanCheck(string varPrefix)
        {
            var ret = "";
            for (int i = 0; i < this.Entity.PrimaryKeys.Count; i++)
            {
                var property = this.Entity.PrimaryKeys[i];
                ret += (i > 0 ? @" && " : @"") + property.Name + " == " + varPrefix + "." + property.Name;
            }
            return ret;
        }


    }
    public class EntityType
    {
        public EntityType()
        {
            this.PrimaryKeys = new PrimaryKeyProperties(this);
        }
        public string Name { get; set; }
        public string Schema { get; set; }
        public string Type { get; set; }
        public string TemporalType { get; set; }
        public Dictionary<string, Property> Properties = new Dictionary<string, Property>();
        public Relationships Relationships = new Relationships();
        public PrimaryKeyProperties PrimaryKeys;
        public bool IsTemporalView { get; set; }
        public bool HasPrimaryKeys()
        {
            foreach (var prop in Properties.Values)
                if (prop.IsKey) return true;
            return false;
        }
    }

    public class SchemaData
    {
        public string Name = "";
        private Dictionary<string, EntityType> _entities = new Dictionary<string, EntityType>();
        public Dictionary<string, EntityType> Entities
        {
            get { return _entities; }
            set { _entities = value; }
        }
        public EntityType this[string entityName]
        {
            get { return _entities[entityName]; }
            set { _entities[entityName] = value; }
        }
        public Dictionary<string, EntityType>.KeyCollection Keys
        {
            get
            {
                return _entities.Keys;
            }
        }

        public void Add(string entityName, EntityType entity)
        {
            if (_entities.ContainsKey(entityName))
            {
                _entities.Remove(entityName);
            }
            _entities.Add(entityName, entity);
        }

        public bool ContainsKey(string entityName)
        {
            return _entities.ContainsKey(entityName);
        }

        public bool ContainsValue(EntityType entity)
        {
            return _entities.ContainsValue(entity);
        }
    }



    public static class PropertyExtensions
    {
        public static Int64 NextInt64(this Random rnd)
        {
            var buffer = new byte[sizeof(Int64)];
            rnd.NextBytes(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }
        public static bool NextBool(this Random rnd)
        {
            return rnd.Next(0, 1).Equals(0);
        }
        public static double NextDouble(this Random rnd)
        {
            var buffer = new byte[sizeof(Int64)];
            rnd.NextBytes(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }
        public static Single NextSingle(this Random rnd)
        {
            var buffer = new byte[sizeof(Int64)];
            rnd.NextBytes(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }
        public static Int16 NextInt16(this Random rnd)
        {
            var buffer = new byte[sizeof(Int16)];
            rnd.NextBytes(buffer);
            return BitConverter.ToInt16(buffer, 0);
        }
        public static Decimal NextDecimal(this Random rnd)
        {
            var buffer = new byte[sizeof(Int16)];
            rnd.NextBytes(buffer);
            return BitConverter.ToInt16(buffer, 0);
        }
        public static Char NextChar(this Random rnd)
        {
            var buffer = new byte[sizeof(Char)];
            rnd.NextBytes(buffer);
            return BitConverter.ToChar(buffer, 0);
        }
        public static DateTime NextDateTime(this Random rnd)
        {
            return DateTime.UtcNow.AddDays(rnd.Next(120));
        }
        public static DateTime NextDateTime2(this Random rnd)
        {
            return Faker.DateTimeFaker.DateTime();
        }
        public static string NextString(this Random rnd)
        {
            return Faker.Lorem.GetFirstWord();
        }
        public static string NextText(this Random rnd)
        {
            return Faker.Lorem.Paragraph(rnd.Next(1, 4));
        }
        public static string NextText(this Random rnd, int maxlength)
        {
            var s = Faker.Lorem.Paragraph(rnd.Next(1, 4));
            if (maxlength > s.Length) maxlength = s.Length - 1;
            return s.Substring(0, maxlength);

        }
        public static string NextLongText(this Random rnd)
        {
            return Faker.Lorem.Paragraph(rnd.Next(2, 99));
        }
        public static string NextLongText(this Random rnd, int maxlength)
        {
            var s = Faker.Lorem.Paragraph(rnd.Next(2, 99));
            if (maxlength > s.Length) maxlength = s.Length-1;
            return s.Substring(0, maxlength);
        }

        public static Byte NextTinyInt(this Random rnd)
        {
            var buffer = new byte[sizeof(Char)];
            rnd.NextBytes(buffer);
            return buffer[0];
        }
        public static string TestDataAsString(this Property property)
        {
            switch (property.Type)
            {

                case "bit":
                    return (((bool)TestData(property)) ? "true " : "false");
                case "date":
                case "datetime":
                case "datetime2":
                case "datetimeoffset":
                case "smalldatetime":
                    return TestData(property).ToString();

                case "bigint":
                case "decimal":
                case "float":
                case "int":
                case "money":
                case "numeric":
                case "smallint":
                case "smallmoney":
                case "tinyint":
                case "real":
                    return TestData(property).ToString();


                case "varbinary":
                    return "'<varbinary>'";

                case "char":
                case "nchar":
                case "ntext":
                case "nvarchar":
                case "text":
                case "varchar":
                case "varchar(max)":
                case "nvarchar(max)":
                    return "\"" + TestData(property).ToString().Replace("\"", "\"\"") + "\"";

                case "xml":
                    return "'<xml>'";
                case "image":
                    return "'<image>'";
                case "rowversion":
                    return "'<rowversion>'";
                case "sql_variant":
                    return "'<sql_variant>'";
                case "time":
                    return "'<time>'";
                case "timestamp":
                    return "'<timestamp>'";
                case "uniqueidentifier":
                    return TestData(property).ToString();
                    ;
            }
            return null;
        }
        public static bool IsString(this Property property)
        {
            switch (property.Type)
            {

                case "bit":
                    return false;
                case "date":
                case "datetime":
                case "datetime2":
                case "datetimeoffset":
                case "smalldatetime":
                    return false;

                case "bigint":
                case "decimal":
                case "float":
                case "int":
                case "money":
                case "numeric":
                case "smallint":
                case "smallmoney":
                case "tinyint":
                case "real":
                    return false;


                case "varbinary":
                    return false;

                case "char":
                case "nchar":
                case "ntext":
                case "nvarchar":
                case "text":
                case "varchar":
                case "varchar(max)":
                case "nvarchar(max)":
                    return true;

                case "xml":
                    return false;
                case "image":
                    return false;
                case "rowversion":
                    return false;
                case "sql_variant":
                    return false;
                case "time":
                    return false;
                case "timestamp":
                    return false;
                case "uniqueidentifier":
                    return false;
                    ;
            }
            return false;
        }
        public static bool IsNumber(this Property property)
        {
            switch (property.Type)
            {

                case "bit":
                    return false;
                case "date":
                case "datetime":
                case "datetime2":
                case "datetimeoffset":
                case "smalldatetime":
                    return false;

                case "bigint":
                case "decimal":
                case "float":
                case "int":
                case "money":
                case "numeric":
                case "smallint":
                case "smallmoney":
                case "tinyint":
                case "real":
                    return true;


                case "varbinary":
                    return false;

                case "char":
                case "nchar":
                case "ntext":
                case "nvarchar":
                case "text":
                case "varchar":
                case "varchar(max)":
                case "nvarchar(max)":
                    return false;

                case "xml":
                    return false;
                case "image":
                    return false;
                case "rowversion":
                    return false;
                case "sql_variant":
                    return false;
                case "time":
                    return false;
                case "timestamp":
                    return false;
                case "uniqueidentifier":
                    return false;
                    ;
            }
            return false;
        }

        public static string AsSSISDataType(this Property property)
        {
            switch (property.Type)
            {

                case "bit":
                    return "DT_BOOL";
                case "date":
                    return "DT_DBDATE";
                case "datetime":
                    return "DT_DBTIMESTAMP";
                case "datetime2":
                    return "DT_DBTIMESTAMP2,7";
                case "datetimeoffset":
                    return "";
                case "smalldatetime":
                    return "DT_DBTIMESTAMP";

                case "bigint":
                    return "DT_I8";
                case "decimal":
                    return "DT_DECIMAL," + property.Precision.ToString();
                case "float":
                    return "DT_R8";
                case "int":
                    return "DT_I4";
                case "money":
                    return "DT_CY";
                case "numeric":
                    return "DT_NUMERIC," + property.Precision.ToString() + "," + property.Scale.ToString() + "";
                case "smallint":
                    return "DT_I2";
                case "smallmoney":
                    return "DT_CY";
                case "tinyint":
                    return "DT_I1";
                case "real":
                    return "DT_R4";

                case "varbinary":
                    return "";

                case "char":
                    return "DT_STR," + property.MaxLength.ToString() + ",1252";
                case "nchar":
                    return "DT_WSTR," + property.MaxLength.ToString() + "";
                case "ntext":
                    return "DT_WSTR," + property.MaxLength.ToString() + "";
                case "nvarchar":
                    return "DT_WSTR," + property.MaxLength.ToString() + "";
                case "text":
                    return "DT_STR," + property.MaxLength.ToString() + ",1252";
                case "varchar":
                    return "DT_STR," + property.MaxLength.ToString() + ",1252";
                case "varchar(max)":
                    return "DT_STR," + property.MaxLength.ToString() + ",1252";
                case "nvarchar(max)":
                    return "DT_WSTR," + property.MaxLength.ToString() + "";

                case "xml":
                    return "DT_STR," + property.MaxLength.ToString() + ", 1252";
                case "image":
                    return "DT_IMAGE";
                case "rowversion":
                    return "";
                case "sql_variant":
                    return "";
                case "time":
                    return "DT_DBTIME";
                case "timestamp":
                    return "";
                case "uniqueidentifier":
                    return "DT_GUID";
                    ;
            }
            return "";
        }
        public static object TestData(this Property property)
        {
            switch (property.Type)
            {
                case "bigint":
                    return new Random(Guid.NewGuid().GetHashCode()).NextInt64();
                case "binary":
                    return "<binary>";
                case "bit":
                    return new Random(Guid.NewGuid().GetHashCode()).NextBool();
                case "char":
                    return new Random(Guid.NewGuid().GetHashCode()).NextChar();
                case "date":
                    return new Random(Guid.NewGuid().GetHashCode()).NextDateTime();
                case "datetime":
                    return new Random(Guid.NewGuid().GetHashCode()).NextDateTime();
                case "datetime2":
                    return new Random(Guid.NewGuid().GetHashCode()).NextDateTime();
                case "datetimeoffset":
                    return new Random(Guid.NewGuid().GetHashCode()).NextDateTime();
                case "decimal":
                    return new Random(Guid.NewGuid().GetHashCode()).NextDecimal();
                case "varbinary":
                    return new Random(Guid.NewGuid().GetHashCode()).NextLongText();
                case "float":
                    return new Random(Guid.NewGuid().GetHashCode()).NextDouble();
                case "image":
                    return new Random(Guid.NewGuid().GetHashCode()).NextLongText();
                case "int":
                    return new Random(Guid.NewGuid().GetHashCode()).Next();
                case "money":
                    return new Random(Guid.NewGuid().GetHashCode()).Next();
                case "nchar":
                    return new Random(Guid.NewGuid().GetHashCode()).NextChar();
                case "ntext":
                    return new Random(Guid.NewGuid().GetHashCode()).Next();
                case "numeric":
                    return new Random(Guid.NewGuid().GetHashCode()).Next();
                case "nvarchar":
                    return new Random(Guid.NewGuid().GetHashCode()).NextLongText(property.MaxLength);
                case "real":
                    return new Random(Guid.NewGuid().GetHashCode()).NextDouble();
                case "rowversion":
                    return new Random(Guid.NewGuid().GetHashCode()).NextDouble();
                case "smalldatetime":
                    return new Random(Guid.NewGuid().GetHashCode()).NextDateTime();
                case "smallint":
                    return new Random(Guid.NewGuid().GetHashCode()).NextInt16();
                case "smallmoney":
                    return new Random(Guid.NewGuid().GetHashCode()).NextInt16();
                case "sql_variant":
                    return new Random(Guid.NewGuid().GetHashCode()).Next();
                case "text":
                    return new Random(Guid.NewGuid().GetHashCode()).NextLongText(property.MaxLength);
                case "time":
                    return "";
                case "timestamp":
                    return "";
                case "tinyint":
                    return new Random(Guid.NewGuid().GetHashCode()).NextTinyInt();
                case "uniqueidentifier":
                    return Guid.NewGuid();
                case "varchar":
                    return new Random(Guid.NewGuid().GetHashCode()).NextLongText(property.MaxLength);
                case "varchar(max)":
                    return new Random(Guid.NewGuid().GetHashCode()).NextLongText(property.MaxLength);
                case "nvarchar(max)":
                    return new Random(Guid.NewGuid().GetHashCode()).NextLongText(property.MaxLength);
                case "xml":
                    return ""
                ;
            }
            return null;
        }
    }
}
