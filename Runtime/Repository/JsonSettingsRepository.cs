using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace SettingsManagement
{
    [Serializable]
    public class JsonSettingsRepository : ISettingsRepository, ISerializationCallbackReceiver
    {
        [SerializeField]
        private string name;
        [SerializeField]
        private SettingsScope scope;

        [NonSerialized]
        public Func<object> OnFirstCreateInstance;
        [NonSerialized]
        public Action<object> OnLoadAfter;

        private Func<string> loadCallback;

        private Action<string> saveCallback;

        private bool initialized;

        private string cachedJson;

        //#if PRETTY_PRINT_JSON
        public bool PrettyPrintJson = true;
        //#else
        //        const bool PrettyPrintJson = false;
        //#endif


        public JsonSettingsRepository(string name, SettingsScope scope)
        {
            this.name = name;
            this.scope = scope;
            initialized = false;
        }

        [Serializable]
        struct SettingsEntry
        {
            public string type;
            public string key;
            public string platform;
            public string variant;
            public string value;
        }


        [SerializeField]
        private List<SettingsEntry> items = new List<SettingsEntry>();


        struct SettingsKey
        {
            public Type type;
            public string platform;
            public string variant;

            public SettingsKey(Type type, string platform, string variant)
            {
                this.type = type;
                this.platform = platform;
                this.variant = variant;
            }

            public override string ToString()
            {
                return $"{platform}.{variant}.{type}";
            }
        }
        //struct VariantKey
        //{
        //    public string key;
        //    public string variant;
        //    public VariantKey(string key, string variant)
        //    {
        //        this.key = key;
        //        this.variant = variant;
        //    }
        //}

        [NonSerialized]
        Dictionary<SettingsKey, Dictionary<string, string>> dic = new();





        public virtual SettingsScope Scope => scope;

        public string Name => name;

        public string FilePath => null;

        public event Action RequestSave;

        private SettingsKey GetKey<T>(string platform, string variant)
        {
            SettingsKey _key = new(typeof(T), platform, variant);
            return _key;
        }

        public bool ContainsKey<T>(string platform, string variant, string key)
        {
            Initialize();
            SettingsKey _key = GetKey<T>(platform, variant);
            return dic.ContainsKey(_key) && dic[_key].ContainsKey(key);
        }

        public T Get<T>(string platform, string variant, string key, T fallback = default)
        {
            Initialize();
            if (key == null) throw new ArgumentNullException(nameof(key));

            SettingsKey _key = GetKey<T>(platform, variant);
            if (dic.TryGetValue(_key, out var entries) && entries.TryGetValue(key, out var _value))
            {
                try
                {
                    return ValueWrapper<T>.Deserialize(_value);
                }
                catch
                {
                    return fallback;
                }
            }

            return fallback;
        }

        public void Set<T>(string platform, string variant, string key, T value)
        {
            Initialize();

            if (key == null) throw new ArgumentNullException(nameof(key));


            SetJson(typeof(T), platform, variant, key, ValueWrapper<T>.Serialize(value));
        }

        internal void SetJson(Type type, string platform, string variant, string key, string value)
        {

            SettingsKey _key = new(type, platform, variant);
            Dictionary<string, string> entries;

            if (!dic.TryGetValue(_key, out entries))
            {
                dic.Add(_key, entries = new());
            }

            if (entries.ContainsKey(key))
            {
                entries[key] = value;
            }
            else
            {
                entries.Add(key, value);
            }
        }

        public void DeleteKey<T>(string platform, string variant, string key)
        {
            Initialize();

            SettingsKey _key = GetKey<T>(platform, variant);
            if (!(dic.TryGetValue(_key, out var entries) && entries.ContainsKey(key)))
                return;

            entries.Remove(key);
        }

        [NonSerialized]
        private int _playingChanged;

        protected virtual void Initialize()
        {
            if (initialized)
                return;
            initialized = true;
            string json = loadCallback();
            Load(json);
        }

        public virtual void Load(string json)
        {
            try
            {
                initialized = true;
                items = null;

                cachedJson = null;

                if (!string.IsNullOrEmpty(json))
                {
                    FromJson(json);
                }

                cachedJson = json;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }



        void FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }
            try
            {
                var oldScope = scope;
                var oldName = name;
                JsonUtility.FromJsonOverwrite(json, this);
                scope = oldScope;
                name = oldName;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this, PrettyPrintJson);
        }


        public void Save()
        {
            Initialize();

            RequestSave?.Invoke();

            //string json = ToJson();

            //if (cachedJson != json)
            //{
            //    saveCallback(json);
            //    cachedJson = json;
            //}
        }




        public void OnBeforeSerialize()
        {
            try
            {
                ///反序列化有错误
                //if (items == null)
                //    return;
                if (items == null)
                    items = new();
                items.Clear();

                foreach (var item in dic)
                {
                    foreach (var entry in item.Value)
                    {
                        string value;
                        value = entry.Value;

                        items.Add(new SettingsEntry()
                        {
                            platform = item.Key.platform,
                            variant = item.Key.variant,
                            type = item.Key.type.AssemblyQualifiedName,
                            key = entry.Key,
                            value = value
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void OnAfterDeserialize()
        {
            dic.Clear();
            if (items == null)
                items = new();

            Dictionary<string, string> entries;
            string variant;
            foreach (var item in items)
            {
                var type = Type.GetType(item.type);

                if (type == null)
                {
                    Debug.LogWarning("Could not instantiate type \"" + item.type + "\". Skipping key: " + item.key + ".");
                    continue;
                }
                variant = item.variant;
                if (variant == string.Empty)
                    variant = null;
                SettingsKey _key = new(type, item.platform, variant);

                if (!dic.TryGetValue(_key, out entries))
                {
                    entries = new();
                    dic.Add(_key, entries);
                }

                entries[item.key] = item.value;
            }

        }





        public override string ToString()
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine($"name: {name}, scope: {scope}");

            foreach (var type in dic)
            {
                builder.AppendLine("Type: " + type.Key);

                foreach (var entry in type.Value)
                {
                    builder.AppendLine(string.Format("   {0,-64}{1}", entry.Key, entry.Value));
                }
            }

            return builder.ToString();
        }


    }
}