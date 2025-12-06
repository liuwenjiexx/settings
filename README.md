## 特性

| 特性                                                | 支持 | 描述                           |
| --------------------------------------------------- | ---- | ------------------------------ |
| 编辑器                                              | ✔    | 编辑器读取和写入配置           |
| 运行时                                              | ✔    | 运行时读取和写入配置           |
| 多平台                                              | ✔    | 平台: Standalone，Android，iOS |
| 变体                                                | ✔    | 变体: debug, release, demo     |
| 属性成员可增减                                      | ✔    |                                |
| 反射静态属性或字段，动态添加和删除配置成员          | ✔    |                                |
| 定制存储位置                                        | ✔    |                                |
| 扩展属性值类型                                      | ✔    |                                |
| 子平台, 如 Standalone 子平台 Windows，WindowsServer | ✔    |                                |

## 使用

1. 定义包名

```c#
private static string PackageName = "com.test.package";
```

2. 创建 `Settings` 实例

   指定 `PackageSettingRepository` 包存储库, `SettingsScope`: `EditorProject` 为编辑器工程配置中

   位置: `ProjectSettings/Packages/<PackageName>/Settings.json`

```c#
private static Settings settings;

private static Settings Settings
    => settings ??= new Settings(
        new PackageSettingRepository(PackageName, SettingsScope.EditorProject));
```

3. 定义设置属性

    `new Setting<T>(Settings, "设置属性名", 默认值, SettingsScope)`

```c#
private static Setting<string> stringValue = new(Settings, nameof(StringValue), "abc", SettingsScope.EditorProject);

public static string StringValue
{
    get => stringValue.Value;
    set => stringValue.SetValue(value, true);
}
```

4. 定义多平台设置

   设置属性添加 `MultiPlatformAttribute` 特性

```c#
[MultiPlatform]
private static Setting<string> PlatformString { get; set; } 
	= new(Settings, "platform.string", string.Empty, SettingsScope.EditorProject);

public static string GetPlatformString(SettingsPlatform platform)
    => PlatformString.GetValue(platform);

public static void SetPlatformString(SettingsPlatform platform, string value)
    => PlatformString.SetValue(platform, value, true);
```

**完整样例**

```c#
public class MySettings
{
    private static string PackageName = "com.test.package";
    private static Settings settings;

    public static Settings Settings
        => settings ??= new Settings(new PackageSettingRepository(PackageName, SettingsScope.EditorProject));



    private static Setting<string> stringValue = new(Settings, "string", "abc", SettingsScope.EditorProject);
    public static string StringValue
    {
        get => stringValue.Value;
        set => stringValue.SetValue(value, true);
    }


    private static Setting<int> intValue = new(Settings, "int", 0, SettingsScope.EditorProject);
    public static int IntValue
    {
        get => intValue.Value;
        set => intValue.SetValue(value, true);
    }

    private static Setting<float> floatValue = new(Settings, "float", 0f, SettingsScope.EditorProject);
    public static float FloatValue
    {
        get => floatValue.Value;
        set => floatValue.SetValue(value, true);
    }

    private static Setting<bool> boolValue = new(Settings, "bool", false, SettingsScope.EditorProject);
    public static bool BoolValue
    {
        get => boolValue.Value;
        set => boolValue.SetValue(value, true);
    }

    private static Setting<Vector3> vector3Value = new(Settings, "Vector3", new Vector3(1, 2, 3), SettingsScope.EditorProject);
    public static Vector3 Vector3Value
    {
        get => vector3Value.Value;
        set => vector3Value.SetValue(value, true);
    }

    private static Setting<string[]> stringArray = new(Settings, "string.array", null, SettingsScope.EditorProject);
    public static string[] StringArray
    {
        get => stringArray.Value;
        set => stringArray.SetValue(value, true);
    }

    private static Setting<List<string>> stringList = new(Settings, "string.list", null, SettingsScope.EditorProject);
    public static List<string> StringList
    {
        get => stringList.Value;
        set => stringList.SetValue(value, true);
    }


    [HideInInspector]
    private static Setting<string> hideField = new(Settings, "hide.string", string.Empty, SettingsScope.EditorProject);
    public static string HideField
    {
        get => hideField.Value;
        set => hideField.SetValue(value, true);
    }


    [MultiPlatform]
    private static Setting<string> PlatformString { get; set; } = new(Settings, "platform.string", string.Empty, SettingsScope.EditorProject);

    public static string GetPlatformString(SettingsPlatform platform)
        => PlatformString.GetValue(platform);

    public static void SetPlatformString(SettingsPlatform platform, string value)
        => PlatformString.SetValue(platform, value, true);


}
```



## 存储

存储仓库, 实现 `ISettingsRepository` 接口, 从 `ISettingsRepository.Get/Set` 获取或设置数据始终进行序列化, `Get` 引用类型始终为新实例

### 存储数据

| 数据成员 | 描述 |
| -------- | ---- |
| value    | 值   |
| platform | 平台 |

### 存储库

| 存储库                   | Runtime | Project | 外部删除 |
| ------------------------ | ------- | ------- | -------- |
| PackageSettingRepository | ✔       | ✔       | ✘        |
| PlayerPrefsRepository    | ✔       | ✔       | ✔        |
| EditorPrefsRepository    | ✘       | ✘       | ✔        |
| SqliteRepository         | ✔       | ✔       | ✘        |
| EditorSettingsRepository | ✘       | ✘       | ✘        |



