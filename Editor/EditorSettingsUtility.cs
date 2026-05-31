using Codice.Client.Common;
using SettingsManagement.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UIElements;

namespace SettingsManagement.Editor
{
    public static class EditorSettingsUtility
    {

        #region UnityPackage

        static Dictionary<string, string> unityPackageDirectories = new Dictionary<string, string>();


        public static string GetPackageDirectory(string packageName)
        {
            return GetUnityPackageDirectory(packageName);
        }

        //2021/4/13
        internal static string GetUnityPackageDirectory(string packageName)
        {
            if (!unityPackageDirectories.TryGetValue(packageName, out var path))
            {
                var tmp = Path.Combine("Packages", packageName);
                if (Directory.Exists(tmp) && File.Exists(Path.Combine(tmp, "package.json")))
                {
                    path = tmp;
                }

                if (path == null)
                {
                    foreach (var dir in Directory.GetDirectories("Assets", "*", SearchOption.AllDirectories))
                    {
                        if (string.Equals(Path.GetFileName(dir), packageName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (File.Exists(Path.Combine(dir, "package.json")))
                            {
                                path = dir;
                                break;
                            }
                        }
                    }
                }

                if (path == null)
                {
                    foreach (var pkgPath in Directory.GetFiles("Assets", "package.json", SearchOption.AllDirectories))
                    {
                        try
                        {
                            if (JsonUtility.FromJson<_UnityPackage>(File.ReadAllText(pkgPath, System.Text.Encoding.UTF8)).name == packageName)
                            {
                                path = Path.GetDirectoryName(pkgPath);
                                break;
                            }
                        }
                        catch { }
                    }
                }

                if (path != null)
                {
                    path = path.Replace('\\', '/');
                }
                unityPackageDirectories[packageName] = path;
            }
            return path;
        }

        [Serializable]
        class _UnityPackage
        {
            public string name;
        }

        #endregion

        public static string GetRuntimePackageDir(string packageName)
        {
            string dir = GetUnityPackageDirectory(packageName);
            return $"{dir}/Runtime";
        }

        public static string GetEditorPackageDir(string packageName)
        {
            string dir = GetUnityPackageDirectory(packageName);
            return $"{dir}/Editor";
        }

        public static string GetTestsRuntimePackageDir(string packageName)
        {
            string dir = GetUnityPackageDirectory(packageName);
            return $"{dir}/Tests/Runtime";
        }

        public static string GetTestsEditorPackageDir(string packageName)
        {
            string dir = GetUnityPackageDirectory(packageName);
            return $"{dir}/Tests/Editor";
        }




        public static string GetRuntimeUXMLPath(string packageName, string uxml)
        {
            string dir = GetRuntimePackageDir(packageName);
            return $"{dir}/UXML/{uxml}.uxml";
        }


        public static string GetEditorUXMLPath(string packageName, string uxml)
        {
            string dir = GetEditorPackageDir(packageName);
            return $"{dir}/UXML/{uxml}.uxml";
        }

        public static string GetTestsEditorUXMLPath(string packageName, string uxml)
        {
            string dir = GetTestsEditorPackageDir(packageName);
            return $"{dir}/UXML/{uxml}.uxml";
        }

        public static string GetTestsRuntimeUXMLPath(string packageName, string uxml)
        {
            string dir = GetTestsRuntimePackageDir(packageName);
            return $"{dir}/UXML/{uxml}.uxml";
        }


        public static TemplateContainer LoadUXML(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            TemplateContainer treeRoot = null;
            if (asset)
            {
                treeRoot = asset.CloneTree();
            }
            else
            {
                Debug.LogError("Load UXML null: " + path);
            }
            return treeRoot;
        }

        public static TemplateContainer LoadUXML(VisualElement parent, string path)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            TemplateContainer treeRoot = LoadUXML(path);

            if (treeRoot != null)
            {
                parent.Add(treeRoot);
            }
            return treeRoot;
        }

        public static string GetRuntimeUSSPath(string packageName, string uss)
        {
            string dir = GetRuntimePackageDir(packageName);
            if (string.IsNullOrEmpty(dir))
                return null;
            return $"{dir}/USS/{uss}.uss";
        }

        public static string GetEditorUSSPath(string packageName, string uss)
        {
            string dir = GetEditorPackageDir(packageName);
            if (string.IsNullOrEmpty(dir))
                return null;
            return $"{dir}/USS/{uss}.uss";
        }

        public static string GetTestsRuntimeUSSPath(string packageName, string uss)
        {
            string dir = GetTestsRuntimePackageDir(packageName);
            if (string.IsNullOrEmpty(dir))
                return null;
            return $"{dir}//USS/{uss}.uss";
        }

