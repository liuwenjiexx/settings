using System;
using UnityEngine;
namespace SettingsManagement
{
    public sealed class PlayerPrefsRepository : ISettingsRepository
    {
        private string packageName;
        private string keyPrefix;

        public PlayerPrefsRepository(string packageName)
        {
            this.packageName = packageName;
            keyPrefix = string.Empty;
            if (!string.IsNullOrEmpty(packageName))
            {
                keyPrefix = packageName + "::";
            }
        }


        public SettingsScope Scope => SettingsScope.RuntimeUser;

        public string Name => "PlayerPrefs";

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
            return PlayerPrefs.HasKey(k);
        }

        public T Get<T>(string platform, string variant, string key, T fallback = default)
        {
            string k = GetKey<T>(platform, variant, key);

            if (!PlayerPrefs.HasKey(k))
                return fallback;

            if (typeof(T) == typeof(string))
                return (T)(object)PlayerPrefs.GetString(k);
            else if (typeof(T) == typeof(float))
                return (T)(object)PlayerPrefs.GetFloat(k);
            else if (typeof(T) == typeof(int))
                return (T)(object)PlayerPrefs.GetInt(k);
            else
                return ValueWrapper<T>.Deserialize(PlayerPrefs.GetString(k));
        }


        public void Set<T>(string platform, string variant, string key, T value)
        {
            string k = GetKey<T>(platform, variant, key);

            if (typeof(T) == typeof(string))
                PlayerPrefs.SetString(k, (string)(object)value);
            else if (typeof(T) == typeof(float))
                PlayerPrefs.SetFloat(k, (float)(object)value);
            else if (typeof(T) == typeof(int))
                PlayerPrefs.SetInt(k, (int)(object)value);
            else
                PlayerPrefs.SetString(k, ValueWrapper<T>.Serialize(value));
            IsDiried = true;
        }


        public void DeleteKey<T>(string platform, string variant, string key)
        {
            string k = GetKey<T>(platform, variant, key);
            PlayerPrefs.DeleteKey(k);
            IsDiried = true;
        }

        public void Save()
        {
            PlayerPrefs.Save();
            IsDiried = false;
        }
    }
}