using System;
using System.Reflection;

namespace EventHorizon.Ads
{
    internal static class AdsSdkReflection
    {
        /// <summary>
        /// Finds type.
        /// </summary>
        public static Type FindType(params string[] fullNames)
        {
            for (int i = 0; i < fullNames.Length; i++)
            {
                Type type = Type.GetType(fullNames[i], false);
                if (type != null)
                {
                    return type;
                }
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                for (int j = 0; j < fullNames.Length; j++)
                {
                    Type type = assemblies[i].GetType(fullNames[j], false);
                    if (type != null)
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Executes create instance.
        /// </summary>
        public static object CreateInstance(Type type)
        {
            return type != null ? Activator.CreateInstance(type) : null;
        }

        /// <summary>
        /// Executes invoke static.
        /// </summary>
        public static object InvokeStatic(Type type, string methodName, params object[] args)
        {
            MethodInfo method = FindMethod(type, methodName, args?.Length ?? 0, true);
            return method != null ? method.Invoke(null, args) : null;
        }

        /// <summary>
        /// Executes invoke.
        /// </summary>
        public static object Invoke(object instance, string methodName, params object[] args)
        {
            MethodInfo method = FindMethod(instance?.GetType(), methodName, args?.Length ?? 0, false);
            return method != null ? method.Invoke(instance, args) : null;
        }

        /// <summary>
        /// Executes add event handler.
        /// </summary>
        public static void AddEventHandler(object eventSource, string eventName, object target, string methodName)
        {
            ChangeEventHandler(eventSource, eventName, target, methodName, true);
        }

        /// <summary>
        /// Executes remove event handler.
        /// </summary>
        public static void RemoveEventHandler(object eventSource, string eventName, object target, string methodName)
        {
            ChangeEventHandler(eventSource, eventName, target, methodName, false);
        }

        /// <summary>
        /// Executes read string.
        /// </summary>
        public static string ReadString(object instance, string memberName, string fallback = "")
        {
            object value = ReadMember(instance, memberName);
            return value?.ToString() ?? fallback;
        }

        /// <summary>
        /// Executes read int.
        /// </summary>
        public static int ReadInt(object instance, string memberName, int fallback = 0)
        {
            object value = ReadMember(instance, memberName);
            if (value == null)
            {
                return fallback;
            }

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return fallback;
            }
        }

        /// <summary>
        /// Executes read bool.
        /// </summary>
        public static bool ReadBool(object instance, string memberName, bool fallback = false)
        {
            object value = ReadMember(instance, memberName);
            if (value == null)
            {
                return fallback;
            }

            try
            {
                return Convert.ToBoolean(value);
            }
            catch
            {
                return fallback;
            }
        }

        /// <summary>
        /// Executes read member.
        /// </summary>
        private static object ReadMember(object instance, string memberName)
        {
            if (instance == null || string.IsNullOrWhiteSpace(memberName))
            {
                return null;
            }

            Type type = instance.GetType();
            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            if (property != null)
            {
                return property.GetValue(instance);
            }

            FieldInfo field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            return field?.GetValue(instance);
        }

        /// <summary>
        /// Finds method.
        /// </summary>
        private static MethodInfo FindMethod(Type type, string methodName, int parameterCount, bool isStatic)
        {
            if (type == null)
            {
                return null;
            }

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            MethodInfo[] methods = type.GetMethods(flags);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == methodName && methods[i].GetParameters().Length == parameterCount)
                {
                    return methods[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Executes change event handler.
        /// </summary>
        private static void ChangeEventHandler(object eventSource, string eventName, object target, string methodName, bool add)
        {
            if (eventSource == null || target == null || string.IsNullOrWhiteSpace(eventName) || string.IsNullOrWhiteSpace(methodName))
            {
                return;
            }

            Type sourceType = eventSource as Type ?? eventSource.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
            EventInfo eventInfo = sourceType.GetEvent(eventName, flags);
            if (eventInfo == null)
            {
                return;
            }

            MethodInfo handlerMethod = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (handlerMethod == null)
            {
                return;
            }

            Delegate handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, target, handlerMethod, false);
            if (handler == null)
            {
                return;
            }

            object instance = eventSource is Type ? null : eventSource;
            if (add)
            {
                eventInfo.AddEventHandler(instance, handler);
            }
            else
            {
                eventInfo.RemoveEventHandler(instance, handler);
            }
        }
    }
}
