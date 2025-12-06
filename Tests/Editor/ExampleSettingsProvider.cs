using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SettingsManagement.Editor
{
    public class ExampleSettingsProvider : SettingsProvider
    {
        const string SettingsPath = "Example/Settings";

        public ExampleSettingsProvider()
          : base(SettingsPath, UnityEditor.SettingsScope.Project)
        {
        }


        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new ExampleSettingsProvider();
            provider.keywords = new string[] { "example" };
            return provider;
        }
        bool isSetVariant;
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            var root = EditorSettingsUtility.CreateSettingsWindow(rootElement, "Example Settings");
            CreateUI(root);
        }

        void CreateUI(VisualElement root)
        {

            DropdownField variantListField = new();
            variantListField.label = "Variant";
            variantListField.SetFormatValueCallback(o =>
            {
                if (string.IsNullOrEmpty(o))
                    return SettingSettings.DefaultVariantName;
                return o;
            });
            isSetVariant = true;
            variantListField.choices.Clear();
            variantListField.choices.Add(string.Empty);
            variantListField.choices.AddRange(SettingSettings.Variants.Select(o => o.variant));
            variantListField.RegisterValueChangedCallback(e =>
            {
                string variant = e.newValue;
                if (variant == SettingSettings.DefaultVariantName)
                    variant = null;
                SettingSettings.Variant = variant;
                SettingsUtility.SetVariant(variant);
            });

            variantListField.SetValueWithoutNotify(SettingsUtility.Variant);

            root.Add(variantListField);

            CreateSettingViewOptions viewOptions = new CreateSettingViewOptions()
            {
                parent = root,
                OwnerSettingsType = typeof(ExampleSettings),
                CanDeleteSetting = (setting) =>
                {
                    if (ExampleSettings.memberSettings.SettingList.Contains(setting))
                        return true;
                    return false;
                },
                OnDeleteSetting = (setting) =>
                {
                    Debug.Log($"Delete Setting: {setting.Key}");
                }
            };

            EditorSettingsUtility.CreateSettingView(viewOptions);


            var memberSettingField = EditorSettingsUtility.CreateMemberSettingField(
                ExampleSettings.memberSettings,
                label: "New Setting",
                defaultType: typeof(PlayerSettings),
                onSettingAdded: (setting) =>
                {
                    root.Clear();
                    CreateUI(root);
                });
            root.Add(memberSettingField);
        }


    }
}