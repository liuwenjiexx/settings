using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SettingsManagement
{
    public interface ISetting
    {
        Settings Settings { get; }

        string Key { get; }

        SettingsScope Scope { get; }

        string RepositoryName { get; }

        object DefaultValue { get; }

        Type ValueType { get; }

        bool IsMultiPlatform { get; }

        bool IsCombine { get; }

        bool Contains(string platform, string variant);

        /// <summary>
        /// 当前是否包含平台值
        /// </summary>
        bool DirectContains(string platform, string variant);

        /// <summary>
        /// 获取当前平台值
        /// </summary>
        object DirectGetValue(string platform, string variant);

        object GetValue(string platform, string variant);
        bool TryGetValue(string platform, string variant, out object value);
        object CopyValue(string platform, string variant);


        bool SetValue(string platform, string variant, object value, bool saveImmediate = false);

        /// <summary>
        /// 删除所有平台值
        /// </summary>
        void Delete(bool saveImmediate = false);

        /// <summary>
        /// 删除指定平台值
        /// </summary>
        void Delete(string platform, string variant, bool saveImmediate = false);

        /// <summary>
        /// 重置指定平台值
        /// </summary>
        void Reset(string platform, string variant, bool saveImmediate = false);


    }
}