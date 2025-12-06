using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace SettingsManagement
{

    [Serializable]
    public class FileSettingsRepository : ISettingsRepository, ISerializationCallbackReceiver
    {
        [SerializeField]
        private string name;
        private string path;
        private string resourcePath;
        [SerializeField]
        private SettingsScope scope;
        [NonSerialized]
        public Func<object> OnFirstCreateInstance;
        [NonSerialized]
        public Action<object> OnLoadAfter;


        FileSystemWatcher fsw;
        DateTime lastWriteTime;

        private bool initialized;

        private string cachedJson;

        //#if PRETTY_PRINT_JSON
        const bool PrettyPrintJson = true;
        //#else
        //        const bool PrettyPrintJson = false;
        //#endif

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


        public FileSettingsRepository(string path, SettingsScope scope, string name)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            path = path.Replace('\\', '/');

            this.path = path;
            this.name = name;
            this.scope = scope;

            resourcePath = GetResourcesPath(path);

            initialized = false;
        }

        //public PackageSettingsRepository(string baseDir, string package, SettingsScope scope, string name = "Settings")
        //{
        //    if (package == null)
        //        throw new ArgumentNullException(nameof(package));
        //    this.package = package;
        //    this.name = name;
        //    this.scope = scope;
        //    GetSettingsPath(scope, baseDir, package, this.name);
        //    initialized = false;
        //}




        public SettingsScope Scope => scope;

        public string Name => name;

        public string FilePath => path;


        //热更
        public static RawLoaderDelegate RawLoader;
        public delegate void RawLoaderDelegate(string assetPath, ref string data, ref bool handle);


        string GetResourcesPath(string filePath)
        {

            if (filePath.StartsWith("Assets/Resources/"))
            {
                filePath = filePath.Substring("Assets/Resources/".Length);
                filePath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath));
                filePath = filePath.Replace('\\', '/');

                return filePath;
            }
            return null;
        }

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

        private void Initialize()
        {
            if (initialized)
                return;
            initialized = true;

            if (!Application.isEditor)
            {
                if (scope == SettingsScope.EditorProject || scope == SettingsScope.EditorUser)
                {
                    throw new Exception($"Settings repository [{path}] [{Name}] editor scope cannot be used at runtime, scope: {scope}");
                }
            }

            Load();
        }

        private void Load()
        {

            string filePath = this.path;
            try
            {
                items = null;
#if UNITY_EDITOR
                _playingChanged = EditorSettingsProvider.playingChanged;
#endif
                string json = null;
                //if(Application.isPlaying)
                //{
                //    if (!IsRuntime)
                //        throw new Exception($"can't  playing load runtime settings [{type.FullName}]");
                //}

                cachedJson = null;

                bool handle = false;
                if (RawLoader != null)
                {
                    RawLoader(filePath, ref json, ref handle);
                    if (handle)
                    {
                        if (!string.IsNullOrEmpty(json))
                        {
                            FromJson(json);
                        }

                        if (Application.isEditor)
                        {
                            if (File.Exists(filePath))
                            {
                                lastWriteTime = File.GetLastWriteTimeUtc(filePath);
                                EnableFileSystemWatcher();
                            }
                        }
                    }
                }

                if (!handle)
                {
                    bool isFile = false;

                    //编辑器如果没刷新还是旧值
#if UNITY_EDITOR
                    if (File.Exists(filePath))
                    {
                        isFile = true;
                    }
#endif
                    if (!isFile && resourcePath != null)
                    {
                        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
                        if (textAsset)
                        {
                            json = Encoding.UTF8.GetString(textAsset.bytes);
                        }

                        if (!string.IsNullOrEmpty(json))
                        {
                            FromJson(json);
                        }
                    }
                    else
                    {
                        try
                        {
                            if (File.Exists(filePath))
                            {
                                json = File.ReadAllText(filePath, Encoding.UTF8);
                                lastWriteTime = File.GetLastWriteTimeUtc(filePath);
                                EnableFileSystemWatcher();
                            }
                            if (!string.IsNullOrEmpty(json))
                            {
                                FromJson(json);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"load file <{filePath}>", ex);
                        }
                    }
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
                Debug.Log($"{FilePath}\n{json}");
                Debug.LogException(ex);
            }
        }

        string ToJson()
        {
            return JsonUtility.ToJson(this, PrettyPrintJson);
        }


        public void Save()
        {
            //if (Application.isPlaying)
            //{
            //    Debug.LogWarning($"Runtime can't save settings");
            //    return;
            //}

#if UNITY_EDITOR
            //if (_playingChanged != EditorSettingsProvider.playingChanged)
            //{
            //    _playingChanged = EditorSettingsProvider.playingChanged;
            //    initialized = false;
            //    return;
            //}
#endif


            Initialize();

            string filePath = this.path;
            string json = ToJson();

            if (cachedJson != json)
            {
                DisableFileSystemWatcher();

                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, json, new UTF8Encoding(false));

                cachedJson = json;
                lastWriteTime = File.GetLastWriteTimeUtc(filePath);
                EnableFileSystemWatcher();
            }
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




        public void EnableFileSystemWatcher()
        {

            if (!Application.isEditor)
                return;
            if (fsw != null)
                return;
            try
            {
                string filePath = path;
                string dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                    return;
                string fileName = Path.GetFileName(filePath);
                fsw = new FileSystemWatcher();
                fsw.BeginInit();
                fsw.Path = dir;
                fsw.Filter = fileName;
                fsw.NotifyFilter = NotifyFilters.LastWrite;
                fsw.Changed += OnFileSystemWatcher;
                fsw.Deleted += OnFileSystemWatcher;
                fsw.Renamed += OnFileSystemWatcher;
                fsw.Created += OnFileSystemWatcher;
                fsw.IncludeSubdirectories = true;
                fsw.EnableRaisingEvents = true;
                fsw.EndInit();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }



        void DisableFileSystemWatcher()
        {
            if (fsw != null)
            {
                fsw.Dispose();
                fsw = null;
            }
        }


        public void OnFileSystemWatcher(object sender, FileSystemEventArgs e)
        {
#if UNITY_EDITOR
            EditorApplication.delayCall += () =>
            {
                if (!Application.isPlaying)
                {
                    bool changed = true;

                    if (File.Exists(path) && lastWriteTime == File.GetLastWriteTimeUtc(FilePath))
                    {
                        changed = false;
                    }

                    if (changed)
                    {
                        initialized = false;
                    }
                }
            };
#endif
        }


        public override string ToString()
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine($"file: {FilePath}, name: {name}, scope: {scope}");

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


#if UNITY_EDITOR
        internal class EditorSettingsProvider
        {

            public static int playingChanged;

            [InitializeOnLoadMethod]
            static void InitializeOnLoadMethod()
            {
                EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
            }

            private static void EditorApplication_playModeStateChanged(PlayModeStateChange state)
            {
                //丢弃运行时设置
                if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredPlayMode)
                {
                    playingChanged = (playingChanged + 1) % 1000;
                }
            }
        }
#endif


        public static string GetSettingsFolder(SettingsScope scope, string baseDir = null)
        {
            string path = null;
            path = string.Empty;

            if (string.IsNullOrEmpty(baseDir))
            {
                if (scope == SettingsScope.RuntimeProject)
                {
                    path = "Assets/Resources";
                }
                else if (scope == SettingsScope.RuntimeUser)
                {
                    path = Application.persistentDataPath;
                    if (Application.isEditor)
                        path = Path.Combine(path, "Editor");
                }
                else if (scope == SettingsScope.UnityEditor)
                {
                    //#if UNITY_STANDALONE_OSX
                    //                    filePath = "/Library/Logs/Unity";
                    //#else
                    path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity/Editor");
                    //#endif
                }
            }
            else
            {
                path = baseDir;
            }


            if (scope == SettingsScope.RuntimeProject || scope == SettingsScope.EditorProject)
            {
                path = Path.Combine(path, "ProjectSettings");
            }
            else if (scope == SettingsScope.RuntimeUser || scope == SettingsScope.EditorUser)
            {
                path = Path.Combine(path, "UserSettings");
            }
            else if (scope == SettingsScope.UnityEditor)
            {
                path = Path.Combine(path, "EditorSettings");
            }

            return path;
        }

    }
}