        public static string GetTestsEditorUSSPath(string packageName, string uss)
        {
            string dir = GetTestsEditorPackageDir(packageName);
            if (string.IsNullOrEmpty(dir))
                return null;
            return $"{dir}/USS/{uss}.uss";
        }



        public static StyleSheet LoadUSS(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (style == null)
            {
                Debug.LogError("Load USS null: " + path);
            }
            return style;
        }

        public static StyleSheet LoadUSS(VisualElement elem, string path)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            var style = LoadUSS(path);
            if (style != null)
            {
                elem.styleSheets.Add(style);
            }
            return style;
        }


        //切换 DedicatedServer 平台
        //EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(BuildTarget.StandaloneWindows64), BuildTarget.StandaloneWindows64);
        //EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;
        private static BuildTargetGroup[] supportedBuildTargetGroups;
        public static IReadOnlyCollection<BuildTargetGroup> SupportedBuildTargetGroups
        {
            get
            {
                if (supportedBuildTargetGroups == null)
                {
                    //Dictionary<int, BuildTargetGroup> allGroups = new();

                    //HashSet<string> obsoleteGroups = new();
                    //foreach (var field in typeof(BuildTargetGroup).GetFields())
                    //{
                    //    if (field.IsDefined(typeof(ObsoleteAttribute), false))
                    //    {
                    //        obsoleteGroups.Add(field.Name);
                    //    }
                    //}

                    //foreach (BuildTargetGroup group in ((IEnumerable<BuildTargetGroup>)Enum.GetValues(typeof(BuildTargetGroup))))
                    //{
                    //    if (obsoleteGroups.Contains(group.ToString())) {
                    //        continue;
                    //    }
                    //    allGroups[(int)group] = group;
                    //}


                    HashSet<string> obsoleteTargets = new();
                    foreach (var field in typeof(BuildTarget).GetFields())
                    {
                        if (field.IsDefined(typeof(ObsoleteAttribute), false))
                        {
                            obsoleteTargets.Add(field.Name);
                        }
                    }

                    List<BuildTargetGroup> targetGroups = new List<BuildTargetGroup>();
                    foreach (BuildTarget buildTarget in ((IEnumerable<BuildTarget>)Enum.GetValues(typeof(BuildTarget))))
                    {
                        if (obsoleteTargets.Contains(buildTarget.ToString()))
                            continue;
                        BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
                        //if (allGroups.ContainsKey((int)targetGroup))
                        //    targetGroup = allGroups[(int)targetGroup];

                        if (targetGroups.Contains(targetGroup))
                            continue;
                        if (!BuildPipeline.IsBuildTargetSupported(targetGroup, buildTarget))
                        {
                            continue;
                        }

                        targetGroups.Add(targetGroup);
                    }

                    targetGroups = targetGroups.OrderBy(o => o.ToString()).ToList();

                    foreach (BuildTargetGroup item in (new BuildTargetGroup[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android, BuildTargetGroup.iOS })
                        .Reverse())
                    {
                        targetGroups = targetGroups.OrderBy(o => o == item ? -1 : 0).ToList();
                    }

                    supportedBuildTargetGroups = targetGroups.ToArray();
                }

                return supportedBuildTargetGroups;

            }
        }

        private static NamedBuildTarget[] supportedNamedBuildTargets;
        public static IReadOnlyCollection<NamedBuildTarget> SupportedNamedBuildTargets
        {
            get
            {
                if (supportedNamedBuildTargets == null)
                {
                    List<NamedBuildTarget> targets = new List<NamedBuildTarget>();
                    foreach (var group in SupportedBuildTargetGroups)
                    {
                        targets.Add(NamedBuildTarget.FromBuildTargetGroup(group));
                    }
                    bool supportedServer = false;
#if UNITY_SERVER
                    supportedServer = true;
#else
                    string path = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines");
                    foreach (var dir in new string[]
                    {
                        "LinuxStandaloneSupport/Variations/linux64_server_development_il2cpp",
                        "LinuxStandaloneSupport/Variations/linux64_server_development_mono",
                        "windowsstandalonesupport/Variations/win64_server_development_il2cpp",
                        "windowsstandalonesupport/Variations/win64_server_development_mono",
                        "OSXStandaloneSupport/Variations/osx_server_development_il2cpp",
                        "OSXStandaloneSupport/Variations/osx_server_development_il2cpp"
                    })
                    {
                        if (Directory.Exists(Path.Combine(path, dir)))
                        {
                            supportedServer = true;
                        }
                    }

#endif
                    if (supportedServer && !targets.Contains(NamedBuildTarget.Server))
                    {
                        targets.Add(NamedBuildTarget.Server);
                    }

                    supportedNamedBuildTargets = targets.ToArray();
                }
                return supportedNamedBuildTargets;
            }
        }

