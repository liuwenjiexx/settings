namespace SettingsManagement
{

    public interface ISettingsRepository
    {

        /// <summary>
        /// 设置域
        /// </summary>
        SettingsScope Scope { get; }

        /// <summary>
        /// 标识仓库名称
        /// </summary>
        string Name { get; }

        public bool IsDiried { get; }

        /// <summary>
        /// 序列化文件路径
        /// </summary>
        string FilePath { get; }

        bool ContainsKey<T>(string platform, string variant, string key);

        void Set<T>(string platform, string variant, string key, T value);

        T Get<T>(string platform, string variant, string key, T fallback = default(T));

        void DeleteKey<T>(string platform, string variant, string key);

        void Save();

    }

}