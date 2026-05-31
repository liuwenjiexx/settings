using SettingsManagement;
using System;
using UnityEngine;

public class MultiSetting<T> : Setting<T>
{
    private string[] keys;
    private Setting<T>[] baseSettings;
    public MultiSetting(Settings settings, string[] keys, T value, SettingsScope scope = SettingsScope.RuntimeProject, bool combine = false)
        : this(null, settings, keys, value, null, scope, combine: combine)
    {
    }
    public MultiSetting(GetParentDelegate tryGetParentValue, Settings settings, string[] keys, T value, string repository, SettingsScope scope = SettingsScope.RuntimeProject, bool combine = false)
        : base(tryGetParentValue, settings, keys[0], value, repository, scope, combine)
    {
        this.keys = keys;

        baseSettings = new Setting<T>[keys.Length - 1];
        for (int i = 1; i < keys.Length; i++)
        {
            string k = keys[i];
            baseSettings[i - 1] = new(Settings, k, value, scope);
        }
    }

    public override bool TryGetValue(string platform, string variant, out T value)
    {
        if (base.TryGetValue(platform, variant, out value))
            return true;

        for (int i = 0; i < baseSettings.Length; i++)
        {
            if (baseSettings[i].TryGetValue(platform, variant, out value))
            {
                return true;
            }
        }

        return false;
    }
}
