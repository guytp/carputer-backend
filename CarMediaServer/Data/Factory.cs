using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using MySql.Data.MySqlClient;

namespace CarMediaServer
{
    /// <summary>
    /// This abstract class provides a base from which all other factories may descend and provides common data
    /// access capabilities.
    /// </summary>
    /// <typeparam name="TDbObject">
    /// The type of database object to provide access for.
    /// </typeparam>
    /// <typeparam name="TPrimaryKey">
    /// The type of primary key for the object.
    /// </typeparam>
    public abstract class Factory<TDbObject, TPrimaryKey> where TDbObject : DbObject
    {
        /// <summary>
        /// Defines a cache of information about all known data objects to assist in factory reflection.
        /// </summary>
        private static Dictionary<Type, DataObjectInformation> _dataObjectInformationMap = new Dictionary<Type, DataObjectInformation>();

        /// <summary>
        /// Defines the Type object referencing the DBObject.
        /// </summary>
        private readonly Type _objectType = typeof(TDbObject);

        /// <summary>
        /// Defines the type of DBObject as a string for logging.
        /// </summary>
        private string _type;

        /// <summary>
        /// Defines the object type information for TDbObject.
        /// </summary>
        private readonly DataObjectInformation _dataObjectInformation;

        /// <summary>
        /// Defines a mapping between data caches and the data object type.
        /// </summary>
        private static readonly Dictionary<Type, object> _caches = new Dictionary<Type, object>();

        /// <summary>
        /// Defines the cache for the current data object.
        /// </summary>
        internal DatabaseCache<TDbObject, TPrimaryKey> _cache;

