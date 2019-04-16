using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace UniJSON
{
    public static class FormatterExtensionsSerializer
    {
        public static void Serialize<T>(this IFormatter f, T arg)
        {
            GenericSerializer<T>.Serialize(f, arg);
        }
    }

    static class GenericSerializer<T>
    {
        delegate void Serializer(IFormatter f, T t);

        static Action<IFormatter, T> GetSerializer(Type t)
        {
            // primitive
            var mi = typeof(IFormatter).GetMethod("Value", new Type[] { t });
            if (mi != null)
            {
                return GenericInvokeCallFactory.OpenAction<IFormatter, T>(mi);
            }
            throw new NotImplementedException();
        }

        public static void Serialize(IFormatter f, T t)
        {
            var s_serializer = new Serializer(GetSerializer(typeof(T)));
            s_serializer(f, t);
        }
    }
}
