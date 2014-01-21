using System;
using System.Collections.Generic;
using System.Reflection;

namespace CarMediaServer
{
    /// <summary>
    /// This helper class is used to store information about a data object to allow more rapid manipulation by the
    /// factory classes.
    /// </summary>
    internal class DataObjectInformation
    {
        /// <summary>
        /// Gets a list of properties on this object that map to database columns.
        /// </summary>
        public IEnumerable<PropertyInfo> Properties
        {
            get
            {
                return DatabaseColumnMap.Keys;
            }
        }

        /// <summary>
        /// Gets a dictionary that ties a property to its matching database column.
        /// </summary>
        public Dictionary<PropertyInfo, string> DatabaseColumnMap { get; private set; }

        /// <summary>
        /// Gets or sets the property defining the primary key for this object.
        /// </summary>
        public PropertyInfo PrimaryKey { get; set; }

        /// <summary>
        /// Gets or sets the name of the table tied to this data object.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the default order by clause used for this object.
        /// </summary>
        public string DefaultOrderBy { get; set; }

        /// <summary>
        /// Create a new instance of this class.
        /// </summary>
        public DataObjectInformation()
        {
            DatabaseColumnMap = new Dictionary<PropertyInfo, string>();
        }
    }
}