using SettingsManagement.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


namespace SettingsManagement
{
    public static class SettingsUtility
    {

        #region Assembly Metadata

        static Dictionary<Assembly, Dictionary<string, string>> assemblyMetadatas;

        public static string GetAssemblyMetadata(Assembly assembly, string key)
        {
            if (!TryGetAssemblyMetadata(assembly, key, out var value))
                throw new Exception($"Not define AssemblyMetadataAttribute. key: {key}");
            return value;
        }

        public static string GetAssemblyMetadata(Assembly assembly, string key, string defaultValue)
        {
            if (!TryGetAssemblyMetadata(assembly, key, out var value))
            {
                value = defaultValue;
            }
            return value;
        }

        public static bool TryGetAssemblyMetadata(Assembly assembly, string key, out string value)
        {
            if (assemblyMetadatas == null)
            {
                assemblyMetadatas = new();

            }

            if (!assemblyMetadatas.TryGetValue(assembly, out var dic))
            {
                dic = new Dictionary<string, string>();
                foreach (var attr in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
                {
                    dic[attr.Key] = attr.Value;
                }
                assemblyMetadatas[assembly] = dic;
            }

            if (!dic.TryGetValue(key, out value))
            {
                value = null;
                return false;
            }
            return true;
        }

        #endregion

        public static string GetPackageName(Type type) => GetPackageName(type.Assembly);

        public static string GetPackageName(Assembly assembly)
        {
            string packageName;
            if (!TryGetAssemblyMetadata(assembly, "Package.Name", out packageName))
            {
                if (!TryGetAssemblyMetadata(assembly, "Unity.Package.Name", out packageName))
                {
                    return null;
                }
            }
            return packageName;
        }


        public static IEnumerable<Assembly> ReferencedAssemblies(Assembly referenced, IEnumerable<Assembly> assemblies)
        {
            string fullName = referenced.FullName;

            foreach (var ass in assemblies)
            {
                if (referenced == ass)
                {
                    yield return ass;
                }
                else
                {
                    foreach (var refAss in ass.GetReferencedAssemblies())
                    {
                        if (fullName == refAss.FullName)
                        {
                            yield return ass;
                            break;
                        }
                    }
                }
            }
        }

        public static IEnumerable<Assembly> ReferencedAssemblies(Assembly referenced)
        {
            return ReferencedAssemblies(referenced, AppDomain.CurrentDomain.GetAssemblies());
        }



        public static object GetDefualtValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }


        internal static IEnumerable<string> EnumerateVariants(string variant)
        {
            if (variant == null)
            {
                yield return null;
            }
            else
            {
                bool match = false;
                var variants = Settings.Variants;
                string _variant;
                for (int i = 0; i < variants.Count; i++)
                {
                    _variant = variants[i];
                    if (match)
                    {
                        yield return _variant;
                    }
                    else
                    {
                        if (_variant == variant)
                        {
                            match = true;
                            yield return _variant;
                        }
                    }
                }

                if (!match)
                {
                    yield return null;
                }
            }
        }

        internal static List<string> displayVariantNames;
        public static List<string> GetDisplayVariantNames()
        {
            if (displayVariantNames != null)
            {
                return displayVariantNames;
            }

            displayVariantNames = new List<string>();
            displayVariantNames.Add(GetDisplayVariantName(null));
            displayVariantNames.AddRange(SettingSettings.Variants.Select(o => o.variant).Select(o => GetDisplayVariantName(o)));
            displayVariantNames = (from o in displayVariantNames
                                   orderby string.Equals(o, SettingSettings.DefaultVariantName, StringComparison.InvariantCultureIgnoreCase) ? 0 :
                                   string.Equals(o, "debug", StringComparison.InvariantCultureIgnoreCase) ? 1 : 2,
                                   o
                                   select o).ToList();

            return displayVariantNames;
        }

        public static string GetDisplayVariantName(string variant)
        {
            if (variant == null)
                return SettingSettings.DefaultVariantName;
            return variant;
        }

        public static string DisplayToVariant(string display)
        {
            if (string.IsNullOrEmpty(display))
                return null;
            if (display == SettingSettings.DefaultVariantName)
                return null;
            return display;
        }

