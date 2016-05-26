using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MVCAsyncPageLoad.Common
{
    internal static class PropertyHelper
    {
        static ConcurrentDictionary<Type, PropertyEntity[]> _cache = new ConcurrentDictionary<Type, PropertyEntity[]>();

        /// <summary>
        /// Creates and caches fast property helpers that expose getters for every public get property on the underlying type.
        /// </summary>
        public static PropertyEntity[] GetProperties(object instance)
        {
            PropertyEntity[] pes;

            Type type = instance.GetType();

            if (!_cache.TryGetValue(type, out pes))
            {
                // We avoid loading indexed properties using the where statement.
                // Indexed properties are not useful (or valid) for grabbing properties off an anonymous object.
                IEnumerable<PropertyInfo> properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                           .Where(prop => prop.GetIndexParameters().Length == 0 && prop.GetGetMethod() != null);

                List<PropertyEntity> list = new List<PropertyEntity>();

                foreach (PropertyInfo pi in properties)
                {
                    PropertyEntity pe = new PropertyEntity(pi);
                    list.Add(pe);
                }

                pes = list.ToArray();

                _cache.TryAdd(type, pes);
            }

            return pes;
        }
    }
}
