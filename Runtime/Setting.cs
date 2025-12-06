using System;
using System.Collections.Generic;

namespace SettingsManagement
{
    public class Setting<T> : ISetting
    {
        private bool initialized;
        private Settings settings;
        private string key;
        private SettingsScope scope;
        private string repositoryName;
        private T value;
        private T defaultValue;
        private ValueFactoryDelegate valueFactory;
        private bool isValueCreated;  
        private GetParentDelegate tryGetParentValue;

        public delegate Setting<T> GetParentDelegate(string key);

        public Setting(Settings settings, string key, T value, SettingsScope scope = SettingsScope.RuntimeProject, bool combine = false)
            : this(settings, key, value, null, scope, combine: combine)
        {
        }

        public Setting(Settings settings, string key, T value, string repository, SettingsScope scope = SettingsScope.RuntimeProject, bool combine = false)
            : this(null, settings, key, value, null, scope, combine: combine)
        { }

        public Setting(GetParentDelegate tryGetParentValue, Settings settings, string key, T value, SettingsScope scope = SettingsScope.RuntimeProject, bool combine = false)
            : this(tryGetParentValue, settings, key, value, null, scope, combine: combine)
        {
        }

        public Setting(GetParentDelegate tryGetParentValue, Settings settings, string key, T value, string repository, SettingsScope scope = SettingsScope.RuntimeProject, bool combine = false)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            this.tryGetParentValue = tryGetParentValue;
            this.settings = settings;
            this.key = key;
            this.defaultValue = value;
            this.repositoryName = repository;
            this.scope = scope;
            this.IsCombine = combine;
            settings.BeforeSaved += Settings_BeforeSaved;
            initialized = false;
            isValueCreated = false;
        }
        /*
        public Setting(Settings settings, string key, ValueFactory valueFactory, SettingsScope scope = SettingsScope.RuntimeProject)
            : this(settings, key, valueFactory, null, scope)
        {
        }
        public Setting(Settings settings, string key, ValueFactory valueFactory, string repository, SettingsScope scope = SettingsScope.RuntimeProject)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (valueFactory == null)
                throw new ArgumentNullException(nameof(valueFactory));

            this.settings = settings;
            this.key = key;
            this.valueFactory = valueFactory;
            isValueCreated = false;
            this.repositoryName = repository;
            this.scope = scope;
            settings.BeforeSaved += Settings_BeforeSaved;
            initialized = false;

        }
        */

        protected internal GetParentDelegate Parent => tryGetParentValue;

        public Settings Settings => settings;

        public string Key => key;

        public SettingsScope Scope => scope;

        public string RepositoryName => repositoryName;

        object ISetting.DefaultValue => DefaultValue;

        public T DefaultValue
        {
            get
            {
                if (!isValueCreated && valueFactory != null)
                {
                    defaultValue = valueFactory();
                    isValueCreated = true;
                }
                return ValueWrapper<T>.Copy(defaultValue);
            }
        }

        public T Value
        {
            get => GetValue();
            set => SetValue(value, true);
        }

        public Type ValueType => typeof(T);

        public bool IsMultiPlatform { get; set; }

        public bool IsCombine { get; private set; }

        //public ValueFactoryDelegate ValueFactory
        //{
        //    set => valueFactory = value;
        //}

        //        public static SettingsPlatform ExactPlatform
        //        {
        //            get
        //            {

        //#if UNITY_ANDROID
        //                return  SettingsPlatform.Android;
        //#elif UNITY_IOS
        //                return SettingsPlatform.iOS;
        //#endif

        //                bool isServer = false;
        //#if UNITY_SERVER
        //                isServer=true;
        //#endif

        //#if UNITY_STANDALONE_WIN
        //                return isServer ? SettingsPlatform.WindowsServer : SettingsPlatform.Windows;
        //#elif UNITY_STANDALONE_OSX
        //                return isServer ? SettingsPlatform.OSXServer : SettingsPlatform.OSX;
        //#elif UNITY_STANDALONE_LINUX
        //                return isServer ? SettingsPlatform.LinuxServer : SettingsPlatform.Linux;
        //#endif
        //            }
        //        }


        public ValueFactoryDelegate ValueFactory
        {
            get => valueFactory;
            set => valueFactory = value;
        }

        Dictionary<(string platform, string variant), T> cachedValues;


