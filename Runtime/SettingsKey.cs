using System;
using UnityEngine;

namespace SettingsManagement
{
    //IEquatable 结构体作为 Dictionary Key 时减少 GC
    //GetHashCode 减少 TryGetValue GC
    public struct SettingsKey : IEquatable<SettingsKey>
    {
        public readonly Type Type;
        public readonly string Platform;
        public readonly string Variant;

        public SettingsKey(Type type, string platform, string variant)
        {
            this.Type = type;
            this.Platform = platform;
            this.Variant = variant;
        }

        public bool Equals(SettingsKey other)
        {
            return Type == other.Type &&
                Platform == other.Platform &&
                Variant == other.Variant;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            if (Type != null)
                hash = Type.GetHashCode();
            if (Platform != null)
                hash ^= Platform.GetHashCode();
            if (Variant != null)
                hash ^= Variant.GetHashCode();
            return hash;
        }

        public override string ToString()
        {
            return $"{Platform}.{Variant}.{Type}";
        }
    }
}
