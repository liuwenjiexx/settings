using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SettingsManagement.UIElements
{
    internal class SettingMetadata
    {
        public string Name;
        public string DisplayName;
        public string Tooltip;
        public Type SettingType;
        public Type ValueType;
        public MemberInfo Member;
        public bool? IsHidden;
        public bool? IsMultiPlatform;
        public string GroupTitle;
        public string[] IncludePlatforms;
        public string[] ExcludePlatforms;

        public InputFieldAttribue FieldAttribute;
        public Type ViewType;
        public InputFieldAttribue ElementAttribute;
        public Type ElementViewType;
        public MemberSettings OwnerSettings;

        private static Dictionary<Type, List<SettingMetadata>> typeToMembers;


        public ISetting GetSetting(object instance = null)
        {
            ISetting setting = null;
            if (Member.MemberType == MemberTypes.Field)
            {
                var fInfo = (FieldInfo)Member;
                if (fInfo.IsStatic)
                {
                    setting = fInfo.GetValue(null) as ISetting;
                }
                else
                {
                    if (instance != null)
                    {
                        setting = fInfo.GetValue(instance) as ISetting;
                    }
                }
            }
            else
            {
                var pInfo = (PropertyInfo)Member;
                if (pInfo.GetGetMethod(true).IsStatic)
                {
                    setting = pInfo.GetValue(null) as ISetting;
                }
                else
                {
                    if (instance != null)
                    {
                        setting = pInfo.GetValue(instance) as ISetting;
                    }
                }
            }

            return setting;
        }

        public SettingField CreateSettingField(ISetting setting, SettingsPlatform platform)
        {
            return CreateSettingField(setting, platform.ToString());
        }

        public SettingField CreateSettingField(ISetting setting, string platform)
        {
            SettingField field = new SettingField(setting);
            field.SettingMember = this;
            field.Member = Member;
            field.DisplayName = DisplayName;
            field.Tooltip = Tooltip;
            field.Platform = platform;
            if (setting.IsMultiPlatform || (IsMultiPlatform.HasValue && IsMultiPlatform.Value))
                field.IsMultiPlatform = true;
            if (OwnerSettings != null)
            {
                field.OwnerSettings = OwnerSettings;
                field.CanDelete = true;
            }
            field.CreateView();
            return field;
        }


        internal static List<SettingMetadata> GetMembers(Type settingOwnerType)
        {
            if (typeToMembers == null)
            {
                typeToMembers = new();
            }

            if (!typeToMembers.TryGetValue(settingOwnerType, out var members))
            {
                members = new();
                foreach (var mInfo in settingOwnerType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                    .Select(o => (MemberInfo)o)
                    .Concat(settingOwnerType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).Select(o => (MemberInfo)o)))
                {
                    if (mInfo.IsDefined(typeof(CompilerGeneratedAttribute)))
                        continue;

                    //if (mInfo.IsDefined(typeof(HideInInspector)))
                    //    continue;
                    Type settingType;

                    if (mInfo.MemberType == MemberTypes.Field)
                    {
                        FieldInfo fInfo = (FieldInfo)mInfo;
                        settingType = fInfo.FieldType;
                    }
                    else
                    {
                        PropertyInfo pInfo = (PropertyInfo)mInfo;
                        settingType = pInfo.PropertyType;
                    }

                    //if(field.IsDefined(com)


                    SettingMetadata member = new SettingMetadata();
                    member.Member = mInfo;
                    member.SettingType = settingType;

                    if (!(typeof(ISetting).IsAssignableFrom(settingType) || settingType == typeof(MemberSettings)))
                        continue;

                    if (mInfo.IsDefined(typeof(MultiPlatformAttribute)))
                    {
                        var multiPlatformAttr = mInfo.GetCustomAttribute<MultiPlatformAttribute>();

                        member.IsMultiPlatform = true;
                        member.IncludePlatforms = multiPlatformAttr.IncludePlatforms;
                        member.ExcludePlatforms = multiPlatformAttr.ExcludePlatforms;
                    }

                    if (mInfo.IsDefined(typeof(HideInInspector)))
                    {
                        member.IsHidden = true;
                    }

                    var headerAttr = mInfo.GetCustomAttribute<HeaderAttribute>();
                    if (headerAttr != null)
                    {
                        member.GroupTitle = headerAttr.header;
                    }

                    if (settingType == typeof(MemberSettings))
                    {
                        members.Add(member);
                        continue;
                    }


                    if (settingType.GetGenericTypeDefinition() == typeof(Setting<>))
                    {
                        member.ValueType = settingType.GetGenericArguments()[0];
                    }
                    member.Name = mInfo.Name;

                    var nameAttr = mInfo.GetCustomAttribute<InspectorNameAttribute>();
                    if (nameAttr != null)
                    {
                        member.DisplayName = nameAttr.displayName;
                    }
                    if (string.IsNullOrEmpty(member.DisplayName))
                    {
                        member.DisplayName = member.Name;
#if UNITY_EDITOR
                        member.DisplayName = ObjectNames.NicifyVariableName(member.Name);
#endif
                    }

                    //member.ViewType = CustomSettingViewAttribute.GetViewType(valueType);


                    foreach (var inputAttr in mInfo.GetCustomAttributes<InputFieldAttribue>())
                    {
                        if (inputAttr.IsElement)
                        {
                            member.ElementAttribute = inputAttr;
                        }
                        else
                        {
                            member.FieldAttribute = inputAttr;
                        }
                        Type viewType = SettingsViewUtility.GetInputViewType(inputAttr.GetType());
                        if (viewType != null)
                        {
                            if (inputAttr.IsElement)
                            {
                                member.ElementViewType = viewType;
                            }
                            else
                            {
                                member.ViewType = viewType;
                            }
                        }
                    }

                    if (member.ViewType == null)
                    {
                        member.ViewType = SettingsViewUtility.GetInputViewType(settingType);
                    }


                    members.Add(member);

                }
            }
            return members;
        }

        public static List<(ISetting setting, SettingMetadata metadata)> GetMembers(Type settingsOwnerType, object instance)
        {
            List<(ISetting, SettingMetadata)> members = new();


            foreach (var ownerMetadata in GetMembers(settingsOwnerType))
            {
                if (ownerMetadata.SettingType == typeof(MemberSettings))
                {
                    var memberSettings = SettingsViewUtility.GetValue(ownerMetadata.Member, instance) as MemberSettings;
                    foreach (var setting in memberSettings.SettingList)
                    {
                        var member = setting.TargetMember;
                        if (member == null)
                            continue;
                        Type settingType = setting.GetType();
                        SettingMetadata metadata = new SettingMetadata();
                        metadata.OwnerSettings = memberSettings;
                        metadata.SettingType = settingType;
                        metadata.DisplayName = member.Name;
#if UNITY_EDITOR
                        metadata.DisplayName = $"{ObjectNames.NicifyVariableName(metadata.DisplayName)}";
#endif
                        metadata.Tooltip = $"{member.DeclaringType.FullName}: {member.Name}";
                        if ((ownerMetadata.IsMultiPlatform.HasValue && ownerMetadata.IsMultiPlatform.Value) || member.IsDefined(typeof(MultiPlatformAttribute), true))
                        {
                            metadata.IsMultiPlatform = true;
                        }
                        var pInfo = member as PropertyInfo;
                        if (pInfo != null)
                        {
                            metadata.ValueType = pInfo.PropertyType;
                        }
                        else
                        {
                            var fInfo = member as FieldInfo;
                            metadata.ValueType = fInfo.FieldType;
                        }
                        metadata.ViewType = SettingsViewUtility.GetInputViewType(settingType);
                        metadata.Name = $"{member.DeclaringType.FullName}{member.Name}";

                     

                        members.Add((setting, metadata));
                    }
                    continue;
                }
                else
                {

                    ISetting setting = null;

                    setting = ownerMetadata.GetSetting(instance);

                    if (setting == null)
                        continue;
                    members.Add((setting, ownerMetadata));
                }
            }

            return members;
        }


        public override string ToString()
        {
            return $"{DisplayName} ({SettingType})";
        }
    }

}