**PackageSettingRepository**

`NPM` 格式 `package.json` 模块配置, 以 `PackageName` 包名 `name`  区分

支持域: `RuntimeProject`, `RuntimeUser`, `EditorProject`, `EditorUser`

| Runtime/Editor | Project/User | ClassName           | Git  | Repository                                               |
| -------------- | ------------ | ------------------- | ---- | -------------------------------------------------------- |
| Runtime        | Project      | *Settings           | ✔    | Assets/Resources/ProjectSettings/Packages/<PackageName>  |
| Runtime        | User         | *UserSettings       | ✘    | <PersistentDataPath>/UserSettings/Packages/<PackageName> |
| Editor         | Project      | Editor*Settings     | ✔    | ProjectSettings/Packages/<PackageName>                   |
| Editor         | User         | Editor*UserSettings | ✘    | UserSettings/Packages/<PackageName>                      |



**PlayerPrefsRepository**

基于 `PlayerPrefs` 实现, 可以被用户删除, `Key` 格式: `<platform>::<type.FullName>::<key>`

支持域: `RuntimeUser`

**EditorPrefsRepository**

基于 `EditorPrefs` 实现, `Key` 格式: `<platform>::<type.FullName>::<key>`

支持域: `EditorUser`

**SqliteRepository**

`Sqlite` 数据库实现存储

**EditorSettingsRepository**

编辑器配置, 工程之间共享

**存储位置**

Windows: `%LOCALAPPDATA%/Unity/Editor/EditorSettings/Packages/<PackageName>`



## 平台

`SettingsPlatform` 根据平台区分设置

| 平台       | 描述                             |
| ---------- | -------------------------------- |
| Standalone | `Windows`, `MacOS`, `Linux` 平台 |
| Android    | `Android` 平台                   |
| iOS        | `iOS` 平台                       |
| Server     | `Dedicated Server` 平台          |



## 编辑器

<img src="Documentation~\assets\SettingsUI.PNG" alt="SettingsUI" style="zoom:80%;" />



**支持特性**

| 特性                   | 描述                |
| ---------------------- | ------------------- |
| MultiPlatformAttribute | UI 显示为多平台设置 |
| HideInInspector        | 不在编辑器显示      |

### 设置面板

继承 `SettingsProvider`, `EditorSettingsUtility.CreateSettingView` 根据 `ISetting` 类型字段或属性生成编辑器UI, className: `settings-field-<member.Name>`

```c#
public class MySettingsProvider : SettingsProvider
{
    private const string SettingsPath = "My Settings";

    public MySettingsProvider()
        : base(SettingsPath, UnityEditor.SettingsScope.Project)
        {
        }

    [SettingsProvider]
    public static SettingsProvider CreateSettingsProvider()
    {
        var provider = new MySettingsProvider();
        provider.keywords = new string[] { "settings", "setting", "config" };
        return provider;
    }

    public override void OnActivate(string searchContext, VisualElement rootElement)
    {
        rootElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorSettingsUtility.SettingsUSSPath));
        EditorSettingsUtility.CreateSettingView(rootElement, typeof(MySettings));
    }

}
```



### 定制类型

```c#
    [Serializable]
    public class CustomType
    {
        public int i;
        public string s;
    }
```

类型必须添加 `SerializableAttribute` 支持序列化

### 定制编辑器

继承 `InputView` , `CustomSettingViewAttribute` 声明定制的数据类型

```c#
[CustomInputView(typeof(string))]
class MyStringField : InputView
{
    public override VisualElement CreateView()
    {
        TextField input= new TextField();
        input.isDelayed = true;
        input.RegisterValueChangedCallback(e =>
			{
                OnValueChanged(e.newValue);
            });
        return input;
    }
    
    public override void SetValue(object value)
    {
        input.SetValueWithoutNotify((string)value);
    }
}
```



## 多存储库

指定存储库 `Repository.Name` 实现多存储位置，默认为 `Settings`

```c#
class ExampleWindow : EditorWindow
{
	private static Settings settings;
    
	//存储为 ExampleWindowSettings.json
	public static Settings Settings
    	=> settings ??= new Settings(new PackageSettingRepository("<PackageName>", SettingsScope.EditorUser, $"{nameof(ExampleWindow)}Settings"));
    
	private static Setting<string> stringValue = new(Settings, nameof(StringValue), null, SettingsScope.EditorUser);

    public static string StringValue
    {
        get => stringValue.Value;
        set => stringValue.SetValue(value, true);
    }
}
```



## 变体

变体默认为空，显示名称为 release，常见变体 debug, demo, demo-debug

工程设置 `Project Settings/Tool/Settings` 配置变体

**切换变体**

```
SettingsUtility.SetVariant(variant);
```

**保存变体**

```
SettingSettings.Variant = variant;
```

**样例**

| 变体       | 优先级                      |          |
| ---------- | --------------------------- | -------- |
| release    |                             | 默认变体 |
| debug      | debug > release             |          |
| demo       | demo > release              |          |
| demo-debug | demo-debug > demo > release |          |

**Windows debug 加载样例**

| Platform   | Variant |
| ---------- | ------- |
| Windows    | debug   |
| Standalone | debug   |
| Default    | debug   |
| Windows    | release |
| Standalone | release |
| Default    | release |

从上到下顺序依次检查是否存在配置值，返回该配置值，如果不存在则返回默认值