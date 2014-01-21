using System;
using System.Collections.Generic;
using System.Text;

namespace CarMediaServer
{
    /// <summary>
    /// This attribute defines core information about database objects.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DataObjectAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the table this object is contained in within the database.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the default order by clause used for this object.
        /// </summary>
        public string DefaultOrderBy { get; set; }

        /// <summary>
        /// Gets or sets the primary key for the object.
        /// </summary>
        public string PrimaryKey { get; set; }

        /// <summary>
        /// Create a new instance of the attribute.
        /// </summary>
        /// <param name="tableName">
        /// The name of the table this object is contained in within the database.
        /// </param>
        /// <param name="defaultOrderBy">
        /// The default order by clause used for this object.
        /// </param>
        /// <param name="primaryKey">
        /// The primary key for the object.
        /// </param>
        public DataObjectAttribute(string tableName, string defaultOrderBy, string primaryKey)
        {
            TableName = tableName;
            DefaultOrderBy = defaultOrderBy;
            PrimaryKey = primaryKey;
        }
    }
}