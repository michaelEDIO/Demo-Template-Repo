using System;
using System.Linq;

namespace EDIOptions.AppCenter.Database
{
    /// <summary>
    /// A class to hold database field information
    /// </summary>
    public class DBField
    {
        /// <summary>
        /// A class to hold database field information
        /// </summary>
        /// <param name="name">Name of field</param>
        /// <param name="type">Type of field</param>
        /// <param name="length">Length of the field (only required for numeric/string field types)</param>
        /// <param name="allowNULL">A flag to indicate if NULL is allowed in this field</param>
        /// <param name="defaultVal">The default value to give this field</param>
        public DBField(string name, FieldTypes type, string length = "0", bool allowNULL = true, string defaultVal = "NULL")
        {
            _Name = name;
            _Type = type;
            _Length = length;
            _AllowNull = allowNULL;
            _DefaultValue = defaultVal;
            _Position = 1;
        }

        /// <summary>
        /// A class to hold database field information
        /// </summary>
        /// <param name="name">Name of field</param>
        /// <param name="type">Type of field</param>
        /// <param name="allowNULL">Length of the field (only required for numeric/string field types)</param>
        /// <param name="defaultVal">The default value to give this field</param>
        /// <param name="position">The position of the field in the table</param>
        public DBField(string name, string type, bool allowNULL, string defaultVal, int position)
        {
            _Name = name.ToUpper();
            _AllowNull = allowNULL;
            _DefaultValue = defaultVal;
            _Position = position;

            string uType = type.ToUpper();
            FieldTypes[] types = Enum.GetValues(typeof(FieldTypes)).Cast<FieldTypes>().ToArray();
            foreach (FieldTypes f in types)
            {
                if (uType == f.ToString())
                {
                    _Type = f;
                    break;
                }
            }
        }

        private string _Name, _DefaultValue, _Length;
        private FieldTypes _Type;
        private bool _AllowNull;

        /// <summary>
        /// Gets this field's default value
        /// </summary>
        public string DefaultValue
        {
            get { return _DefaultValue; }
        }

        /// <summary>
        /// Gets whether this field allows NULL
        /// </summary>
        public bool AllowNull
        {
            get { return _AllowNull; }
        }

        /// <summary>
        /// Gets the length of the field
        /// </summary>
        public string Length
        {
            get { return _Length; }
            set { _Length = value; }
        }

        /// <summary>
        /// Gets the type of field
        /// </summary>
        public FieldTypes Type
        {
            get { return _Type; }
        }

        /// <summary>
        /// Gets the name of the field
        /// </summary>
        public string Name
        {
            get { return _Name; }
        }

        private int _Position;

        /// <summary>
        /// Gets the position of the field
        /// </summary>
        public int Position
        {
            get { return _Position; }
        }

        /// <summary>
        /// Gets the string required to add this field in a CREATE statement
        /// </summary>
        public string QueryString
        {
            get
            {
                string q = _Name + " " + _Type.ToString();
                switch (_Type)
                {
                    case FieldTypes.TINYINT:
                    case FieldTypes.SMALLINT:
                    case FieldTypes.MEDIUMINT:
                    case FieldTypes.INT:
                    case FieldTypes.BIGINT:
                    case FieldTypes.CHAR:
                    case FieldTypes.VARCHAR:
                    case FieldTypes.DOUBLE:
                    case FieldTypes.DECIMAL:
                    case FieldTypes.FLOAT:
                        {
                            q += " (" + Length + ")";
                            break;
                        };
                    default:
                        {
                            break;
                        }
                }
                //CHARSETS
                switch (_Type)
                {
                    case FieldTypes.CHAR:
                    case FieldTypes.VARCHAR:
                    case FieldTypes.TINYTEXT:
                    case FieldTypes.TEXT:
                    case FieldTypes.MEDIUMTEXT:
                    case FieldTypes.LONGTEXT:
                        {
                            q += " CHARACTER SET utf8";
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
                q += " " + (_AllowNull ? "NULL" : "NOT NULL");
                if (_DefaultValue == "NULL")
                {
                    if (_AllowNull)
                    {
                        q += " DEFAULT NULL";
                    }
                }
                else
                {
                    switch (_Type)
                    {
                        case FieldTypes.TINYINT:
                        case FieldTypes.SMALLINT:
                        case FieldTypes.MEDIUMINT:
                        case FieldTypes.INT:
                        case FieldTypes.BIGINT:
                        case FieldTypes.DOUBLE:
                        case FieldTypes.DECIMAL:
                        case FieldTypes.FLOAT:
                            {
                                q += " DEFAULT " + _DefaultValue;
                                break;
                            }
                        default:
                            {
                                q += " DEFAULT '" + _DefaultValue + "'";
                                break;
                            };
                    }
                }
                return q;
            }
        }

        /// <summary>
        /// Enumeration of field types
        /// </summary>
        public enum FieldTypes
        {
            TINYINT, SMALLINT, MEDIUMINT, INT, BIGINT,
            DOUBLE, DECIMAL, FLOAT,
            CHAR, VARCHAR,
            DATE, TIME, DATETIME, YEAR, TIMESTAMP,
            TINYBLOB, BLOB, MEDIUMBLOB, LONGBLOB,
            TINYTEXT, TEXT, MEDIUMTEXT, LONGTEXT,
            OTHER
        }
    }
}