using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SettingsManagement.UIElements
{
    public static class SettingsViewUtility
    {
        private static List<Type> settingTypes;

        const string SettingsField_Member_ClassName = "settings-field-{0}";
        const string PlatformSettingsPanel_Header_ClassName = "settings-platform-panel_header";
        const string PlatformSettingsPanel_HeaderActive_ClassName = "settings-platform-panel_header_active";
        const string PlatformSettingsPanel_Header_Group_Prefix = "settings-platform-panel_header_group_";


        private static Dictionary<Type, Type> typeMapViewTypes;
        private static List<InputViewMetadata> inputViewMetadatas;
        class InputViewMetadata
        {
            public Type ValueType;
            public Type ViewType;
            public bool IncludeChildren;
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



        public static Type GetInputViewType(Type valueType)
        {
            Type viewType = null;
            if (typeMapViewTypes == null)
            {
                typeMapViewTypes = new();
                inputViewMetadatas = new();
                foreach (var type in SettingsUtility.GetTypesWithAttribute(typeof(CustomInputViewAttribute)))
                {
                    if (!type.IsClass || type.IsAbstract)
                        continue;
                    if (!typeof(InputView).IsAssignableFrom(type))
                    {
                        Debug.LogError($"{nameof(CustomInputViewAttribute)} Type '{type.Name}' not interit '{typeof(InputView).Name}'");
                        continue;
                    }
                    foreach (var viewAttr in type.GetCustomAttributes<CustomInputViewAttribute>())
                    {
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
            }

            if (typeMapViewTypes.TryGetValue(valueType, out viewType))
                return viewType;
            /*
            if (BaseInputView.IsBaseField(valueType))
            {
                viewType = typeof(BaseInputView);
                typeMapViewTypes[valueType] = viewType;
                return viewType;
            }
            */
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

            string variant = Settings.Variant;


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
            string variant = Settings.Variant;

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


        static string GetSettingFieldMemberClassName(SettingMetadata member)
        {
            return string.Format(SettingsField_Member_ClassName, member.Name);
        }

        public static VisualElement CreatePlatformSettingsPanel(CreateSettingViewOptions options)
        {
            string[] platforms = options.platforms;
            string activePlatform = options.activePlatform;


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

                Texture iconImage = null;

#if UNITY_EDITOR
                iconImage = GetPlatformIcon(platform);
#endif

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

#if UNITY_EDITOR
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

#endif

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