        private static SettingsPlatform[] supportedPlatforms;
        public static IReadOnlyCollection<SettingsPlatform> SupportedPlatforms
        {
            get
            {
                if (supportedPlatforms == null)
                {
                    List<SettingsPlatform> list = new();
                    foreach (var item in SupportedNamedBuildTargets)
                    {
                        SettingsPlatform platform = NamedBuildTargetToPlatform(item);
                        if (!list.Contains(platform))
                        {
                            list.Add(platform);
                        }
                    }
                    supportedPlatforms = list.ToArray();
                }
                return supportedPlatforms;
            }
        }

        private static string[] supportedPlatformNames;
        public static IReadOnlyCollection<string> SupportedPlatformNames
        {
            get
            {
                if (supportedPlatformNames == null)
                {
                    List<string> list = new();
                    foreach (var item in SupportedNamedBuildTargets)
                    {
                        string platform = NamedBuildTargetToPlatformName(item);
                        if (!list.Contains(platform))
                        {
                            list.Add(platform);
                        }
                    }
                    supportedPlatformNames = list.ToArray();
                }
                return supportedPlatformNames;
            }
        }

        public static IReadOnlyCollection<string> SupportedPlatformNames2
        {
            get
            {
                List<string> platforms = new();
                foreach (var platform in EditorSettingsUtility.SupportedPlatformNames)
                {
                    platforms.Add(platform);
                    switch (platform)
                    {
                        case PlatformNames.Standalone:
                            platforms.Add(PlatformNames.Windows);
                            platforms.Add(PlatformNames.OSX);
                            platforms.Add(PlatformNames.Linux);
                            break;
                        case PlatformNames.Server:
                            platforms.Add(PlatformNames.WindowsServer);
                            platforms.Add(PlatformNames.OSXServer);
                            platforms.Add(PlatformNames.LinuxServer);
                            break;
                        default:

                            break;
                    }
                }
                return platforms;
            }
        }

        public static BuildTarget CurrentBuildTarget => EditorUserBuildSettings.activeBuildTarget;

        public static BuildTargetGroup CurrentBuildTargetGroup => BuildPipeline.GetBuildTargetGroup(CurrentBuildTarget);

        public static NamedBuildTarget CurrentNamedBuildTarget => ToNamedBuildTarget(CurrentBuildTargetGroup);

        public static NamedBuildTarget ToNamedBuildTarget(BuildTargetGroup buildTargetGroup)
        {
            NamedBuildTarget namedBuildTarget;
            if (buildTargetGroup == BuildTargetGroup.Standalone && EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server)
            {
                namedBuildTarget = NamedBuildTarget.Server;
            }
            else
            {
                namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            }
            return namedBuildTarget;
        }




