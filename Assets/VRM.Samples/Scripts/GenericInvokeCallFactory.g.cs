
using System;
using System.Reflection;


namespace UniJSON
{
    public static partial class GenericInvokeCallFactory
    {
        public static Action<S, A0> OpenAction<S, A0>(MethodInfo m)
        {
            if (m.IsStatic)
            {
                throw new ArgumentException(string.Format("{0} is static", m));
            }

            return (Action<S, A0>)Delegate.CreateDelegate(typeof(Action<S, A0>), m);
        }
    }
}

