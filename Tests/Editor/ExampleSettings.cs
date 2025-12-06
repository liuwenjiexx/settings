using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Build;
using UnityEngine;
using Unity;
using SettingsManagement;
using SettingsManagement.Editor;
using static UnityEditor.Progress;
using SettingsManagement.Examples;

/// <summary>
/// 设置
/// </summary>
public class ExampleSettings
{
    static Settings settings;

    public static Settings Settings
        => settings ??= new Settings(new PackageSettingRepository(SettingsUtility.GetPackageName(typeof(Settings)), SettingsScope.EditorUser, "ExampleSettings"));


    public const string debugVariant = "debug";
    public const string demoVariant = "demo";
    public const string demoDebugVariant = "demo-debug";


    private static Setting<string> stringValue = new(Settings, "string", "abc", SettingsScope.EditorUser);
    public static string StringValue
    {
        get => stringValue.Value;
        set => stringValue.SetValue(value, true);
    }

    private static Setting<string> variantString = new(Settings, "string.variant", "variant string", SettingsScope.EditorUser);
    public static string VariantString
    {
        get => variantString.Value;
        set => variantString.SetValue(value, true);
    }

    private static Setting<int> intValue = new(Settings, "int", 0, SettingsScope.EditorUser);
    public static int IntValue
    {
        get => intValue.Value;
        set => intValue.SetValue(value, true);
    }

    private static Setting<float> floatValue = new(Settings, "float", 0f, SettingsScope.EditorUser);
    public static float FloatValue
    {
        get => floatValue.Value;
        set => floatValue.SetValue(value, true);
    }

    private static Setting<bool> boolValue = new(Settings, "bool", false, SettingsScope.EditorUser);
    public static bool BoolValue
    {
        get => boolValue.Value;
        set => boolValue.SetValue(value, true);
    }

    private static Setting<Vector3> vector3Value = new(Settings, "Vector3", new Vector3(1, 2, 3), SettingsScope.EditorUser);
    public static Vector3 Vector3Value
    {
        get => vector3Value.Value;
        set => vector3Value.SetValue(value, true);
    }

    private static Setting<Color> colorValue = new(Settings, "Color", Color.white, SettingsScope.EditorUser);
    public static Color ColorValue
    {
        get => colorValue.Value;
        set => colorValue.SetValue(value, true);
    }
    private static Setting<Rect> rectValue = new(Settings, "Rect", new Rect(1, 2, 3, 4), SettingsScope.EditorUser);
    public static Rect RectValue
    {
        get => rectValue.Value;
        set => rectValue.SetValue(value, true);
    }


    private static Setting<AssetPath> objectValue = new(Settings, "Object", default, SettingsScope.EditorUser);
    public static AssetPath ObjectValue
    {
        get => objectValue.Value;
        set => objectValue.SetValue(value, true);
    }


    private static Setting<string[]> stringArray = new(Settings, "string.array", null, SettingsScope.EditorUser);
    public static string[] StringArray
    {
        get => stringArray.Value;
        set => stringArray.SetValue(value, true);
    }

    private static Setting<List<string>> stringList = new(Settings, "string.list", null, SettingsScope.EditorUser);
    public static List<string> StringList
    {
        get => stringList.Value;
        set => stringList.SetValue(value, true);
    }


    [HideInInspector]
    private static Setting<string> hideField = new(Settings, "hide.string", string.Empty, SettingsScope.EditorUser);
    public static string HideField
    {
        get => hideField.Value;
        set => hideField.SetValue(value, true);
    }

    #region Custom Type

    private static Setting<CustomType> customType = new(Settings, "custom-type", new CustomType() { i = 1, s = "abc" }, SettingsScope.EditorUser);
    public static CustomType CustomType
    {
        get => customType.Value;
        set => customType.SetValue(value, true);
    }
    private static Setting<List<CustomType>> customTypeList = new(Settings, "custom-type.list", new List<CustomType>()
    {
        new CustomType(){ i=1, s="abc"},
        new CustomType(){ i=2, s="def"}
    }, SettingsScope.EditorUser);
    public static List<CustomType> CustomTypeList
    {
        get => customTypeList.Value;
        set => customTypeList.SetValue(value, true);
    }

    #endregion

    [MultiPlatform]
    private static Setting<string> platformString = new(Settings, "platform.string", string.Empty, SettingsScope.EditorUser);

    public static string GetPlatformString(string platform)
        => platformString.GetValue(platform, SettingsUtility.Variant);

    public static void SetPlatformString(string platform, string value)
        => platformString.SetValue(platform, SettingsUtility.Variant, value, true);


    [MultiPlatform]
    private static Setting<List<string>> PlatformArrayString = new(Settings, "platform.array-string", new(), SettingsScope.EditorUser);


    [MultiPlatform]
    private static Setting<string> variantPlatform = new(Settings, "platform.string.variant", string.Empty, SettingsScope.EditorUser);


    [MultiPlatform]
    public static MemberSettings memberSettings = new(Settings, "memberSettings", SettingsScope.EditorUser);


}


