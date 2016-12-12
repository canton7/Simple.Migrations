using System;
using System.Collections.Generic;
using System.Reflection;

namespace SimpleMigrations.Platform
{
    internal static class TypeHelpers
    {
#if NET40
        public static Type GetTypeInfo(this Type type)
        {
            return type;
        }

        public static Type AsType(this Type type)
        {
            return type;
        }

        public static T GetCustomAttribute<T>(this Type type) where T : Attribute
        {
            return (T)Attribute.GetCustomAttribute(type, typeof(T));
        }

        public static IEnumerable<Type> GetDefinedTypes(this Assembly assembly)
        {
            return assembly.GetTypes();
        }
#else
        public static IEnumerable<TypeInfo> GetDefinedTypes(this Assembly assembly)
        {
            return assembly.DefinedTypes;
        }
#endif
    }
}