        public static string SettingsUSSPath { get; private set; } = GetEditorUSSPath(SettingsUtility.GetPackageName(typeof(EditorSettingsUtility)), "Settings");
        /*
                [Obsolete]
                public static VisualElement CreatePlatformSettingsPanel(VisualElement parent, Action<VisualElement, SettingsPlatform> onCreate, Func<SettingsPlatform, bool> hasOverride, Action<SettingsPlatform, bool> onOverride)
                {
                    var root = new VisualElement();
                    root.AddToClassList("settings-platform-panel");

                    parent.Add(root);

                    var headerContainer = new VisualElement();
                    headerContainer.AddToClassList("settings-platform-panel_header_container");
                    root.Add(headerContainer);

                    var contentContainer = new VisualElement();
                    contentContainer.AddToClassList("settings-platform-panel_content_container");
                    root.Add(contentContainer);


                    Toggle overrideTgl = new Toggle();
                    var contentParent = new VisualElement();

                    overrideTgl.RegisterValueChangedCallback(e =>
                    {
                        var active = headerContainer.Q(className: PlatformSettingsPanel_HeaderActive_ClassName);
                        if (active == null || active.userData == null)
                            return;
                        var platform = (SettingsPlatform)active.userData;

                        if (platform == SettingsPlatform.Default)
                            return;

                        contentParent.SetEnabled(e.newValue);

                        onOverride(platform, e.newValue);
                    });
                    contentContainer.Add(overrideTgl);

                    contentParent.AddToClassList("settings-platform-panel_content");
                    contentContainer.Add(contentParent);


                    Action<SettingsPlatform> showPlatform = (platform) =>
                    {
                        var buildTarget = PlatformToNamedBuildTarget(platform);

                        var header = headerContainer.Q(className: PlatformSettingsPanel_Header_Group_Prefix + platform);
                        if (header == null)
                            return;
                        if (header.ClassListContains(PlatformSettingsPanel_HeaderActive_ClassName))
                            return;
                        headerContainer.Query(className: PlatformSettingsPanel_Header_ClassName).ForEach(o =>
                        {
                            o.RemoveFromClassList(PlatformSettingsPanel_HeaderActive_ClassName);
                        });

                        header.AddToClassList(PlatformSettingsPanel_HeaderActive_ClassName);

                        contentParent.Clear();

                        if (platform != SettingsPlatform.Default)
                        {
                            overrideTgl.style.display = DisplayStyle.Flex;
                            overrideTgl.text = $"Override For {GetBuildTargetDisplayName(buildTarget)}";
                            if (hasOverride(platform))
                            {
                                overrideTgl.SetValueWithoutNotify(true);
                                contentParent.SetEnabled(true);
                            }
                            else
                            {
                                overrideTgl.SetValueWithoutNotify(false);
                                contentParent.SetEnabled(false);
                            }
                        }
                        else
                        {
                            contentParent.SetEnabled(true);
                            overrideTgl.style.display = DisplayStyle.None;
                        }

                        onCreate(contentParent, platform);
                    };

                    IEnumerable<SettingsPlatform> platforms = SupportedPlatforms;

                    platforms = new SettingsPlatform[] { SettingsPlatform.Default }.Concat(platforms);

                    foreach (var platform in platforms)
                    {
                        var buildTarget = PlatformToNamedBuildTarget(platform);
                        VisualElement header = new VisualElement();
                        header.AddToClassList(PlatformSettingsPanel_Header_ClassName);

                        header.AddToClassList(PlatformSettingsPanel_Header_Group_Prefix + platform);
                        header.tooltip = $"{GetBuildTargetDisplayName(buildTarget)} settings";
                        header.userData = platform;

                        header.RegisterCallback<MouseDownEvent>(e =>
                        {
                            showPlatform(platform);
                        });


                        var iconImage = GetBuildTargetIcon(buildTarget);


                        if (iconImage)
                        {
                            Image icon = new Image();
                            icon.AddToClassList("settings-platform-panel_header_icon");
                            header.Add(icon);
                            icon.image = iconImage;
                        }
                        else
                        {
                            Label headerText = new Label();
                            if (buildTarget == NamedBuildTarget.Unknown)
                            {
                                headerText.text = platform.ToString();
                            }
                            else
                            {
                                headerText.text = GetBuildTargetDisplayName(buildTarget);
                            }
                            header.Add(headerText);
                        }

                        headerContainer.Add(header);

                    }

                    if (platforms.Contains(SettingsPlatform.Standalone))
                    {
                        showPlatform(SettingsPlatform.Standalone);
                    }
                    else
                    {
                        showPlatform(platforms.First());
                    }


                    return root;
                }
                */

         static VisualElement CreatePlatformSettingsPanel(
            VisualElement parent,
            Action<VisualElement, string> onCreate,
            Func<string, bool> hasOverride,
            Action<string, bool> onOverride,
            string[] platforms = null,
            string activePlatform = null)
        {
            CreateSettingViewOptions options = new CreateSettingViewOptions()
            {
                parent = parent,
                createPlatformMembers = onCreate,
                hasOverride = hasOverride,
                onOverride = onOverride,
                platforms = platforms,
                activePlatform = activePlatform
            };
            if (platforms == null)
            {
                List<string> platformList = new();
                platformList.Add(PlatformNames.Default);
                //if (options.showSubplatform)
                {
                    platformList.AddRange(SupportedPlatformNames2);
                }
                //else
                //{
                //    platformList.AddRange(SupportedPlatformNames);
                //}
                if (options.filterPlatform != null)
                {
                    platformList.RemoveAll(o => !options.filterPlatform(o));
                }
                platforms = platformList.ToArray();
                options.platforms = platforms;
            }
            return SettingsViewUtility.CreatePlatformSettingsPanel(options);
        }


        static string GetBuildTargetName(NamedBuildTarget group)
        {
            if (group == NamedBuildTarget.Unknown)
            {
                return "Default";
            }
            else if (group == NamedBuildTarget.iOS)
            {
                return "iOS";
            }
            return group.TargetName;
        }
        static string GetBuildTargetDisplayName(NamedBuildTarget group)
        {

            if (group == NamedBuildTarget.Unknown)
            {
                return "Default";
            }
            else if (group == NamedBuildTarget.iOS)
            {
                return "iOS";
            }
            else if (group == NamedBuildTarget.Server)
            {
                return "Dedicated Server";
            }
            else if (group == NamedBuildTarget.Standalone)
            {
                return "Windows, Mac, Linux";
            }
            return group.TargetName;
        }



