using System;
using UnityEditor;

namespace SettingsManagement.Editor
{
    /// <summary>
    /// 编辑器各工程共享
    /// </summary>
    public sealed class EditorPrefsRepository : ISettingsRepository
    {
        private string packageName;
        private string projectName;
        private string keyPrefix;

        public EditorPrefsRepository(string packageName, string projectName)
        {
            this.packageName = packageName;
            this.projectName = projectName;
            keyPrefix = string.Empty;
            if (!string.IsNullOrEmpty(packageName))
            {
                keyPrefix = packageName + "::";
            }

            if (!string.IsNullOrEmpty(projectName))
            {
                keyPrefix = projectName + "::" + keyPrefix;
            }
        }

        public SettingsScope Scope => SettingsScope.EditorUser;

        public string Name => "EditorPrefs";
        public bool IsDiried { get; private set; }
        public string FilePath => null;

        private string GetKey<T>(string platform, string variant, string key)
        {
            return GetKey(platform, variant, typeof(T), key);
        }

        private string GetKey(string platform, string variant, Type type, string key)
        {
            if (variant != null)
                return $"{keyPrefix}{platform}::{variant}::{type.FullName}::{key}";
            return $"{keyPrefix}{platform}::{type.FullName}::{key}";
        }

        public bool ContainsKey<T>(string platform, string variant, string key)
        {
            string k = GetKey<T>(platform, variant, key);
            return EditorPrefs.HasKey(k);
        }

        public T Get<T>(string platform, string variant, string key, T fallback = default)
        {
            string k = GetKey<T>(platform, variant, key);

            if (!EditorPrefs.HasKey(k))
                return fallback;

            if (typeof(T) == typeof(string))
                return (T)(object)EditorPrefs.GetString(k);
            else if (typeof(T) == typeof(float))
                return (T)(object)EditorPrefs.GetFloat(k);
            else if (typeof(T) == typeof(int))
                return (T)(object)EditorPrefs.GetInt(k);
            else if (typeof(T) == typeof(bool))
                return (T)(object)EditorPrefs.GetBool(k);
            else
                return ValueWrapper<T>.Deserialize(EditorPrefs.GetString(k));
        }


        public void Set<T>(string platform, string variant, string key, T value)
        {
            string k = GetKey<T>(platform, variant, key);

            if (typeof(T) == typeof(string))
                EditorPrefs.SetString(k, (string)(object)value);
            else if (typeof(T) == typeof(float))
                EditorPrefs.SetFloat(k, (float)(object)value);
            else if (typeof(T) == typeof(int))
                EditorPrefs.SetInt(k, (int)(object)value);
            else if (typeof(T) == typeof(bool))
                EditorPrefs.SetBool(k, (bool)(object)value);
            else
                EditorPrefs.SetString(k, ValueWrapper<T>.Serialize(value));
            //IsDiried = true;
        }


        public void DeleteKey<T>(string platform, string variant, string key)
        {
            string k = GetKey<T>(platform, variant, key);

            if (EditorPrefs.HasKey(k))
            {
                EditorPrefs.DeleteKey(k);
                //IsDiried = true;
            }
        }

        public void Save()
        {

        }

    }
}
