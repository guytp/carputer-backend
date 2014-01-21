using System;

namespace CarMediaServer
{
    /// <summary>
    /// This attribute provides a map between a single property on a data object and relevent database information.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DataPropertyMappingAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the column for this property in the database.
        /// </summary>
        public string DatabaseColumn { get; set; }

        /// <summary>
        /// Create a new instance of the attribute.
        /// </summary>
        /// <param name="databaseColumn">
        /// The name of the column for this property in the database.
        /// </param>
        public DataPropertyMappingAttribute(string databaseColumn)
        {
            DatabaseColumn = databaseColumn;
        }

        /// <summary>
        /// Create a new instance of the attribute.
        /// </summary>
        public DataPropertyMappingAttribute()
        {
        }
    }
}