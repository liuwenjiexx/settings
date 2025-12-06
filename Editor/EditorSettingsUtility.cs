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


        const string PlatformSettingsPanel_Header_ClassName = "settings-platform-panel_header";
        const string PlatformSettingsPanel_HeaderActive_ClassName = "settings-platform-panel_header_active";
        const string PlatformSettingsPanel_Header_Group_Prefix = "settings-platform-panel_header_group_";

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
        public static VisualElement CreatePlatformSettingsPanel(
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
            return CreatePlatformSettingsPanel(options);
        }

        public static VisualElement CreatePlatformSettingsPanel(CreateSettingViewOptions options)
        {
            string[] platforms = options.platforms;
            string activePlatform = options.activePlatform;
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

            if (string.IsNullOrEmpty(activePlatform))
            {
                if (platforms.Contains(PlatformNames.Standalone))
                {
                    activePlatform = PlatformNames.Standalone;
                }
                else
                {
                    activePlatform = PlatformNames.Default;
                }
            }

            if (options.platformRoot == null)
            {
                var root = new VisualElement();
                root.AddToClassList("settings-platform-panel");

                if (options.parent != null)
                {
                    options.parent.Add(root);
                }
                options.platformRoot = root;
            }
            CreatePlatformSettingsPanel2(options);
            return options.platformRoot;
        }

        private static void CreatePlatformSettingsPanel2(CreateSettingViewOptions options)
        {
            var root = options.platformRoot;

            root.Clear();

            var headerContainer = new VisualElement();
            headerContainer.AddToClassList("settings-platform-panel_header_container");
            root.Add(headerContainer);

            var contentContainer = new VisualElement();
            contentContainer.AddToClassList("settings-platform-panel_content_container");
            root.Add(contentContainer);



            var contentParent = new VisualElement();
            contentParent.AddToClassList("settings-platform-panel_content");

            //Toggle overrideTgl = new Toggle();
            //overrideTgl.RegisterValueChangedCallback(e =>
            //{
            //    var active = headerContainer.Q(className: PlatformSettingsPanel_HeaderActive_ClassName);
            //    if (active == null || active.userData == null)
            //        return;
            //    var platform = (string)active.userData;

            //    if (platform == PlatformNames.Default)
            //        return;

            //    contentParent.SetEnabled(e.newValue);

            //    onOverride(platform, e.newValue);
            //});
            //contentContainer.Add(overrideTgl);

            contentContainer.Add(contentParent);


            Action<string> showPlatform = (platform) =>
            {
                options.activePlatform = platform;
                options.platformChanged?.Invoke(platform);

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

                //if (platform != PlatformNames.Default)
                //{
                //    overrideTgl.style.display = DisplayStyle.Flex;
                //    overrideTgl.text = $"Override For {PlatformNames.GetDisplayName(platform)}";
                //    if (hasOverride(platform))
                //    {
                //        overrideTgl.SetValueWithoutNotify(true);
                //        contentParent.SetEnabled(true);
                //    }
                //    else
                //    {
                //        overrideTgl.SetValueWithoutNotify(false);
                //        contentParent.SetEnabled(false);
                //    }
                //}
                //else
                //{
                //    contentParent.SetEnabled(true);
                //    overrideTgl.style.display = DisplayStyle.None;
                //}

                options.createPlatformMembers(contentParent, platform);

            };

            foreach (var platform in options.platforms)
            {
                if (!options.showSubplatform && PlatformNames.IsSubplatform(platform))
                    continue;

                VisualElement header = new VisualElement();
                header.AddToClassList(PlatformSettingsPanel_Header_ClassName);

                header.AddToClassList(PlatformSettingsPanel_Header_Group_Prefix + platform);
                header.tooltip = $"{PlatformNames.GetDisplayName(platform)} settings";
                header.userData = platform;

                header.RegisterCallback<MouseDownEvent>(e =>
                {
                    if (e.button == 0)
                    {
                        showPlatform(platform);
                    }
                });

                if (platform == PlatformNames.Default)
                {
                    header.AddManipulator(new MenuManipulator(e =>
                    {
                        //if (options.showSubplatformChanged != null)
                        {
                            e.menu.AppendAction("Show Subplatform",
                                act =>
                                {
                                    options.showSubplatform = !options.showSubplatform;
                                    CreatePlatformSettingsPanel2(options);
                                    options.showSubplatformChanged?.Invoke(options.showSubplatform);
                                },
                                act =>
                                {
                                    return options.showSubplatform ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                                });
                        }
                    }));
                }

                var iconImage = GetPlatformIcon(platform);


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
                    headerText.text = PlatformNames.GetShortDisplayName(platform);
                    header.Add(headerText);
                }

                headerContainer.Add(header);

            }

            if (options.activePlatform != null && options.platforms.Contains(options.activePlatform))
            {
                showPlatform(options.activePlatform);
            }
            else
            {
                showPlatform(options.platforms.First());
            }
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

        public static Texture GetPlatformIcon(string platform)
        {
            switch (platform)
            {
                case PlatformNames.Standalone:
                    return EditorGUIUtility.IconContent("d_BuildSettings.Standalone.Small@2x")?.image;
                case PlatformNames.Android:
                    return EditorGUIUtility.IconContent("d_BuildSettings.Android.Small@2x")?.image;
                case PlatformNames.iOS:
                    return EditorGUIUtility.IconContent("d_BuildSettings.iPhone.Small@2x")?.image;
                case PlatformNames.PS4:
                    return EditorGUIUtility.IconContent("d_BuildSettings.PS4@2x")?.image;
                case PlatformNames.PS5:
                    return EditorGUIUtility.IconContent("d_BuildSettings.PS5@2x")?.image;
                case PlatformNames.WebGL:
                    return EditorGUIUtility.IconContent("d_BuildSettings.WebGL@2x")?.image;
                case PlatformNames.Switch:
                    return EditorGUIUtility.IconContent("d_BuildSettings.Switch@2x")?.image;
                case PlatformNames.tvOS:
                    return EditorGUIUtility.IconContent("d_BuildSettings.tvOS@2x")?.image;
                case PlatformNames.Server:
                    //case PlatformNames.WindowsServer:
                    //case PlatformNames.OSXServer:
                    //case PlatformNames.LinuxServer:
                    return EditorGUIUtility.IconContent("d_BuildSettings.DedicatedServer.Small@2x")?.image;
            }

            return null;
        }

        const string SettingsField_Member_ClassName = "settings-field-{0}";

        static string GetSettingFieldMemberClassName(SettingMetadata member)
        {
            return string.Format(SettingsField_Member_ClassName, member.Name);
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

        public static object GetValue(MemberInfo member, object instance)
        {
            object value = null;
            if (member is FieldInfo fInfo)
            {
                if (fInfo.IsStatic)
                {
                    value = fInfo.GetValue(null);
                }
                else
                {
                    if (instance != null)
                    {
                        value = fInfo.GetValue(instance);
                    }
                }
            }
            else if (member is PropertyInfo pInfo)
            {
                var getter = pInfo.GetGetMethod(true);
                if (getter.IsStatic)
                {
                    value = pInfo.GetValue(null);
                }
                else
                {
                    if (instance != null)
                    {
                        value = pInfo.GetValue(instance);
                    }
                }
            }

            return value;
        }

        private static Dictionary<Type, Type> typeMapViewTypes;
        private static List<InputViewMetadata> inputViewMetadatas;
        class InputViewMetadata
        {
            public Type ValueType;
            public Type ViewType;
            public bool IncludeChildren;
        }

        public static Type GetInputViewType(Type valueType)
        {
            Type viewType = null;
            if (typeMapViewTypes == null)
            {
                typeMapViewTypes = new();
                inputViewMetadatas = new();
                foreach (var type in TypeCache.GetTypesWithAttribute(typeof(CustomInputViewAttribute)))
                {
                    if (!type.IsClass || type.IsAbstract)
                        continue;
                    if (!typeof(InputView).IsAssignableFrom(type))
                    {
                        Debug.LogError($"{nameof(CustomInputViewAttribute)} Type '{type.Name}' not interit '{typeof(InputView).Name}'");
                        continue;
                    }
                    var viewAttr = type.GetCustomAttribute<CustomInputViewAttribute>();
                    var targetType = viewAttr.TargetType;
                    if (targetType == null) continue;
                    InputViewMetadata metadata = new();
                    metadata.ViewType = type;
                    metadata.ValueType = targetType;
                    metadata.IncludeChildren = true;
                    inputViewMetadatas.Add(metadata);

                    if (!metadata.ViewType.IsAbstract)
                    {
                        typeMapViewTypes[targetType] = type;
                    }
                }
            }

            if (typeMapViewTypes.TryGetValue(valueType, out viewType))
                return viewType;

            if (BaseInputView.IsBaseField(valueType))
            {
                viewType = typeof(BaseInputView);
                typeMapViewTypes[valueType] = viewType;
                return viewType;
            }

            if (valueType.IsEnum)
            {
                if (typeMapViewTypes.TryGetValue(typeof(Enum), out viewType))
                {
                    typeMapViewTypes[valueType] = viewType;
                }
                return viewType;
            }

            foreach (var metadata in inputViewMetadatas)
            {
                if (metadata.IncludeChildren && metadata.ValueType.IsAssignableFrom(valueType))
                {
                    viewType = metadata.ViewType;
                    typeMapViewTypes[valueType] = viewType;
                    break;
                }

                if (metadata.ValueType.IsGenericTypeDefinition)
                {
                    if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == metadata.ValueType)
                    {

                        viewType = metadata.ViewType;
                        typeMapViewTypes[valueType] = viewType;
                        break;
                    }
                }
            }

            return null;
        }

        public static InputView CreateInputView(Type valueType)
        {
            Type viewType = GetInputViewType(valueType);
            if (viewType == null)
                return null;
            InputView view = Activator.CreateInstance(viewType) as InputView;
            if (view == null)
                return null;
            view.ValueType = valueType;
            return view;
        }


        public static void CreateSettingView(CreateSettingViewOptions options)
        {

            List<(ISetting setting, SettingMetadata member)> settingMembers = new();
            List<(ISetting setting, SettingMetadata member)> platformSettingMembers = new();

            bool hasMultiPlatform = false;


            foreach (var item in SettingMetadata.GetMembers(options.OwnerSettingsType, options.instance))
            {
                ISetting setting = item.setting;
                var metadata = item.metadata;

                if (metadata.IsHidden.HasValue && metadata.IsHidden.Value)
                {
                    continue;
                }

                if (options.filter != null && !options.filter(setting))
                    continue;

                if (setting.IsMultiPlatform || (metadata.IsMultiPlatform.HasValue && metadata.IsMultiPlatform.Value))
                {
                    hasMultiPlatform = true;
                    platformSettingMembers.Add(item);
                }
                else
                {

                    settingMembers.Add(item);
                }

            }

            string variant = SettingsUtility.Variant;


            CreateSettings(settingMembers, options, options.parent, PlatformNames.Default);

            if (hasMultiPlatform)
            {
                if (options.createPlatformMembers == null)
                {
                    options.createPlatformMembers = (parent, platform) =>
                    {
                        List<(ISetting setting, SettingMetadata member)> tmp = new();
                        foreach (var item in platformSettingMembers)
                        {
                            var member = item.member;

                            if (member.IncludePlatforms != null && member.IncludePlatforms.Length > 0)
                            {
                                if (!member.IncludePlatforms.Contains(platform))
                                    continue;
                            }
                            else if (member.ExcludePlatforms != null && member.ExcludePlatforms.Length > 0)
                            {
                                if (member.ExcludePlatforms.Contains(platform))
                                    continue;
                            }

                            tmp.Add(item);
                        }

                        CreateSettings(tmp, options, parent, platform);

                    };
                }

                if (options.hasOverride == null)
                {
                    options.hasOverride = (platform) =>
                     {
                         Debug.LogError("XXX");
                         foreach (var item in settingMembers)
                         {
                             var member = item.member;
                             if (!(member.IsMultiPlatform.HasValue && member.IsMultiPlatform.Value))
                                 continue;

                             ISetting setting = item.setting;
                             if (setting.Contains(platform, variant))
                                 return true;
                         }

                         return false;
                     };
                }

                if (options.onOverride == null)
                {
                    options.onOverride = (platform, enable) =>
                     {
                         if (platform == PlatformNames.Default)
                             return;
                         Settings settings = null;
                         foreach (var item in settingMembers)
                         {
                             var member = item.member;
                             if (!(member.IsMultiPlatform.HasValue && member.IsMultiPlatform.Value))
                                 continue;

                             ISetting setting = item.setting;
                             if (enable)
                             {
                                 setting.SetValue(platform, variant, setting.GetValue(PlatformNames.Default, variant));
                             }
                             else
                             {
                                 setting.Delete(platform, variant);
                             }
                             settings = setting.Settings;
                         }

                         if (settings != null)
                         {
                             settings.Save();
                         }
                     };
                }

                CreatePlatformSettingsPanel(options);

            }
        }

        static void CreateSettings(List<(ISetting setting, SettingMetadata member)> settingMembers, CreateSettingViewOptions options, VisualElement parent, string platform)
        {
            string variant = SettingsUtility.Variant;

            GroupBox groupBox = null;
            foreach (var item in settingMembers)
            {
                var member = item.member;
                ISetting setting = item.setting;

                if (!string.IsNullOrEmpty(member.GroupTitle))
                {
                    groupBox = new GroupBox();
                    groupBox.AddToClassList("setting-group");
                    groupBox.text = member.GroupTitle;
                    parent.Add(groupBox);
                    //Label headerLabel = new Label();
                    //headerLabel.AddToClassList("setting-group__title");
                    //headerLabel.text=member.GroupTitle;
                    //options.parent.Add(headerLabel);
                }

                if (options.CreateFieldBefore != null)
                {
                    if (!options.CreateFieldBefore(setting))
                        continue;
                }

                var field = member.CreateSettingField(setting, platform);
                field.OnDeleteSetting = options.OnDeleteSetting;
                field.OnMoveSetting = options.OnMoveSetting;
                var settingView = field.View;
                if (field != null)
                {
                    settingView.name = member.Name;
                    settingView.AddToClassList(GetSettingFieldMemberClassName(member));
                    if (groupBox != null)
                    {
                        groupBox.contentContainer.Add(settingView);
                    }
                    else
                    {
                        parent.Add(settingView);
                    }
                    options.CreateFieldAfter?.Invoke(setting, settingView);
                }
                else
                {
                    //VisualElement placeholder = new VisualElement();
                    //placeholder.AddToClassList(GetSettingFieldMemberClassName(member));
                    //placeholder.style.display = DisplayStyle.None;
                    //root.Add(placeholder);
                }
            }
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


        public static bool HasInputView(Type valueType)
        {
            Type viewType = GetInputViewType(valueType);
            if (viewType != null)
                return true;
            if (valueType.IsArray || typeof(IList).IsAssignableFrom(valueType))
            {
                Type itemType;
                Type itemViewType;
                viewType = typeof(ArrayView);
                if (valueType.IsArray)
                {
                    itemType = valueType.GetElementType();
                }
                else
                {
                    itemType = valueType.GetGenericArguments()[0];
                }
                itemViewType = GetInputViewType(itemType);
                if (itemViewType != null)
                    return true;
            }
            return false;
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

        public static Action InitializeSettingFieldLabel(ISetting setting, VisualElement settingLabel, Func<ISetting, bool> hasValue, Action<ISetting> setAsValue, Action<ISetting> unsetValue, Action<ISetting> deleteSetting = null, Action<ISetting, bool> moveSetting = null, Action<ISetting, DropdownMenu> onMenu = null, Func<ISetting, bool> isBoldLabel = null)
        {
            settingLabel.AddManipulator(new MenuManipulator(e =>
            {
                e.menu.AppendAction("Set as value",
                    act =>
                    {
                        setAsValue(setting);
                        UpdateSettingFieldLabel(settingLabel, hasValue(setting));
                    },
                    act =>
                    {
                        if (hasValue(setting))
                        {
                            return DropdownMenuAction.Status.Disabled;
                        }
                        return DropdownMenuAction.Status.Normal;
                    });

                e.menu.AppendAction("Unset",
                    act =>
                    {
                        unsetValue(setting);
                        UpdateSettingFieldLabel(settingLabel, hasValue(setting));
                    },
                    act =>
                    {
                        if (!hasValue(setting))
                        {
                            return DropdownMenuAction.Status.Disabled;
                        }
                        return DropdownMenuAction.Status.Normal;
                    });


                if (moveSetting != null)
                {
                    e.menu.AppendSeparator();
                    e.menu.AppendAction("Move Up",
                        act =>
                        {
                            moveSetting(setting, true);
                        });
                    e.menu.AppendAction("Move Down",
                        act =>
                        {
                            moveSetting(setting, false);
                        });
                }

                if (deleteSetting != null)
                {
                    e.menu.AppendSeparator();
                    e.menu.AppendAction("Delete",
                        act =>
                        {
                            deleteSetting(setting);
                        });
                }
                onMenu?.Invoke(setting, e.menu);
            }));

            //UpdateSettingFieldLabel(settingLabel, isBoldLabel(setting));
            return () => UpdateSettingFieldLabel(settingLabel, isBoldLabel != null ? isBoldLabel(setting) : false);
        }

        public static void UpdateSettingFieldLabel(VisualElement settingLabel, bool isBoldLabel)
        {
            if (isBoldLabel)
            {
                if (!settingLabel.ClassListContains(SettingField.SettingLabelOverride_ClassName))
                {
                    settingLabel.AddToClassList(SettingField.SettingLabelOverride_ClassName);
                }
            }
            else
            {
                if (settingLabel.ClassListContains(SettingField.SettingLabelOverride_ClassName))
                {
                    settingLabel.RemoveFromClassList(SettingField.SettingLabelOverride_ClassName);
                }
            }
        }


        private static List<Type> settingTypes;

        internal static IEnumerable<Assembly> ReferencedAssemblies(Assembly referenced, IEnumerable<Assembly> assemblies)
        {
            string fullName = referenced.FullName;

            foreach (var ass in assemblies)
            {
                if (referenced == ass)
                {
                    yield return ass;
                }
                else
                {
                    foreach (var refAss in ass.GetReferencedAssemblies())
                    {
                        if (fullName == refAss.FullName)
                        {
                            yield return ass;
                            break;
                        }
                    }
                }
            }
        }

        internal static IEnumerable<Assembly> ReferencedAssemblies(Assembly referenced)
        {
            return ReferencedAssemblies(referenced, AppDomain.CurrentDomain.GetAssemblies());
        }

        public static List<Type> GetSettingTypes()
        {
            if (settingTypes == null)
            {
                settingTypes = new List<Type>();

                HashSet<Type> exclude = new HashSet<Type>()
                {
                    typeof(MemberSettings),
                    typeof(Settings),
                };

                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                //foreach (var asm in ReferencedAssemblies(typeof(SettingsAttribute).Assembly))
                {
                    foreach (var attr in asm.GetCustomAttributes<SettingsAttribute>())
                    {
                        var settingsType = attr.SettingsType;
                        if (settingsType != null)
                        {
                            if (!settingTypes.Contains(settingsType))
                            {
                                settingTypes.Add(settingsType);
                            }
                        }
                    }

                    foreach (var type in asm.GetTypes())
                    {
                        if (type.IsEnum || type.IsInterface || !type.IsClass)
                            continue;

                        if (exclude.Contains(type))
                            continue;

                        if (type.IsDefined(typeof(SettingsAttribute), true))
                        {
                            if (!settingTypes.Contains(type))
                            {
                                settingTypes.Add(type);
                            }
                            continue;
                        }

                        if (type.Name.EndsWith("Settings") && !type.IsNested)
                        {
                            if (!settingTypes.Contains(type))
                            {
                                settingTypes.Add(type);
                            }
                            continue;
                        }
                    }
                }
                settingTypes.Sort((a, b) => a.FullName.CompareTo(b.FullName));
            }

            return settingTypes;
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
                foreach (var asm in GetSettingTypes())
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
                    if (!HasInputView(valueType))
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
            field.formatSelectedValueCallback =callback;
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

    }

    public class CreateSettingViewOptions
    {
        public VisualElement parent;
        public VisualElement platformRoot;
        public Type OwnerSettingsType;
        public object instance;
        public Func<ISetting, bool> filter;
        public Action<VisualElement, string> createPlatformMembers;
        public Func<string, bool> hasOverride;
        public Action<string, bool> onOverride;
        public string[] platforms;
        public string activePlatform;
        public bool showSubplatform;
        public Func<string, bool> filterPlatform;
        public Action<bool> showSubplatformChanged;
        public Func<ISetting, bool> CreateFieldBefore;
        public Action<string> platformChanged;
        public Action<ISetting, VisualElement> CreateFieldAfter;
        public Func<ISetting, bool> CanDeleteSetting;
        public Action<ISetting> OnDeleteSetting;
        public Action<ISetting, int, int> OnMoveSetting;

    }


}