        public static Texture GetBuildTargetGroupIcon(BuildTargetGroup buildTargetGroup)
        {
            switch (buildTargetGroup)
            {
                case BuildTargetGroup.Standalone:
                    return EditorGUIUtility.IconContent("d_BuildSettings.Standalone.Small@2x")?.image;
                case BuildTargetGroup.Android:
                    return EditorGUIUtility.IconContent("d_BuildSettings.Android.Small@2x")?.image;
                case BuildTargetGroup.iOS:
                    return EditorGUIUtility.IconContent("d_BuildSettings.iPhone.Small@2x")?.image;
                case BuildTargetGroup.PS4:
                    return EditorGUIUtility.IconContent("d_BuildSettings.PS4@2x")?.image;
                case BuildTargetGroup.PS5:
                    return EditorGUIUtility.IconContent("d_BuildSettings.PS5@2x")?.image;
                case BuildTargetGroup.WebGL:
                    return EditorGUIUtility.IconContent("d_BuildSettings.WebGL@2x")?.image;
                case BuildTargetGroup.Switch:
                    return EditorGUIUtility.IconContent("d_BuildSettings.Switch@2x")?.image;
                case BuildTargetGroup.tvOS:
                    return EditorGUIUtility.IconContent("d_BuildSettings.tvOS@2x")?.image;
            }
            return null;
        }

        public static Texture GetBuildTargetIcon(NamedBuildTarget buildTarget)
        {
            if (buildTarget == NamedBuildTarget.Server)
            {
                return EditorGUIUtility.IconContent("d_BuildSettings.DedicatedServer.Small@2x")?.image;
            }

            return GetBuildTargetGroupIcon(buildTarget.ToBuildTargetGroup());
        }



        public static void CreateSettingView(VisualElement root, Type type, object instance = null, Func<ISetting, bool> filter = null, string[] platforms = null)
        {
            CreateSettingViewOptions options = new CreateSettingViewOptions();
            options.parent = root;
            options.OwnerSettingsType = type;
            options.instance = instance;
            options.filter = filter;
            options.platforms = platforms;
            CreateSettingView(options);
        }


        public static void CreateSettingView(CreateSettingViewOptions options)
        {
            if (options.platforms == null)
            {
                List<string> platformList = new();
                platformList.Add(PlatformNames.Default);
                //if (options.showSubplatform)
                {
                    platformList.AddRange(SupportedPlatformNames2);
                }
                //else
                //{
                //    platformList.AddRange(SupportedPlatformNames);
                //}
                if (options.filterPlatform != null)
                {
                    platformList.RemoveAll(o => !options.filterPlatform(o));
                }
                options.platforms = platformList.ToArray();
            }

            SettingsViewUtility.CreateSettingView(options);
        }



        private static Dictionary<SettingsPlatform, NamedBuildTarget> platformToBuildTargets;
        public static NamedBuildTarget PlatformToNamedBuildTarget(SettingsPlatform platform)
        {
            if (platformToBuildTargets == null)
            {
                NamedBuildTargetToPlatform(NamedBuildTarget.Standalone);
                platformToBuildTargets = new();
                foreach (var item in buildTargetToPlatforms)
                {
                    platformToBuildTargets[item.Value] = item.Key;
                }
            }

            if (platformToBuildTargets.TryGetValue(platform, out var buildTarget))
            {
                return buildTarget;
            }
            return NamedBuildTarget.Unknown;
        }




        private static Dictionary<NamedBuildTarget, SettingsPlatform> buildTargetToPlatforms;

        public static SettingsPlatform NamedBuildTargetToPlatform(NamedBuildTarget buildTarget)
        {
            if (buildTargetToPlatforms == null)
            {
                buildTargetToPlatforms = new()
                {
                    {NamedBuildTarget.Unknown, SettingsPlatform.Default},
                    {NamedBuildTarget.Standalone, SettingsPlatform.Standalone},
                    {NamedBuildTarget.Server, SettingsPlatform.Server},
                    {NamedBuildTarget.Android, SettingsPlatform.Android},
                    {NamedBuildTarget.iOS, SettingsPlatform.iOS},
                    {NamedBuildTarget.WebGL, SettingsPlatform.WebGL},
                    {NamedBuildTarget.WindowsStoreApps, SettingsPlatform.WindowsStoreApps},
                    {NamedBuildTarget.PS4, SettingsPlatform.PS4},
                    {NamedBuildTarget.XboxOne, SettingsPlatform.XboxOne},
                    {NamedBuildTarget.NintendoSwitch, SettingsPlatform.NintendoSwitch},
                    {NamedBuildTarget.Stadia, SettingsPlatform.Stadia},
#if UNITY_2022_3_OR_NEWER
                    {NamedBuildTarget.LinuxHeadlessSimulation, SettingsPlatform.CloudRendering},
#else
                    {NamedBuildTarget.LinuxHeadlessSimulation, SettingsPlatform.CloudRendering},
#endif
                    {NamedBuildTarget.EmbeddedLinux, SettingsPlatform.EmbeddedLinux},
                };
            }

            if (buildTargetToPlatforms.TryGetValue(buildTarget, out var platform))
            {
                return platform;
            }
            return SettingsPlatform.Default;
        }