        public static void GetCombineValues<T>(Setting<List<T>> setting, string platform, string variant, List<T> outList)
        {
            foreach (var variant2 in Settings.GetVariants(variant).Reverse())
            {
                foreach (var platform2 in PlatformNames.BasePlatforms(platform, true))
                {
                    if (setting.DirectContains(platform2, variant2))
                    {
                        var value = setting.DirectGetValue(platform2, variant2);

                        if (value != null)
                        {
                            foreach (T v in (IEnumerable)value)
                            {
                                outList.Add(v);
                            }
                        }
                    }
                }
            }
        }


        public static bool GetCombineValues<T>(Setting<T> setting, string platform, string variant, out T result)
        {
            IList list;
            Type type = typeof(T);
            Type itemType = null;
            bool isList = false, isArray = false;
            if (type.IsArray)
            {
                itemType = type.GetElementType();
                isArray = true;
            }
            else if (typeof(IList).IsAssignableFrom(type))
            {
                itemType = type.GetGenericArguments()[0];
                isList = true;
            }
            else
            {
                itemType = type;
            }
            list = Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType)) as IList;

            foreach (var variant2 in Settings.GetVariants(variant).Reverse())
            {
                foreach (var platform2 in PlatformNames.BasePlatforms(platform, true))
                {
                    if (setting.DirectContains(platform2, variant2))
                    {
                        var value = setting.DirectGetValue(platform2, variant2);

                        if (isArray || isList)
                        {
                            if (value != null)
                            {
                                foreach (var v in (IEnumerable)value)
                                {
                                    list.Add(v);
                                }
                            }
                        }
                        else
                        {
                            list.Add(value);
                        }
                    }
                }
            }

            if (isArray)
            {
                var array = Array.CreateInstance(itemType, list.Count);
                list.CopyTo(array, 0);
                result = (T)(object)array;
            }
            else
            {
                result = (T)(object)list;
            }
            return list.Count > 0;
        }

        public static T Copy<T>(T value)
        {
            return ValueWrapper<T>.Copy(value);
        }


