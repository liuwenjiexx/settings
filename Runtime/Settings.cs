using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SettingsManagement
{
    public sealed class Settings
    {

        private ISettingsRepository[] repositories;


        internal const SettingsPlatform DefaultPlatform = SettingsPlatform.Default;


        //public Settings(string package, string repositoryName = "Settings")
        //{
        //    if (package == null) throw new ArgumentNullException(nameof(package));
        //    if (repositoryName == null) throw new ArgumentNullException(nameof(repositoryName));

        //    if (Application.isEditor)
        //    {
        //        repositories = new ISettingsRepository[]
        //        {
        //            new PackageSettingsRepository(package, SettingsScope.RuntimeProject, repositoryName),
        //            new PackageSettingsRepository(package, SettingsScope.RuntimeUser, repositoryName),
        //            new PackageSettingsRepository(package, SettingsScope.EditorProject, repositoryName),
        //            new PackageSettingsRepository(package, SettingsScope.EditorUser, repositoryName)
        //        };
        //    }
        //    else
        //    {
        //        repositories = new ISettingsRepository[]
        //        {
        //            new PackageSettingsRepository(package, SettingsScope.RuntimeProject, repositoryName),
        //            new PackageSettingsRepository(package, SettingsScope.RuntimeUser, repositoryName)
        //        };
        //    }


        //}

        public Settings(params ISettingsRepository[] repositories)
        {
            this.repositories = repositories.ToArray();
        }

        public Settings(IEnumerable<ISettingsRepository> repositories)
        {
            this.repositories = repositories.ToArray();
        }

        public IReadOnlyCollection<ISettingsRepository> Repositories => repositories;


        private static string variant;
        private static List<string> _variants = null;

        public static string Variant
        {
            get
            {
                if (_variants == null)
                {
                    InitilizeVariants(SettingSettings.Variant);
                }

                return variant;
            }
        }

        public static IReadOnlyList<string> Variants
        {
            get
            {
                if (_variants == null)
                {
                    InitilizeVariants(SettingSettings.Variant);
                }
                return _variants;
            }
        }

        public event Action BeforeSaved;
        public event Action AfterSaved;

        public static event Action VariantChanged;


        public static void SetVariant(string variant)
        {
            if (string.IsNullOrEmpty(variant))
                variant = null;
            if (variant == SettingSettings.DefaultVariantName)
                variant = null;
            if (Variant == variant)
            {
                return;
            }
            InitilizeVariants(variant);
            VariantChanged?.Invoke();
        }

        internal static void InitilizeVariants(string variant)
        {
            _variants = new List<string>();

            string baseVariant = variant;
            while (true)
            {
                if (string.IsNullOrEmpty(baseVariant))
                    break;
                _variants.Add(baseVariant);
                var variantConfig = SettingSettings.Variants.FirstOrDefault(o => o.variant == baseVariant);
                if (variantConfig == null)
                {
                    throw new Exception($"Variant config null '{baseVariant}'");
                }
                baseVariant = variantConfig.baseVariant;
            }

            _variants.Add(null);
            Settings.variant = _variants[0];
        }

        public ISettingsRepository GetRepository(SettingsScope scope)
        {
            foreach (var repo in repositories)
            {
                if (repo.Scope == scope)
                    return repo;
            }
            return null;
        }
        public ISettingsRepository GetRepository(string name, SettingsScope scope)
        {
            return GetRepository(name, scope, null);
        }

        private ISettingsRepository GetRepository(string name, SettingsScope scope, string key)
        {
            ISettingsRepository result = null;
            if (name == null)
            {
                foreach (var repo in repositories)
                {
                    if (repo.Scope == scope)
                    {
                        result = repo;
                        break;
                    }
                }
            }
            else
            {
                foreach (var repo in repositories)
                {
                    if (repo.Scope == scope && repo.Name == name)
                    {
                        result = repo;
                        break;
                    }
                }
            }

            if (result == null && key != null)
            {
                Debug.LogWarning($"Not found settings repository, key: {key}, scope: {scope}, repositoryName: {name}");
            }

            return result;
        }


        //public void Set<T>(SettingsPlatform platform, string key, T value, SettingsScope scope = SettingsScope.RuntimeProject)
        //{
        //    Set(platform, key, value, null, scope);
        //}
        //public void Set<T>(SettingsPlatform platform, string key, T value, string repositoryName, SettingsScope scope = SettingsScope.RuntimeProject)
        //{
        //    Set(platform.ToString(), key, value, repositoryName, scope);
        //}

        public void Set<T>(string platform, string variant, string key, T value, SettingsScope scope = SettingsScope.RuntimeProject)
        {
            Set(platform, variant, key, value, null, scope);
        }


        public void Set<T>(string platform, string variant, string key, T value, string repositoryName, SettingsScope scope = SettingsScope.RuntimeProject)
        {
            var repo = GetRepository(repositoryName, scope, key);
            if (repo == null)
                return;
            repo.Set(platform, variant, key, value);
        }

        //public T Get<T>(SettingsPlatform platform, string key, string variant, SettingsScope scope = SettingsScope.RuntimeProject, T fallback = default(T))
        //{
        //    return Get<T>(platform, key, variant, null, scope, fallback);
        //}
        //public T Get<T>(SettingsPlatform platform, string key, string variant, string repositoryName, SettingsScope scope = SettingsScope.RuntimeProject, T fallback = default(T))
        //{
        //    return Get<T>(platform.ToString(), key, variant, repositoryName, scope, fallback);
        //}


        public T Get<T>(string platform, string variant, string key, SettingsScope scope = SettingsScope.RuntimeProject, T fallback = default(T))
        {
            return Get<T>(platform, variant, key, null, scope, fallback);
        }

        public T Get<T>(string platform, string variant, string key, string repositoryName, SettingsScope scope = SettingsScope.RuntimeProject, T fallback = default(T))
        {
            var repo = GetRepository(repositoryName, scope, key);
            if (repo == null)
                return fallback;
            return repo.Get(platform, variant, key, fallback);
        }


        //public bool ContainsKey<T>(SettingsPlatform platform, string key, string variant, SettingsScope scope = SettingsScope.RuntimeProject)
        //{
        //    return ContainsKey<T>(platform, key, variant, null, scope);
        //}
        //public bool ContainsKey<T>(SettingsPlatform platform, string variant, string key, string repositoryName, SettingsScope scope = SettingsScope.RuntimeProject)
        //{
        //    return ContainsKey<T>(platform.ToString(), key, variant, repositoryName, scope);
        //}


        //public bool ContainsKey<T>(string platform, string key, string variant, SettingsScope scope = SettingsScope.RuntimeProject)
        //{
        //    return ContainsKey<T>(platform, key, variant, null, scope);
        //}
        public bool ContainsKey<T>(string platform, string variant, string key, string repositoryName, SettingsScope scope = SettingsScope.RuntimeProject)
        {
            var repo = GetRepository(repositoryName, scope, key);
            if (repo == null)
                return false;
            return repo.ContainsKey<T>(platform, variant, key);
        }

        //public void DeleteKey<T>(SettingsPlatform platform, string key, SettingsScope scope = SettingsScope.RuntimeProject)
        //{
        //    DeleteKey<T>(platform, key, null, scope);
        //}
        //public void DeleteKey<T>(SettingsPlatform platform, string key, string repositoryName, SettingsScope scope = SettingsScope.RuntimeProject)
        //{
        //    DeleteKey<T>(platform.ToString(), key, repositoryName, scope);
        //}

        public void DeleteKey<T>(string platform, string variant, string key, SettingsScope scope = SettingsScope.RuntimeProject)
        {
            DeleteKey<T>(platform, variant, key, null, scope);
        }
        public void DeleteKey<T>(string platform, string variant, string key, string repositoryName, SettingsScope scope = SettingsScope.RuntimeProject)
        {
            var repo = GetRepository(repositoryName, scope, key);
            if (repo == null)
                return;
            repo.DeleteKey<T>(platform, variant, key);
        }

        public void Save()
        {
            if (saveScopeStack.Count > 0)
                return;

            BeforeSaved?.Invoke();

            foreach (var repo in this.repositories)
            {
                repo.Save();
            }

            AfterSaved?.Invoke();
        }


        Stack<SettingsSaveScope> saveScopeStack = new();

        internal bool InSaveScope => saveScopeStack.Count > 0;

        public IDisposable BeginSaveScope()
        {
            SettingsSaveScope scope = new(this);

            saveScopeStack.Push(scope);

            return scope;
        }



        class SettingsSaveScope : IDisposable
        {
            private Settings settings;

            public SettingsSaveScope(Settings settings)
            {
                this.settings = settings;
            }

            public void Dispose()
            {
                if (!settings.saveScopeStack.Contains(this))
                    return;
                while (settings.saveScopeStack.Peek() != this)
                    settings.saveScopeStack.Pop();
                if (settings.saveScopeStack.Count == 0)
                {
                    settings.Save();
                }
            }

        }


    }



}