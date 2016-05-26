using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Routing;

namespace MVCAsyncPageLoad.Common
{
    internal static class TypeHelper
    {
        /// <summary>
        /// Given an object of anonymous type, add each property as a key and associated with its value to a dictionary.
        /// </summary>
        public static RouteValueDictionary ObjectToDictionary(object instance)
        {
            RouteValueDictionary dictionary = new RouteValueDictionary();

            if (instance != null)
            {
                foreach (PropertyEntity pe in PropertyHelper.GetProperties(instance))
                {
                    dictionary.Add(pe.Name, pe.GetValue(instance));
                }
            }

            return dictionary;
        }
    }
}
