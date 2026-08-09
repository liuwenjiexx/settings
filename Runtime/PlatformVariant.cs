using System;
using UnityEngine;
namespace SettingsManagement
{
    public struct PlatformVariant : IEquatable<PlatformVariant>
    {
        public readonly string Platform;
        public readonly string Variant;

        public PlatformVariant(string platform, string variant)
        {
            this.Variant = variant;
            this.Platform = platform;
        }

        public bool Equals(PlatformVariant other)
        {
            return Platform == other.Platform &&
                Variant == other.Variant;
        }


        public override bool Equals(object obj)
        {
            return obj is PlatformVariant other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hash = 0;
            if (Variant != null)
                hash = Variant.GetHashCode();
            if (Platform != null)
                hash ^= Platform.GetHashCode();
            return hash;
        }
        public override string ToString()
        {
            return $"{Variant}-{Platform}";
        }

        public static bool operator ==(PlatformVariant a, PlatformVariant b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(PlatformVariant a, PlatformVariant b)
        {
            return !(a == b);
        }
    }
}