        private static Dictionary<string, NamedBuildTarget> platformToBuildTargets2;
        public static NamedBuildTarget PlatformNameToNamedBuildTarget(string platform)
        {
            if (platformToBuildTargets2 == null)
            {
                NamedBuildTargetToPlatformName(NamedBuildTarget.Standalone);
                platformToBuildTargets2 = new();
                foreach (var item in buildTargetToPlatforms2)
                {
                    platformToBuildTargets2[item.Value] = item.Key;
                }
                platformToBuildTargets2[PlatformNames.Windows] = NamedBuildTarget.Standalone;
                platformToBuildTargets2[PlatformNames.OSX] = NamedBuildTarget.Standalone;
                platformToBuildTargets2[PlatformNames.Linux] = NamedBuildTarget.Standalone;

                platformToBuildTargets2[PlatformNames.WindowsServer] = NamedBuildTarget.Server;
                platformToBuildTargets2[PlatformNames.OSXServer] = NamedBuildTarget.Server;
                platformToBuildTargets2[PlatformNames.LinuxServer] = NamedBuildTarget.Server;
            }

            if (platformToBuildTargets2.TryGetValue(platform, out var buildTarget))
            {
                return buildTarget;
            }


            return NamedBuildTarget.Unknown;
        }

        private static Dictionary<NamedBuildTarget, string> buildTargetToPlatforms2;
        public static string NamedBuildTargetToPlatformName(NamedBuildTarget buildTarget)
        {
            if (buildTargetToPlatforms2 == null)
            {
                buildTargetToPlatforms2 = new()
                {
                    {NamedBuildTarget.Unknown, PlatformNames.Default},
                    {NamedBuildTarget.Standalone, PlatformNames.Standalone},
                    {NamedBuildTarget.Server, PlatformNames.Server},
                    {NamedBuildTarget.Android, PlatformNames.Android},
                    {NamedBuildTarget.iOS, PlatformNames.iOS},
                    {NamedBuildTarget.WebGL, PlatformNames.WebGL},
                    {NamedBuildTarget.WindowsStoreApps, PlatformNames.WindowsStoreApps},
                    {NamedBuildTarget.PS4, PlatformNames.PS4},
                    {NamedBuildTarget.XboxOne, PlatformNames.XboxOne},
                    {NamedBuildTarget.NintendoSwitch, PlatformNames.Switch},
                    {NamedBuildTarget.Stadia, PlatformNames.Stadia},
#if UNITY_2021_3_OR_NEWER
                    {NamedBuildTarget.LinuxHeadlessSimulation, PlatformNames.LinuxHeadlessSimulation},
#else
                    {NamedBuildTarget.LinuxHeadlessSimulation, SettingsPlatforms.LinuxHeadlessSimulation},
#endif
                    {NamedBuildTarget.EmbeddedLinux, PlatformNames.EmbeddedLinux},
                };
            }

            if (buildTargetToPlatforms2.TryGetValue(buildTarget, out var platform))
            {
                return platform;
            }
            return PlatformNames.Default;
        }



