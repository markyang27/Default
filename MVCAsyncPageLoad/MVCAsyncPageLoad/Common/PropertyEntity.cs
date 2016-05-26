using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MVCAsyncPageLoad.Common
{
    internal class PropertyEntity
    {
        string _name;
        PropertyInfo _pi;

        public PropertyEntity(PropertyInfo pi)
        {
            _pi = pi;
            _name = _pi.Name;
        }

        public string Name 
        { 
            get { return _name; } 
        }

        public object GetValue(object instance)
        {
            object value = null;

            if (instance != null)
            {
                value = _pi.GetValue(instance, null);
            }

            return value;
        }
    }
}