        /// <summary>
        /// Static constructor for database objects which enumerates all loaded assemblies to setup the data environment.
        /// </summary>
        static Factory()
		{
			// Inspect the type of DB object and construct a column list, a column mapping for
			// all DbObject types that are loaded.
			Type type = typeof(DbObject);

			List<Type> types = new List<Type>();
			foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					Type[] assemblyTypes = ass.GetTypes();
					foreach (Type assemblyType in assemblyTypes)
					{
						try
						{
							if ((type.IsAssignableFrom(assemblyType)) && (!assemblyType.IsAbstract))
							     types.Add(assemblyType);
						}
					    catch
					    {
						}
					}
				}
				catch
				{
				}

			}
            foreach (Type t in types)
            {
                object[] classAttributes = t.GetCustomAttributes(typeof(DataObjectAttribute), true);
                if ((classAttributes == null) || (classAttributes.Length < 1))
                    continue;
                DataObjectAttribute dataObjectAttribute = (DataObjectAttribute)classAttributes[0];
                DataObjectInformation objectInformation = new DataObjectInformation()
                {
                    TableName = dataObjectAttribute.TableName,
                    DefaultOrderBy = dataObjectAttribute.DefaultOrderBy
                };
                PropertyInfo[] properties = t.GetProperties();
                if (properties == null)
                    continue;
                foreach (PropertyInfo property in properties)
                {
                    object[] attributes = property.GetCustomAttributes(typeof(DataPropertyMappingAttribute), true);
                    if ((attributes == null) || (attributes.Length < 1))
                        continue;
                    DataPropertyMappingAttribute propertyMapping = (DataPropertyMappingAttribute)attributes[0];
                    if (string.IsNullOrEmpty(propertyMapping.DatabaseColumn))
                        propertyMapping.DatabaseColumn = property.Name;
                    if (property.Name == dataObjectAttribute.PrimaryKey)
                        objectInformation.PrimaryKey = property;
                    objectInformation.DatabaseColumnMap.Add(property, propertyMapping.DatabaseColumn);
                }
                if ((objectInformation.Properties.Count() < 0) || (string.IsNullOrEmpty(objectInformation.TableName)) || (objectInformation.PrimaryKey == null))
                    continue;
                _dataObjectInformationMap.Add(t, objectInformation);
            }
        }

        /// <summary>
        /// Create a new instance of the factory.
        /// </summary>
        public Factory()
        {
            if (!_dataObjectInformationMap.ContainsKey(_objectType))
                throw new Exception("No mapping information for " + _objectType.FullName);
            _dataObjectInformation = _dataObjectInformationMap[_objectType];
            if (_dataObjectInformation.PrimaryKey.PropertyType != typeof(TPrimaryKey))
                throw new Exception("Data object primary key type does not match factory primary key type");
            if (_caches.ContainsKey(_objectType))
                _cache = (DatabaseCache<TDbObject, TPrimaryKey>)_caches[_objectType];
            else
            {
                lock (_caches)
                {
                    _cache = new DatabaseCache<TDbObject, TPrimaryKey>(_dataObjectInformation);
                    _caches.Add(_objectType, _cache);
                }
            }
            _type = GetType().Name;
        }

        /// <summary>
        /// Read all objects from the database.
        /// </summary>
        /// <returns>
        /// An array containing all objects in the database.
        /// </returns>
        public TDbObject[] ReadAll()
        {
            // Return from cache if possible
            Logger.Debug(_type + " checking cache");
            TDbObject[] cache = _cache.ReadAll();
            if ((cache != null) && (cache.Length > 0))
            {
                Logger.Debug(_type + " using cache data");
                return cache;
            }

            // Declarations
            List<TDbObject> objects = new List<TDbObject>();
            MySqlConnection conn = null;

            try
            {
                // Open the connection
                Logger.Debug(_type + " not found in cache, querying database.  Creating connection.");
                conn = new MySqlConnection(Configuration.ConnectionString);
                conn.Open();

                // Create the command and execute it to insert data
                Logger.Debug(_type + " generating SQL statement for query");
                MySqlCommand command = new MySqlCommand(GenerateSelectStatement(), conn);
                Logger.Debug(_type + " executing query");
                MySqlDataReader reader = command.ExecuteReader();
                Logger.Debug(_type + " processing rows");
                while (reader.Read())
                {
                    TDbObject obj = (TDbObject)Activator.CreateInstance(_objectType);
                    int i = 0;
                    foreach (PropertyInfo property in _dataObjectInformation.Properties)
                    {
                        object value = reader.GetValue(i);
                        if (value == DBNull.Value)
                            value = null;
                        if (property.PropertyType == typeof(bool))
                            value = Convert.ToBoolean(value);
                        property.SetValue(obj, value, null);
                        i++;
                    }
                    objects.Add(obj);
                }
                Logger.Debug(_type + " rows all processed from data reader");
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                }
            }

            // Return the results and cache
            Logger.Debug(_type + " storing objects in cache for future usage");
            _cache.InvalidateCache();
            TDbObject[] objectArray = objects.ToArray();
            _cache.CacheObject(objectArray, true);
            Logger.Debug(_type + " objects cached, query complete");
            return objectArray;
        }

        /// <summary>
        /// Generate a SQL statement based upon the properties of the class.
        /// </summary>
        /// <returns>
        /// A complete SELECT statement to read objects from the database.
        /// </returns>
        private string GenerateSelectStatement()
        {
            string sql = string.Empty;
            foreach (PropertyInfo property in _dataObjectInformation.Properties)
                sql += (sql != string.Empty ? ", " : string.Empty) + _dataObjectInformation.DatabaseColumnMap[property];
            sql += " FROM " + _dataObjectInformation.TableName;
            if (!string.IsNullOrEmpty(_dataObjectInformation.DefaultOrderBy))
                sql += " ORDER BY " + _dataObjectInformation.DefaultOrderBy;
            sql = "SELECT " + sql;
            Logger.Debug(_type + " generated SQL: " + sql);
            return sql;
        }

        /// <summary>
        /// Deletes an object from the database.
        /// </summary>
        /// <param name="primaryKey">
        /// The primary key of the object to delete.
        /// </param>
        public void Delete(TPrimaryKey primaryKey)
        {
            MySqlConnection conn = null;
            try
            {
                // Open the connection
                Logger.Debug(_type + " deleting from database for PK " + primaryKey);
                conn = new MySqlConnection(Configuration.ConnectionString);
                conn.Open();

                // Create the command and execute it to insert data
                MySqlCommand command = new MySqlCommand("DELETE FROM " + _dataObjectInformation.TableName + " WHERE " + _dataObjectInformation.DatabaseColumnMap[_dataObjectInformation.PrimaryKey] + "=@primaryKey", conn);
                command.Parameters.AddWithValue("@primaryKey", primaryKey);
                Logger.Debug(_type + " executing delete");
                command.ExecuteNonQuery();
                Logger.Debug(_type + " deleted from database, removing from cache");

                // Invalidate the cache
                _cache.InvalidateCache(primaryKey);
                Logger.Debug(_type + " finished deletion of " + primaryKey);
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// Add a new object to the database and set its primary key.
        /// </summary>
        /// <param name="obj">
        /// The object to add to the database.
        /// </param>
        public void Create(TDbObject obj)
        {
            MySqlConnection conn = null;
            try
            {
                // Open the connection
                Logger.Debug(_type + " creating new object ");
                conn = new MySqlConnection(Configuration.ConnectionString);
                conn.Open();

                // Creation the insertion SQL and corresponding properties
                MySqlCommand command = new MySqlCommand();
                string allColumns = string.Empty;
                string allValues = string.Empty;
                Logger.Debug(_type + " generating insertion SQL");
                foreach (PropertyInfo property in _dataObjectInformation.Properties)
                {
                    if (allColumns != string.Empty)
                    {
                        allColumns += ", ";
                        allValues += ", ";
                    }
                    string column = _dataObjectInformation.DatabaseColumnMap[property];
                    allColumns += column;
                    allValues += "@" + column;
                    command.Parameters.AddWithValue("@" + column, property.GetValue(obj, null));
                }
                command.CommandText = "INSERT INTO " + _dataObjectInformation.TableName + " (" + allColumns + ") VALUES(" + allValues + ")";
                command.CommandType = System.Data.CommandType.Text;
                command.Connection = conn;
                Logger.Debug(_type + " generated SQL: " + command.CommandText);

                // Execute the command
                Logger.Debug(_type + " executing command");
                command.ExecuteNonQuery();

                // Read the newly inserted ID
                Logger.Debug(_type + " retrieving primary key");
                command = new MySqlCommand("SELECT LAST_INSERT_ID()", conn);
                object newPk = command.ExecuteScalar();
                Logger.Debug(_type + " new primary key is " + newPk);
                _dataObjectInformation.PrimaryKey.SetValue(obj, Convert.ChangeType(newPk, _dataObjectInformation.PrimaryKey.PropertyType), null);

                // Add to cache
                Logger.Debug(_type + " caching object");
                _cache.CacheObject(obj);
                Logger.Debug(_type + " completed insertion");
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// Update an existing object in the database.
        /// </summary>
        /// <param name="obj">
        /// The object to update in the database.
        /// </param>
        public void Update(TDbObject obj, bool useCache = true)
        {
            MySqlConnection conn = null;
            try
            {
                // Open the connection
                Logger.Debug(_type + " updating existing object ");
                conn = new MySqlConnection(Configuration.ConnectionString);
                conn.Open();

                // Creation the insertion SQL and corresponding properties
                MySqlCommand command = new MySqlCommand();
                string setSql = string.Empty;
                Logger.Debug(_type + " generating update SQL");
                foreach (PropertyInfo property in _dataObjectInformation.Properties)
                {
                    if (property == _dataObjectInformation.PrimaryKey)
                        continue;
                    if (setSql != string.Empty)
                        setSql += ", ";
                    string column = _dataObjectInformation.DatabaseColumnMap[property];
                    setSql += column + " = @" + column;
                    command.Parameters.AddWithValue("@" + column, property.GetValue(obj, null));
                }
                command.CommandText = "UPDATE " + _dataObjectInformation.TableName + " SET " + setSql + " WHERE " + _dataObjectInformation.DatabaseColumnMap[_dataObjectInformation.PrimaryKey] + "=@primaryKey";
                command.Parameters.AddWithValue("@primaryKey", _dataObjectInformation.PrimaryKey.GetValue(obj, null));
                command.CommandType = System.Data.CommandType.Text;
                command.Connection = conn;
                Logger.Debug(_type + " generated SQL: " + command.CommandText);

                // Execute the command
                Logger.Debug(_type + " executing command");
                command.ExecuteNonQuery();

                // Update cache
				if (useCache)
				{
    	            Logger.Debug(_type + " re-caching object");
                	_cache.InvalidateCache(obj);
                	_cache.CacheObject(obj);
	                Logger.Debug(_type + " completed update");
				}
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                }
            }
        }

		/// <summary>
		/// Invalidates the cache for this data type..
		/// </summary>
		protected void InvalidateCache()
		{
			_cache.InvalidateCache();
		}
    }
}