using System;
using System.Collections.Generic;

namespace CarMediaServer
{
    /// <summary>
    /// This class allows caching of data objects that can be used throughout the application.
    /// </summary>
    internal class DatabaseCache<TDbObject, TPrimaryKey> where TDbObject : DbObject
    {
        /// <summary>
        /// Defines the cache for all objects that have been read.
        /// </summary>
        private List<TDbObject> _cachedObjects = null;

        /// <summary>
        /// Defines whether or not the cache is full.  A full cache is one that's considered to have been filled
        /// from the results of a Factory.ReadAll() method.
        /// </summary>
        private bool _isCacheFull = false;

        /// <summary>
        /// Defines a dictionary keying cached objects by their primary key.
        /// </summary>
        private Dictionary<TPrimaryKey, TDbObject> _cacheByPk = new Dictionary<TPrimaryKey, TDbObject>();

        /// <summary>
        /// Defines an object used for locking purposes.
        /// </summary>
        private object _lockObject = new object();

        /// <summary>
        /// Defines the metadata for this object.
        /// </summary>
        private DataObjectInformation _objectInformation;

        /// <summary>
        /// Defines the type of object cached which is used in log messages.
        /// </summary>
        private string _type;

        /// <summary>
        /// Create a new instance of this class.
        /// </summary>
        /// <param name="information">
        /// The metadata object for this class.
        /// </param>
        internal DatabaseCache(DataObjectInformation information)
        {
            if (information == null)
                throw new ArgumentNullException("information");
            _objectInformation = information;
            _type = typeof(TDbObject).Name;
        }

        /// <summary>
        /// Returns the cache for all objects that were recorded last time ReadAll() was performed.
        /// </summary>
        /// <returns>
        /// All the objects that have been cached in the database.
        /// </returns>
        internal TDbObject[] ReadAll()
        {
            if (!_isCacheFull)
                return null;
            lock (_lockObject)
            {
                TDbObject[] ret = _cachedObjects == null || _cachedObjects.Count < 1 ? null : _cachedObjects.ToArray();
                if (ret == null)
                    Logger.Debug("Cache is empty for " + _type);
                else
                    Logger.Debug("Returning cache with " + ret.Length + " " + _type + " objects");
                return ret;
            }
        }

        /// <summary>
        /// Returns a single object searched from the cache by its primary key.
        /// </summary>
        /// <param name="primaryKey">
        /// The primary key to search on.
        /// </param>
        /// <returns>
        /// A single database object or null if one is not found in the cache.
        /// </returns>
        internal TDbObject Read(TPrimaryKey primaryKey)
        {
            lock (_lockObject)
            {
                if (_cacheByPk.ContainsKey(primaryKey))
                {
                    Logger.Debug("Returning " + _type + " from cache using PK " + primaryKey);
                    return _cacheByPk[primaryKey];
                }
            }
            return default(TDbObject);
        }

        /// <summary>
        /// Store an object in the cache.  If an object with the same primary key already exists then it is updated.
        /// </summary>
        /// <param name="obj">
        /// The object to store in the cache.
        /// </param>
        internal void CacheObject(TDbObject obj)
        {
            CacheObject(new TDbObject[] { obj }, false);
        }

        /// <summary>
        /// Store objects in the cache.  If an objects with the same primary key already exists then they are updated.
        /// </summary>
        /// <param name="objects">
        /// The objects to store in the cache.
        /// </param>
        /// <param name="isReadAll">
        /// Defines whether or not this call was due to ReadAll on a factory.  If it is then it marks
        /// the local cache as being complete and empties it out first.
        /// </param>
        internal void CacheObject(TDbObject[] objects, bool isReadAll)
        {
            // Return if no objects
            if ((objects == null) || (objects.Length < 1))
                return;

            lock (_lockObject)
            {
                if (isReadAll)
                {
                    _isCacheFull = true;
                    if ((_cachedObjects != null) && (_cachedObjects.Count > 0))
                    {
                        Logger.Debug("The cache for " + _type + " is being cleared as a ReadAll() population is occuring");
                        _cachedObjects = null;
                    }
                }

                // If the list of objects is empty (i.e. recently invalidated) then just store and return to save time
                if ((_cachedObjects == null) || (_cachedObjects.Count < 1))
                {
                    Logger.Debug("Caching " + objects.Length + " objects of " + _type + " in to fresh cache"); 
                    if (_cachedObjects == null)
                        _cachedObjects = new List<TDbObject>(objects);
                    else
                        _cachedObjects.AddRange(objects);
                    foreach (TDbObject obj in objects)
						if (!_cacheByPk.ContainsKey(GetPrimaryKeyValue(obj)))
	                        _cacheByPk.Add(GetPrimaryKeyValue(obj), obj);
						else
							_cacheByPk[GetPrimaryKeyValue(obj)] = obj;
                    return;
                }

                Logger.Debug("Caching " + objects.Length + " objects of " + _type + " in to existing cache using merge/add"); 
                foreach (TDbObject obj in objects)
                {
                    TPrimaryKey primaryKey = GetPrimaryKeyValue(obj);
                    if (_cacheByPk.ContainsKey(primaryKey))
                    {
                        _cachedObjects.Remove(_cacheByPk[primaryKey]);
                        _cachedObjects.Add(obj);
                        _cacheByPk[primaryKey] = obj;
                    }
                    else
                    {
                        _cacheByPk.Add(primaryKey, obj);
                        _cachedObjects.Add(obj);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the primary key for the specified object cast in to its correct form.  This is read using the object
        /// metadata.
        /// </summary>
        /// <param name="obj">
        /// The object to read the value from.
        /// </param>
        /// <returns></returns>
        private TPrimaryKey GetPrimaryKeyValue(TDbObject obj)
        {
            return (TPrimaryKey)_objectInformation.PrimaryKey.GetValue(obj, null);
        }

        /// <summary>
        /// Invalidates the entire cache.
        /// </summary>
        internal void InvalidateCache()
        {
            lock (_lockObject)
            {
                Logger.Debug("Invalidating entire " + _type + " cache");
                _cacheByPk.Clear();
                _cachedObjects = null;
                _isCacheFull = false;
            }
        }

        /// <summary>
        /// Invalidates a single object in the cache.
        /// </summary>
        /// <param name="obj">
        /// The object to remove from the cache.
        /// </param>
        internal void InvalidateCache(TDbObject obj)
        {
            InvalidateCache(GetPrimaryKeyValue(obj));
        }

        /// <summary>
        /// Invalidates a single object in the cache.
        /// </summary>
        /// <param name="primaryKey">
        /// The primary key of the object to remove from the cache.
        /// </param>
        internal void InvalidateCache(TPrimaryKey primaryKey)
        {
            lock (_lockObject)
            {
                Logger.Debug("Invalidating " + _type + " from cache using PK " + primaryKey); 
                if (!_cacheByPk.ContainsKey(primaryKey))
                    return;
                _cachedObjects.Remove(_cacheByPk[primaryKey]);
                _cacheByPk.Remove(primaryKey);
            }
        }
    }
}