        public static VisualElement CreateSettingsWindow(VisualElement parent, string title, bool scroll = true, string helpLink = null, Action<DropdownMenu> onMenu = null)
        {
            VisualElement root = new VisualElement();
            root.AddToClassList("settings-window");
            parent.Add(root);

            StyleSheet style = LoadUSS(SettingsUSSPath);
            root.styleSheets.Add(style);

            VisualElement titleContainer = new VisualElement();
            titleContainer.AddToClassList("settings-window_title-container");
            Label windowTitle = new Label();
            windowTitle.AddToClassList("settings-window_title");
            windowTitle.text = title;
            titleContainer.Add(windowTitle);
            VisualElement space = new VisualElement();
            space.style.flexGrow = 1;
            titleContainer.Add(space);
            VisualElement toolbarContainer = new VisualElement();
            toolbarContainer.AddToClassList("settings-window_toolbar");
            titleContainer.Add(toolbarContainer);

            if (!string.IsNullOrEmpty(helpLink))
            {
                VisualElement helpView = null;
                var helpIcon = EditorGUIUtility.IconContent("d__Help");
                if (helpIcon != null && helpIcon.image)
                {
                    Image helpImg = new Image();
                    helpImg.AddToClassList("settings-window_help");
                    //不支持 Image Tint
                    //helpImg.image = helpIcon.image;
                    helpImg.style.backgroundImage = new StyleBackground(helpIcon.image as Texture2D);
                    toolbarContainer.Add(helpImg);
                    helpView = helpImg;
                }
                else
                {
                    Label helpLabel = new Label();
                    helpLabel.AddToClassList("settings-window_help");
                    helpLabel.text = "?";
                    toolbarContainer.Add(helpLabel);
                    helpView = helpLabel;
                }

                helpView.RegisterCallback<MouseDownEvent>(e =>
                {
                    if (string.IsNullOrEmpty(helpLink))
                        return;
                    //File
                    if (File.Exists(helpLink))
                    {
                        Application.OpenURL(Path.GetFullPath(helpLink));
                    }
                });

            }

            if (onMenu != null)
            {
                var menuIcon = EditorGUIUtility.IconContent("d__Menu");
                Image menuButton = new Image();
                menuButton.AddToClassList("settings-window_menu");
                //menuButton.image = menuIcon.image;
                menuButton.style.backgroundImage = new StyleBackground(menuIcon.image as Texture2D);
                menuButton.AddManipulator(new MenuManipulator(e =>
                {
                    onMenu(e.menu);
                }, MouseButton.LeftMouse));
                toolbarContainer.Add(menuButton);
            }

            root.Add(titleContainer);

            VisualElement windowContent = new VisualElement();
            windowContent.AddToClassList("settings-window_content");
            root.Add(windowContent);

            if (scroll)
            {
                ScrollView scrollView = new ScrollView();
                scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                windowContent.Add(scrollView);
                return scrollView.contentContainer;
            }
            return windowContent;
        }








        static FieldInfo formatListItemCallbackField;
        static FieldInfo formatSelectedValueCallbackField;

        private static void InitFormatListItemCallback()
        {
#if !UNITY_2022_1_OR_NEWER
            if (formatListItemCallbackField == null)
            {
                BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                Type type = typeof(DropdownField);
                formatListItemCallbackField = type.GetField("formatListItemCallback", bindingFlags);
                formatListItemCallbackField = formatListItemCallbackField ?? type.GetField("m_FormatListItemCallback", bindingFlags);
                formatSelectedValueCallbackField = type.GetField("formatSelectedValueCallback", bindingFlags);
                formatSelectedValueCallbackField = formatSelectedValueCallbackField ?? type.GetField("m_FormatSelectedValueCallback", bindingFlags);

            }
#endif
        }

        public static void SetFormatListItemCallback(this DropdownField field, Func<string, string> callback)
        {
#if UNITY_2022_1_OR_NEWER
            field.formatListItemCallback = callback;
#else
            InitFormatListItemCallback();
            if (formatListItemCallbackField != null)
            {
                formatListItemCallbackField.SetValue(field, callback);
            }
#endif
        }

        public static void SetFormatSelectedValueCallback(this DropdownField field, Func<string, string> callback)
        {
#if UNITY_2022_1_OR_NEWER
            field.formatSelectedValueCallback = callback;
#else
            InitFormatListItemCallback();
            if (formatSelectedValueCallbackField != null)
            {
                formatSelectedValueCallbackField.SetValue(field, callback);
            }
#endif
        }

        public static void SetFormatValueCallback(this DropdownField field, Func<string, string> callback)
        {
            SetFormatListItemCallback(field, callback);
            SetFormatSelectedValueCallback(field, callback);
        }


        public static VisualElement CreateMemberSettingField(
            MemberSettings settings,
            Action<IMemberSetting> onSettingAdded = null,
            string label = null,
            Type defaultType = null)
        {
            Action<IList<Type>> loadTypes;
            Action<Type, IList<MemberInfo>> loadMembers;

            loadTypes = (list) =>
            {
                /*
                HashSet<Assembly> excludeAsm = new();
                List<string> includeDir = new()
                    {
                        "ScriptAssemblies",
                        "UnityEngine",
                    };

                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        if (asm.Location == null)
                            continue;
                    }
                    catch
                    {
                        continue;
                    }
                    if (excludeAsm.Contains(asm))
                        continue;
                    if (!includeDir.Any(o => asm.Location.Contains(o)))
                        continue;
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
                            continue;
                        if (type.IsEnum || type.IsAbstract || type.IsInterface)
                            continue;
                        if (!type.IsClass)
                            continue;

                        //if (type.FullName.Contains("<"))
                        //{
                        //    var attrs = type.GetCustomAttributes();
                        //}
                        list.Add(type);
                    }
                }
                */
                foreach (var asm in SettingsViewUtility.GetSettingTypes())
                {
                    list.Add(asm);
                }
            };

            loadMembers = (type, list) =>
            {
                foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.SetField | BindingFlags.SetProperty))
                {
                    if (member.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
                        continue;
                    if (!(member.MemberType == MemberTypes.Property || member.MemberType == MemberTypes.Field))
                        continue;
                    Type valueType = null;
                    if (member.MemberType == MemberTypes.Property)
                    {
                        var pInfo = member as PropertyInfo;
                        if (!pInfo.CanWrite)
                            continue;
                        valueType = pInfo.PropertyType;
                    }
                    else if (member.MemberType == MemberTypes.Field)
                    {
                        var fInfo = member as FieldInfo;
                        if (fInfo.IsInitOnly)
                            continue;
                        valueType = fInfo.FieldType;
                    }
                    if (valueType == null)
                        continue;
                    if (!SettingsViewUtility.HasInputView(valueType))
                        continue;
                    list.Add(member);
                }
            };