        protected void Initialize()
        {
            if (initialized) return;

            initialized = true;
            if (cachedValues == null)
                cachedValues = new();
            cachedValues.Clear();
            /*    if (SettingSettings.Variant != null && settings.ContainsKey<T>(PlatformNames.Default, key, SettingSettings.Variant, repositoryName, scope))
                {
                    value = settings.Get<T>(PlatformNames.Default, key, SettingSettings.Variant, repositoryName, scope);
                    variant = SettingSettings.Variant;
                    hasDefaultPlatform = true;
                }*/

            if (settings.ContainsKey<T>(PlatformNames.Default, null, key, repositoryName, scope))
            {
                value = settings.Get<T>(PlatformNames.Default, null, key, repositoryName, scope);
                cachedValues[(PlatformNames.Default, null)] = value;
            }
            else
            {
                //if (valueFactory != null)
                //{
                //    value = (T)valueFactory(PlatformNames.Default);
                //}
                //else
                //{
                value = DefaultValue;
                //}
            }

            //if (!typeof(T).IsValueType)
            //{

            //}
            //else
            //{
            //    cachedValues = null;
            //}
        }


        private void Settings_BeforeSaved()
        {
            //同步引用类型值到存储
            if (cachedValues != null)
            {
                foreach (var item in cachedValues)
                {
                    var item2 = item.Key;
                    if (settings.ContainsKey<T>(item2.platform, item2.variant, key, repositoryName, scope))
                    {
                        settings.Set(item2.platform, item2.variant, key, item.Value, repositoryName, scope);
                    }
                }
            }
        }


        public T GetValue()
        {
            return GetValue(PlatformNames.Current2, SettingsUtility.Variant);
        }


        object ISetting.GetValue(string platform, string variant) => GetValue(platform, variant);

        public virtual T GetValue(string platform, string variant)
        {
            Initialize();

            T value;
            if (IsCombine)
            {
                SettingsUtility.GetCombineValues(this, platform, variant, out value);
                return value;
            }

            if (!TryGetValue(platform, variant, out value))
            {
                if (platform == PlatformNames.Default && variant == null)
                {
                    value = this.value;
                }
                else
                {
                    value = DefaultValue;
                }
            }

            return value;
        }

        public T DirectGetValue(string platform, string variant)
        {
            Initialize();
            T value;

            if (platform == PlatformNames.Default && variant == null)
            {
                value = this.value;
                return value;
            }

            if (cachedValues.TryGetValue((platform, variant), out value))
            {
                return value;
            }

            if (settings.ContainsKey<T>(platform, variant, key, repositoryName, scope))
            {
                value = settings.Get<T>(platform, variant, key, repositoryName, scope);
                cachedValues[(platform, variant)] = value;
                return value;
            }


            return DefaultValue;
        }


        object ISetting.DirectGetValue(string platform, string variant)
        {
            return DirectGetValue(platform, variant);
        }


        public bool TryGetValue(string platform, string variant, out T value)
        {
            Initialize();


            foreach (var variant2 in SettingsUtility.EnumerateVariants(variant))
            {
                foreach (var platform2 in PlatformNames.BasePlatforms(platform))
                {
                    if (cachedValues.TryGetValue((platform2, variant2), out value))
                    {
                        return true;
                    }

                    if (settings.ContainsKey<T>(platform2, variant2, key, repositoryName, scope))
                    {
                        value = settings.Get<T>(platform2, variant2, key, repositoryName, scope);
                        cachedValues[(platform2, variant2)] = value;
                        return true;
                    }
                }
            }

            if (tryGetParentValue != null)
            {
                var parent = tryGetParentValue(key);
                if (parent != null && parent.TryGetValue(platform, variant, out value))
                {
                    return true;
                }
            }

            value = default;
            return false;
        }


        public bool Contains(string platform, string variant)
        {
            return settings.ContainsKey<T>(platform, variant, key, repositoryName, scope);
        }

        public bool DirectContains(string platform, string variant)
        {
            return settings.ContainsKey<T>(platform, variant, key, repositoryName, scope);
        }

        /// <summary>
        /// 设置选项 platform: Default, variant: null
        /// </summary>
        public bool SetValue(T value, bool saveImmediate = false)
        {
            return SetValue(PlatformNames.Default, null, value, saveImmediate);
        }

        bool ISetting.SetValue(string platform, string variant, object value, bool saveImmediate)
            => SetValue(platform, variant, (T)value, saveImmediate);

