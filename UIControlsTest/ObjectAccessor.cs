using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UIControlsTest
{
    public class PrivateObject
    {
        private readonly object o;

        public PrivateObject(object o)
        {
            this.o = o;
        }

        public object GetValue(string fieldName)
        {
            var memberInfo = (MemberInfo)o.GetType().GetMember(fieldName).First();
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)memberInfo).GetValue(o);
                case MemberTypes.Property:
                    return ((PropertyInfo)memberInfo).GetValue(o);
                default:
                    return 0;
            }
        }

        public object Invoke(string methodName, params object[] args)
        {
            var methodInfo = o.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo == null)
            {
                throw new Exception($"Method'{methodName}' not found is class '{o.GetType()}'");
            }
            return methodInfo.Invoke(o, args);
        }
    }
}