            return CreateMemberSettingField(settings, loadTypes: loadTypes, loadMembers: loadMembers, onSettingAdded: onSettingAdded, label: label, defaultType: defaultType);
        }



        public static VisualElement CreateMemberSettingField(
            MemberSettings settings,
            Action<IList<Type>> loadTypes,
            Action<Type, IList<MemberInfo>> loadMembers,
            Action<IMemberSetting> onSettingAdded = null,
            string label = null,
            Type defaultType = null)
        {
            List<Type> allTypes = null;
            Dictionary<Type, List<MemberInfo>> allMembers = new();


            VisualElement container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.flexGrow = 1;

            SearchDropdownField typeField = new SearchDropdownField();
            typeField.label = label;
            typeField.style.flexGrow = 1f;
            typeField.LoadItems = (list) =>
            {
                if (allTypes == null)
                {
                    allTypes = new List<Type>();
                    loadTypes(allTypes);
                    allTypes.Sort((a, b) => a.FullName.CompareTo(b.FullName));
                }

                foreach (var item in allTypes)
                {
                    list.Add(item);
                }
                //Debug.Log("type:" + list.Count);
            };

            typeField.Filer = (o, filter) =>
            {
                var type = o as Type;
                if (!string.IsNullOrEmpty(filter) && !Regex.IsMatch(type.FullName, filter, RegexOptions.IgnoreCase))
                    return false;
                return true;
            };
            typeField.FormatListItemCallback = (o) =>
            {
                if (o != null && o is string)
                {
                    return "Select Settings Type";
                }

                Type type = o as Type;
                return type.FullName;
            };
            typeField.SetValueWithoutNotify(defaultType);
            if (typeField.value == null)
                typeField.SetValueWithoutNotify(string.Empty);
            container.Add(typeField);


            SearchDropdownField memberField = new SearchDropdownField();
            memberField.style.flexGrow = 1f;
            memberField.LoadItems = (list) =>
            {
                Type type = typeField.value as Type;
                if (type == null)
                    return;

                if (!allMembers.TryGetValue(type, out var members))
                {
                    members = new();
                    loadMembers(type, members);
                    members.Sort((a, b) => a.Name.CompareTo(b.Name));
                    allMembers[type] = members;
                }

                foreach (var item in members)
                {
                    if (settings.ContainsMember(item))
                    {
                        continue;
                    }

                    list.Add(item);
                }
                //Debug.Log("member:" + list.Count);
            };

            memberField.Filer = (o, filter) =>
            {
                var member = o as MemberInfo;
                if (settings.ContainsMember(member))
                    return false;
                if (!string.IsNullOrEmpty(filter) && !Regex.IsMatch(member.Name, filter, RegexOptions.IgnoreCase))
                    return false;
                return true;
            };

            memberField.FormatListItemCallback = (o) =>
            {
                if (o is string)
                {
                    return "Add Setting Property";
                }

                MemberInfo member = o as MemberInfo;
                Type valueType = typeof(void);
                if (member is PropertyInfo pInfo)
                {
                    valueType = pInfo.PropertyType;
                }
                else if (member is FieldInfo fInfo)
                {
                    valueType = fInfo.FieldType;
                }
                if (valueType.IsEnum)
                {
                    return $"{member.Name} (Enum)";
                }
                else
                {
                    return $"{member.Name} ({valueType?.Name})";
                }
            };
            //memberField.FormatSelectedValueCallback = memberField.FormatListItemCallback;
            container.Add(memberField);

            typeField.RegisterValueChangedCallback(e =>
            {
                memberField.SetValueWithoutNotify(string.Empty);
            });

            memberField.RegisterValueChangedCallback(e =>
            {
                var mInfo = e.newValue as MemberInfo;
                if (mInfo != null)
                {
                    if (!settings.ContainsMember(mInfo))
                    {
                        memberField.SetValueWithoutNotify(string.Empty);
                        IMemberSetting setting = settings.AddSetting(mInfo);
                        onSettingAdded?.Invoke(setting);
                    }
                }
            });
            memberField.SetValueWithoutNotify(string.Empty);
            return container;
        }

    }




}