        public bool SetValue(string platform, string variant, T value, bool saveImmediate = false)
        {
            Initialize();


            //bool changed = false;
            //if (!settings.ContainsKey<T>(platform, key, repositoryName, scope))
            //{
            //    changed = true;
            //}
            //else
            //{
            //数组元素不好比较相等
            //    object oldValue = GetValue(platform);
            //    if (!object.Equals(oldValue, value))
            //    {
            //        changed = true;
            //    }
            //}

            var oldValue = GetValue(platform, variant);
            bool changed = false;
            if (!object.Equals(oldValue, value))
            {
                changed = true;
            }
            // if (changed)
            {
                settings.Set<T>(platform, variant, key, value, repositoryName, scope);
                cachedValues[(platform, variant)] = value;

                if (saveImmediate)
                {
                    settings.Save();
                }
            }
            return changed;
        }

        public bool SetValueWithCheck(T value, bool saveImmediate = false)
        {
            return SetValueWithCheck(PlatformNames.Default, null, value, saveImmediate);
        }

  
        public bool SetValueWithCheck(string platform, string variant, T value, bool saveImmediate = false)
        {
            Initialize();


            //bool changed = false;
            //if (!settings.ContainsKey<T>(platform, key, repositoryName, scope))
            //{
            //    changed = true;
            //}
            //else
            //{
            //数组元素不好比较相等
            //    object oldValue = GetValue(platform);
            //    if (!object.Equals(oldValue, value))
            //    {
            //        changed = true;
            //    }
            //}

            var oldValue = GetValue(platform, variant);
            bool changed = false;
            if (!object.Equals(oldValue, value))
            {
                changed = true;
            }
            if (changed)
            {
                settings.Set<T>(platform, variant, key, value, repositoryName, scope);
                cachedValues[(platform, variant)] = value;

                if (saveImmediate)
                {
                    settings.Save();
                }
            }
            return changed;
        }

        public void Delete(bool saveImmediate = false)
        {
            Initialize();
            bool changed = false;

            cachedValues.Clear();
            foreach (string variant in SettingsUtility.EnumerateVariants(SettingsUtility.Variant))
            {
                foreach (string platform in PlatformNames.AllPlatforms2)
                {
                    if (settings.ContainsKey<T>(platform, variant, key, repositoryName, scope))
                    {
                        settings.DeleteKey<T>(platform, variant, key, repositoryName, scope);
                        changed = true;
                    }
                }
            }

            if (changed && saveImmediate)
            {
                settings.Save();
            }

            initialized = false;
        }


        public void Delete(string platform, string variant, bool saveImmediate = false)
        {
            Initialize();
            bool changed = false;

            cachedValues?.Remove((platform, variant));

            if (settings.ContainsKey<T>(platform, variant, key, repositoryName, scope))
            {
                settings.DeleteKey<T>(platform, variant, key, repositoryName, scope);
                changed = true;
            }

            if (changed && saveImmediate)
            {
                settings.Save();
            }

            if (platform == PlatformNames.Default && variant == null)
            {
                value = DefaultValue;
            }
        }




        public void Reset(bool saveImmediate = false)
            => Reset(PlatformNames.Default, SettingsUtility.Variant, saveImmediate);


        public void Reset(string platform, string variant, bool saveImmediate = false)
        {
            Initialize();
            T oldValue = GetValue(platform, variant);
            T newValue = DefaultValue;
            if (!object.Equals(oldValue, newValue))
            {
                SetValue(platform, variant, newValue, saveImmediate);
            }
        }


        public static implicit operator T(Setting<T> setting)
        {
            return setting.Value;
        }

        public override string ToString()
        {
            return $"{scope} setting. Key: {key}  Value: {Value}";
        }

        object ISetting.CopyValue(string platform, string variant)
        {
            T value = GetValue(platform, variant);
            return ValueWrapper<T>.Copy(value);
        }

        bool ISetting.TryGetValue(string platform, string variant, out object value)
        {
            if (TryGetValue(platform, variant, out T v))
            {
                value = v;
                return true;
            }
            value = default(T);
            return false;
        }

        public void SetDiry()
        {
            Initialize();
            SetValue(Value, true); 
        }

        public delegate T ValueFactoryDelegate();

        //public struct ValueFactory
        //{
        //    internal ValueFactoryDelegate valueFactory;
        //    public static implicit operator ValueFactoryDelegate(ValueFactory value)
        //    {
        //        return value.valueFactory;
        //    }
        //    //public static implicit operator ValueFactory(ValueFactoryDelegate value)
        //    //{
        //    //    return new ValueFactory() { valueFactory = value };
        //    //}
        //    public static explicit operator ValueFactory(ValueFactoryDelegate value)
        //    {
        //        return new ValueFactory() { valueFactory = value };
        //    }
        //    public ValueFactory(ValueFactoryDelegate valueFactory)
        //    {
        //        this.valueFactory = valueFactory;
        //    }
        //}
    }


}
