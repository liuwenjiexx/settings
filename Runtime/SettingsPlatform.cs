using System;
using System.Collections.Generic;
using System.Linq;

namespace SettingsManagement
{
    public enum SettingsPlatform
    {
        Default,
        /// <summary>
        /// Windows, Mac, Linux
        /// </summary>
        Standalone,
        Server,
        Android,
        iOS,
        WebGL,
        WindowsStoreApps,
        PS4,
        XboxOne,
        tvOS,
        NintendoSwitch,
        Stadia,
        CloudRendering,
        EmbeddedLinux,

        Windows = 20,
        OSX,
        Linux,
        WindowsServer,
        OSXServer,
        LinuxServer,
    }

    public class PlatformNames
    {
        public const string Default = "Default";
        /// <summary>
        /// Windows, Mac, Linux
        /// </summary>
        public const string Standalone = "Standalone";
        public const string Server = "Server";
        public const string Android = "Android";
        public const string iOS = "iOS";
        public const string WebGL = "WebGL";
        public const string WindowsStoreApps = "Windows Store Apps";
        public const string PS4 = "PS4";
        public const string PS5 = "PS5";
        public const string XboxOne = "XboxOne";
        public const string tvOS = "tvOS";
        public const string VisionOS = "VisionOS";
        public const string Switch = "Nintendo Switch";
        public const string Stadia = "Stadia";
        public const string LinuxHeadlessSimulation = "LinuxHeadlessSimulation";
        public const string EmbeddedLinux = "EmbeddedLinux";

        public const string Windows = "Windows";
        public const string OSX = "OSX";
        public const string Linux = "Linux";

        public const string WindowsServer = "WindowsServer";
        public const string OSXServer = "OSXServer";
        public const string LinuxServer = "LinuxServer";

        private static string[] allPlatforms;
        public static string[] AllPlatforms
        {
            get
            {
                if (allPlatforms == null)
                {
                    allPlatforms = new string[]
                    {
                        Standalone,
                        Server,
                        Android,
                        iOS,
                        WebGL,
                        WindowsStoreApps,
                        PS4,
                        PS5,
                        XboxOne,
                        tvOS,
                        Switch,
                        Stadia,
                        LinuxHeadlessSimulation,
                        EmbeddedLinux,
                    };
                    Array.Sort(allPlatforms);
                }
                return allPlatforms;
            }
        }

        private static string[] allPlatforms2;
        public static string[] AllPlatforms2
        {
            get
            {
                if (allPlatforms2 == null)
                {
                    allPlatforms2 = new string[]
                    {
                        Standalone,
                        Server,
                        Android,
                        iOS,
                        WebGL,
                        WindowsStoreApps,
                        PS4,
                        PS5,
                        XboxOne,
                        tvOS,
                        Switch,
                        Stadia,
                        LinuxHeadlessSimulation,
                        EmbeddedLinux,
                        Windows,
                        OSX,
                        Linux,
                        WindowsServer,
                        LinuxServer,
                        OSXServer
                    };
                    Array.Sort(allPlatforms2);
                }
                return allPlatforms2;
            }
        }

        internal static bool IsDedicatedServer
        {
            get
            {
#if UNITY_SERVER
                return true;
#endif
                return false;
            }
        }


        public static string Current
        {
            get
            {
#if UNITY_SERVER
                return PlatformNames.Server;
#endif

#if UNITY_STANDALONE
                return PlatformNames.Standalone;
#elif UNITY_ANDROID
                return PlatformNames.Android;
#elif UNITY_IOS
                return PlatformNames.iOS;
#endif
                return null;
            }
        }

        public static string Current2
        {
            get
            {
#if UNITY_STANDALONE_WIN
#if UNITY_SERVER
                return PlatformNames.WindowsServer;
#else
                return PlatformNames.Windows;
#endif

#elif UNITY_STANDALONE_LINUX

#if UNITY_SERVER
            return PlatformNames.LinuxServer;
#else
            return PlatformNames.Linux;
#endif

#elif UNITY_STANDALONE_OSX

#if UNITY_SERVER
            return PlatformNames.OSXServer;
#else
            return PlatformNames.OSX;
#endif

#endif

                return Current;
            }
        }

