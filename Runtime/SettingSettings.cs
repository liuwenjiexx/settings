using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.Serialization;
using UnityEngine;

namespace SettingsManagement
{
    public class SettingSettings
    {
        static Settings settings;


        static Settings Settings
        {
            get
            {
                if (settings == null)
                {
                    settings = new Settings(
                        new PackageSettingRepository(SettingsUtility.GetPackageName(typeof(SettingSettings)), SettingsScope.RuntimeProject)
#if UNITY_EDITOR
                        , new PackageSettingRepository(SettingsUtility.GetPackageName(typeof(SettingSettings)), SettingsScope.EditorUser)
#endif
                        );
                }
                return settings;
            }
        }

        public static string DebugVariantName = "debug";

        [HideInInspector]
        private static Setting<string> defaultVariantName = new(Settings, nameof(DefaultVariantName), "release", SettingsScope.RuntimeProject);

        public static string DefaultVariantName
        {
            get
            {
                return defaultVariantName.GetValue(PlatformNames.Default, null);
            }
            set
            {
                defaultVariantName.SetValue(PlatformNames.Default, null, value, true);
            }
        }

        [HideInInspector]
        private static Setting<VariantConfig[]> variants = new(Settings, "Variants", new VariantConfig[] { new VariantConfig(DebugVariantName) }, SettingsScope.RuntimeProject);

        public static VariantConfig[] Variants
        {
            get
            {
                return variants.GetValue(PlatformNames.Default, null) ?? new VariantConfig[0];
            }
            set
            {
                variants.SetValue(PlatformNames.Default, null, value, true);
            }
        }

        [HideInInspector]
        internal static Setting<string> variant = new(Settings, "Variant", null, SettingsScope.RuntimeProject);

        public static string Variant
        {
            get
            {
                string value = variant.GetValue(PlatformNames.Default, null);
                if (string.IsNullOrEmpty(value))
                    value = null;
                if (value == DefaultVariantName)
                    value = null;
                return value;
            }
            set
            {
                if (value == DefaultVariantName)
                    value = null;
                if (variant.SetValueIfChange(PlatformNames.Default, null, value, true))
                {
                    Settings.SetVariant(Variant);
                }
            }
        }


        //#if UNITY_EDITOR

        //        private static Setting<string> editorVariant = new(Settings, nameof(EditorVariant), null, SettingsScope.EditorUser);

        //        public static string EditorVariant
        //        {
        //            get
        //            {
        //                if (editorVariant.Value == string.Empty)
        //                    return null;
        //                return editorVariant.Value;
        //            }
        //            set
        //            {
        //                if (variant.Value != value)
        //                {
        //                    variant.SetValue(value, true);
        //                }
        //            }
        //        }
        //#endif


        [Tooltip("调试环境变量")]
        [InspectorName("Debug Environment Variable")]
        private static Setting<SerializableDictionary<string, string>> userEnvironmentVariables = new(Settings, "DebugEnvironmentVariables", null, SettingsScope.EditorUser);

        public static Dictionary<string, string> UserEnvironmentVariables
        {
            get
            {
                if (!Application.isEditor)
                    return null;
                var value = userEnvironmentVariables.Value;
                if (value == null)
                {
                    value = new();
                    userEnvironmentVariables.SetValue(value, true);
                }
                return value.Dictionary;
            } 
        }

        public static void DiryUserEnvironmentVariables()
        {
            if (!Application.isEditor)
                throw new InvalidOperationException();
            userEnvironmentVariables.SetDiry();
        }



        [Serializable]
        class NamedValue
        {
            [SerializeField]
            public string name;
            [SerializeField]
            public string value;
        }


        [Tooltip("调试命令行参数")]
        [InspectorName("Debug Command Line Argument")]
        private static Setting<List<string>> userCommandLineArgs = new(Settings, "DebugCommandLineArgs", null, SettingsScope.EditorUser);

        public static List<string> UserCommandLineArgs
        {
            get
            {
                if (!Application.isEditor)
                    return null;
                var value = userCommandLineArgs.Value;
                if (value == null)
                {
                    value = new();
                    userCommandLineArgs.SetValue(value, true);
                }
                return value;
            }
            set
            {
                if (!Application.isEditor)
                    throw new InvalidOperationException();
                userCommandLineArgs.SetValue(value, true);
            }
        }



    }

    [Serializable]
    public class VariantConfig
    {
        public string variant;
        public string baseVariant;

        public VariantConfig() { }

        public VariantConfig(string variant, string baseVariant = null)
        {
            this.variant = variant;
            this.baseVariant = baseVariant;
        }
    }


}