#if UNITY_EDITOR
        /*
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void AfterAssembliesLoaded()
        {
            //初始化环境变量
            if (SettingSettings.UserEnvironmentVariables != null)
            {
                foreach (var pair in SettingSettings.UserEnvironmentVariables)
                {
                    Environment.SetEnvironmentVariable(pair.Key, pair.Value);
                }
            }
        }
        */
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void AfterAssembliesLoaded()
        {
            //初始化环境变量
            if (SettingSettings.UserEnvironmentVariables != null)
            {
                foreach (var pair in SettingSettings.UserEnvironmentVariables)
                {
                    if (string.IsNullOrEmpty(pair.Key))
                        continue;
                    Environment.SetEnvironmentVariable(pair.Key, pair.Value);
                }
            }
        }

        /*
    private static Dictionary<string, string> environmentVariables;

    private static Dictionary<string, string> EnvironmentVariables
    {
        get
        {
            if (environmentVariables == null)
            {
                environmentVariables = new Dictionary<string, string>();
                //var user = SettingSettings.UserEnvironmentVariables;
                //if (user != null)
                //{
                //    foreach (var item in user)
                //    {
                //        environmentVariables[item.Key] = item.Value;
                //    }
                //}
            }
            return environmentVariables;
        }
    }

    public static string GetEnvironmentVariable(string variable)
    {
        string value;
        if (EnvironmentVariables.TryGetValue(variable, out value))
            return value;

        value = Environment.GetEnvironmentVariable(variable);
        return value;
    }

    public static void AddEnvironmentVariable(string name, string value)
    {
        EnvironmentVariables[name] = value;
    }
    */

        public static void FillEnvironmentVariables(object target, ConverterDelegate converter = null)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            Type type = target.GetType();
            var dic = Environment.GetEnvironmentVariables();
            /*foreach (var item in EnvironmentVariables)
            {
                dic[item.Key] = item.Value;
            }*/

            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.SetProperty;
            bindingFlags |= BindingFlags.Instance;

            _FillEnvironmentVariables(type, ref target, dic, converter, bindingFlags);
        }

        public static void FillEnvironmentVariables(Type type, ConverterDelegate converter = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            object target = null;
            var dic = Environment.GetEnvironmentVariables();

            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.SetProperty;
            bindingFlags |= BindingFlags.Static;

            _FillEnvironmentVariables(type, ref target, dic, converter, bindingFlags);
        }

        static bool _FillEnvironmentVariables(Type type, ref object target, IDictionary dic, ConverterDelegate converter, BindingFlags flags)
        {
            string argName;
            string strValue;
            object value;
            object oldValue;
            Type valueType = null;
            PropertyInfo pInfo;
            FieldInfo fInfo;
            bool hasValue;
            bool changed = false;

            foreach (var member in type.GetMembers(flags))
            {
                strValue = null;
                value = null;
                hasValue = false;

                valueType = null;
                fInfo = null;
                pInfo = null;
                try
                {
                    if (member.MemberType == MemberTypes.Property)
                    {
                        pInfo = (PropertyInfo)member;
                        valueType = pInfo.PropertyType;

                        value = pInfo.GetValue(target);
                    }
                    else if (member.MemberType == MemberTypes.Field)
                    {
                        fInfo = (FieldInfo)member;
                        valueType = fInfo.FieldType;

                        value = fInfo.GetValue(target);
                    }
                    else
                    {
                        continue;
                    }

                    oldValue = value;

                    foreach (var attr in member.GetCustomAttributes<EnvironmentVariableAttribute>())
                    {
                        argName = null;
                        strValue = null;

                        if (pInfo != null)
                        {
                            if (!pInfo.CanWrite)
                            {
                                Debug.LogWarning($"{pInfo.DeclaringType.Name}.{pInfo.Name} Can't Write");
                                break;
                            }
                        }
                        else if (fInfo.IsInitOnly)
                        {
                            Debug.LogWarning($"{fInfo.DeclaringType.Name}.{fInfo.Name} Can't Write");
                            break;
                        }


                        if (!string.IsNullOrEmpty(attr.Name))
                        {
                            argName = attr.Name;
                        }
                        else
                        {
                            argName = member.Name;
                        }

                        if (dic.Contains(argName))
                        {
                            value = dic[argName];
                            if (value is string str)
                            {
                                if (str != null && attr.Trim)
                                {
                                    str = str.Trim();
                                }
                                strValue = str;
                                value = strValue;
                            }
                            hasValue = true;
                            break;
                        }
                    }


                    if (hasValue)
                    {
                        if (value == null)
                        {
                            if (valueType.IsValueType)
                            {
                                value = Activator.CreateInstance(valueType);
                            }
                        }
                        else if (value.GetType() != valueType)
                        {
                            try
                            {
                                if (!(converter != null && converter(ref value, valueType)))
                                {
                                    value = ConvertValue(value, valueType);
                                }
                            }
                            catch (Exception e)
                            {
                                throw new Exception($"Convert error TargetType: {valueType?.Name}, String: '{(strValue == null ? "(null)" : strValue)}', StringLength: {strValue?.Length}", e);
                            }
                        }
                    }

                    if (value != null)
                    {
                        Type realValueType = value.GetType();
                        //Type realValueGenType = null;
                        //if (realValueType.IsGenericType)
                        //    realValueGenType = realValueType.GetGenericTypeDefinition();

                        ////递归设置参数
                        //if (!(realValueType.IsPrimitive ||
                        //    realValueType.IsEnum ||
                        //    realValueType == typeof(string) ||
                        //    realValueGenType == typeof(Nullable<>) ||
                        //    realValueGenType == typeof(NullableValue<>) ||
                        //    realValueGenType == typeof(List<>)))
                        //{
                        //    if (_FillEnvironmentVariables(realValueType, ref value, dic, converter, flags))
                        //    {
                        //        changed = true;
                        //    }
                        //}
                        if (realValueType.IsDefined(typeof(EnvironmentVariableAttribute), true))
                        {
                            if (_FillEnvironmentVariables(realValueType, ref value, dic, converter, flags))
                            {
                                changed = true;

                            }
                        }
                    }

                    if (hasValue)
                    {
                        if (!object.Equals(value, oldValue))
                        {
                            changed = true;

                            if (pInfo != null)
                            {
                                if (pInfo.CanWrite)
                                {
                                    pInfo.SetValue(target, value);
                                }
                            }
                            else
                            {
                                if (!fInfo.IsInitOnly)
                                {
                                    fInfo.SetValue(target, value);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Fill Environment Variables Error, Type: {member.DeclaringType.Name}, Member: {member.Name}, ValueType: {valueType?.Name}", e);
                }
            }

            return changed;
        }

        internal static object ConvertValue(object value, Type targetType)
        {
            if (value == null)
            {
                if (targetType == typeof(bool))
                    return false;

                if (targetType.IsValueType)
                {
                    return Activator.CreateInstance(targetType);
                }
                return null;
            }

            if (value.GetType() == targetType)
            {
                return value;
            }


            Type valueType2 = targetType;
            if (targetType.IsGenericType)
            {
                var genericDef = targetType.GetGenericTypeDefinition();
                if (genericDef == typeof(NullableValue<>))
                {
                    valueType2 = targetType.GetGenericArguments()[0];
                }
            }

            if (value is string str)
            {
                if (str != null)
                {
                    //原生类型强制去除 [' '\t\r\n] 看不到的字符
                    if (valueType2.IsPrimitive)
                    {
                        var typeCode = Type.GetTypeCode(valueType2);
                        if (!(typeCode == TypeCode.Object || typeCode == TypeCode.String))
                        {
                            str = str.Trim();
                            value = str;
                        }
                    }
                }

                if (valueType2.IsEnum)
                {
                    if (!string.IsNullOrEmpty(str))
                    {
                        if (Enum.TryParse(valueType2, str, true, out var v))
                        {
                            value = v;
                        }
                    }
                }
                else if (valueType2 == typeof(bool))
                {
                    if (string.IsNullOrEmpty(str))
                    {
                        value = false;
                    }
                    else if (str == "0")
                    {
                        value = false;
                    }
                    else if (str == "1")
                    {
                        value = true;
                    }
                }
            }



            if (value.GetType() != valueType2)
            {
                value = Convert.ChangeType(value, valueType2);
            }

            if (targetType.IsGenericType)
            {
                var genericDef = targetType.GetGenericTypeDefinition();
                if (genericDef == typeof(NullableValue<>))
                {
                    value = Activator.CreateInstance(targetType, value);
                }
            }

            return value;
        }

        #region Command Line Args 扩展命令行参数


        private static CommandArguments _cmdLineArgs;
        private static CommandArguments CmdLineArgs
        {
            get
            {
                if (_cmdLineArgs == null)
                {
                    _cmdLineArgs = new CommandArguments();
                    _cmdLineArgs.AddArgs(Environment.GetCommandLineArgs());
                    AddCommandLineArgs(SettingSettings.UserCommandLineArgs);
                }
                return _cmdLineArgs;
            }
        }

        public static IEnumerable<string> CommandLineArgs => CmdLineArgs.List;


        public static void ParseCommandLineArgs(string cmdLineArgs)
        {
            if (string.IsNullOrEmpty(cmdLineArgs))
                return;

            if (!string.IsNullOrEmpty(cmdLineArgs))
            {
                CmdLineArgs.ParseArgs(cmdLineArgs);
            }
        }

        public static void AddCommandLineArgs(IEnumerable<string> args)
        {
            if (args == null)
                return;
            CmdLineArgs.AddArgs(args);
        }

        public static bool TryGetCommandLineArg(string name, out string value)
        {
            return CmdLineArgs.TryGet(name, out value);
        }

        public static bool TryGetCommandLineArg<T>(string name, out T value)
        {
            return CmdLineArgs.TryGet<T>(name, out value);
        }

        public static void FillCommandLineArgs(object target, ConverterDelegate converter = null)
        {
            CmdLineArgs.Fill(target, converter);
        }

        #endregion


        public static IEnumerable<Type> GetTypesWithAttribute(Type attrType, bool inherit = true)
        {
#if UNITY_EDITOR
            return TypeCache.GetTypesWithAttribute(attrType);
#else

            foreach (var ass in ReferencedAssemblies(attrType.Assembly))
            {
                foreach (var type in ass.GetTypes())
                {
                    if (type.IsDefined(attrType, inherit))
                        yield return type;
                }
            }
#endif

        }

    }

}