        public static bool Is(string platform)
        {
            if (platform == Server)
            {
                return IsDedicatedServer;
            }

            if (platform == Current || platform == Current2)
                return true;

            return false;
        }

        public static bool IsServer(string platform)
        {
            switch (platform)
            {
                case Server:
                case WindowsServer:
                case OSXServer:
                case LinuxServer:
                    return true;
            }
            return false;
        }

        public static bool IsStandalone(string platform)
        {
            switch (platform)
            {
                case Standalone:
                case Windows:
                case OSX:
                case Linux:
                    return true;
            }
            return false;
        }

        public static bool IsSubplatform(string platform)
        {
            switch (platform)
            {
                case Windows:
                case OSX:
                case Linux:
                case WindowsServer:
                case OSXServer:
                case LinuxServer:
                    return true;
            }
            return false;
        }

        public static string GetDisplayName(string platform)
        {
            switch (platform)
            {
                case PlatformNames.Server:
                    return "Dedicated Server";
                case PlatformNames.Standalone:
                    return "Windows, Mac, Linux";
                    //case PlatformNames.WindowsServer:
                    //    return "Windows Dedicated Server";
                    //case PlatformNames.LinuxServer:
                    //    return "Linux Dedicated Server";
                    //case PlatformNames.OSXServer:
                    //    return "Mac Dedicated Server";
            }
            return platform;
        }

        public static string GetShortDisplayName(string platform)
        {
            switch (platform)
            {
                case PlatformNames.Server:
                    return "Server";
                case PlatformNames.Standalone:
                    return "Standalone";
                    //case PlatformNames.WindowsServer:
                    //    return "Windows Server";
                    //case PlatformNames.LinuxServer:
                    //    return "Linux Server";
                    //case PlatformNames.OSXServer:
                    //    return "Mac Server";
            }
            return platform;
        }

        static Dictionary<(string platform, bool invert), string[]> cacheBasePlatforms = new();
        public static string[] GetBasePlatforms(string platform, bool invert = false)
        {
            (string platform, bool invert) key = (platform, invert);
            if (!cacheBasePlatforms.TryGetValue(key, out var value))
            {
                value = BasePlatforms(platform, invert).ToArray();
                cacheBasePlatforms[key] = value;
            }
            return value;
        }

        public static IEnumerable<string> BasePlatforms(string platform, bool invert = false)
        {
            if (string.IsNullOrEmpty(platform))
                yield break;
            if (invert)
            {
                yield return Default;
            }
            else
            {
                yield return platform;
            }
            if (platform != Default)
            {
                if (!(platform == Standalone || platform == Server))
                {
                    if (IsStandalone(platform))
                    {
                        yield return Standalone;
                    }
                    else if (IsServer(platform))
                    {
                        yield return Server;
                    }
                }
                if (invert)
                {
                    yield return platform;
                }
                else
                {
                    yield return Default;
                }
            }
        }

    }

    [Serializable]
    public class PlatformMatch
    {
        public bool isAny = true;
        public string include;
        public string exclude;

        public bool IsMatch(string platform)
        {
            if (string.IsNullOrEmpty(platform))
                return false;


            if (isAny)
            {
                if (!string.IsNullOrEmpty(exclude))
                {
                    var items = exclude.Split(";", StringSplitOptions.RemoveEmptyEntries);

                    if (PlatformNames.IsStandalone(platform))
                    {
                        if (items.Contains(PlatformNames.Standalone) || items.Contains(platform))
                        {
                            return false;
                        }
                    }
                    else if (PlatformNames.IsServer(platform))
                    {
                        if (items.Contains(PlatformNames.Server) || items.Contains(platform))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (items.Contains(platform))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(include))
                {
                    return false;
                }
                var items = include.Split(";", StringSplitOptions.RemoveEmptyEntries);

                if (PlatformNames.IsStandalone(platform))
                {
                    if (!(items.Contains(PlatformNames.Standalone) || items.Contains(platform)))
                    {
                        return false;
                    }
                }
                else if (PlatformNames.IsServer(platform))
                {
                    if (!(items.Contains(PlatformNames.Server) || items.Contains(platform)))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!items.Contains(platform))
                    {
                        return false;
                    }
                }

            }

            return true;
        }
    }

}
