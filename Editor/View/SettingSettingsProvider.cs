using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SettingsManagement.Editor
{
    class SettingSettingsProvider : UnityEditor.SettingsProvider
    {
        const string SettingsPath = "Tool/Settings";

        private DropdownField variantField;
        private ListView variantListView;
        private Label variantPriorityField;
        private ListView envVariableListView;

        public SettingSettingsProvider()
          : base(SettingsPath, UnityEditor.SettingsScope.Project)
        {
        }


        [UnityEditor.SettingsProvider]
        public static UnityEditor.SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingSettingsProvider();
            provider.keywords = new string[] { "setting", "settings", "config" };
            return provider;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            string helpFile = Path.GetFullPath(Path.Combine(EditorSettingsUtility.GetPackageDirectory(SettingsUtility.GetPackageName(typeof(SettingSettingsProvider))), "README.md"));

            var root = EditorSettingsUtility.CreateSettingsWindow(rootElement, "Settings", helpLink: helpFile);
            root = EditorSettingsUtility.LoadUXML(root, EditorSettingsUtility.GetEditorUXMLPath(SettingsUtility.GetPackageName(typeof(SettingSettingsProvider)), nameof(SettingSettingsProvider)));

            SettingsUtility.VariantChanged += OnVariantChanged;

            variantField = root.Q<DropdownField>("variant");
            variantPriorityField = root.Q("variant-priority").Q<Label>(className: "unity-base-field__input");
            variantListView = root.Q<ListView>("variant-list");


            variantField.RegisterValueChangedCallback(e =>
            {
                if (isSetValue)
                {
                    return;
                }

                string variant = SettingsUtility.DisplayToVariant(e.newValue);
                SettingSettings.Variant = variant;
                SettingsUtility.SetVariant(variant);
            });



            variantListView.makeItem = () =>
            {
                VisualElement container = new VisualElement();
                container.AddToClassList("variant-list-item");

                {
                    VisualElement template = new VisualElement();
                    template.AddToClassList("variant-list-tpl");
                    Label variantLabel = new Label();
                    variantLabel.AddToClassList("variant-list-item_name");
                    template.Add(variantLabel);

                    Label baseLabel = new Label();
                    baseLabel.AddToClassList("variant-list-item_base");
                    template.Add(baseLabel);

                    VisualElement action = new VisualElement();
                    action.AddToClassList("variant-list-item_action");
                    template.Add(action);

                    var deleteButton = new Button();

                    deleteButton.text = "-";
                    action.Add(deleteButton);

                    deleteButton.clicked += () =>
                    {
                        var variantConfig = container.userData as VariantConfig;

                        if (string.IsNullOrEmpty(variantConfig.variant))
                            return;


                        variantConfig.variant = variantConfig.variant?.Trim();
                        if (string.IsNullOrEmpty(variantConfig.variant))
                            return;
                        var variants = SettingSettings.Variants;

                        var depend = variants.FirstOrDefault(o => o.baseVariant == variantConfig.variant);
                        if (depend != null)
                        {
                            Debug.LogError($"Can't remove Setting Variant '{variantConfig.variant}' depend on '{depend.variant}'");
                            return;
                        }

                        ArrayUtility.Remove(ref variants, variantConfig);
                        SettingSettings.Variants = variants;

                        if (SettingSettings.Variant == variantConfig.variant)
                        {
                            SettingSettings.Variant = null;
                        }
                        if (SettingsUtility.Variant == variantConfig.variant)
                        {
                            SettingsUtility.SetVariant(null);
                        }

                        OnVariantChanged();
                    };

                    container.Add(template);
                }

                {
                    VisualElement newTemplate = new VisualElement();
                    newTemplate.AddToClassList("variant-list-new-tpl");

                    TextField nameField = new TextField();
                    nameField.AddToClassList("variant-list-item_name");
                    newTemplate.Add(nameField);
                    nameField.RegisterValueChangedCallback(e =>
                    {
                        var variantConfig = container.userData as VariantConfig;
                        variantConfig.variant = e.newValue;
                    });


                    DropdownField baseVariantField = new DropdownField();
                    baseVariantField.AddToClassList("variant-list-item_base");
                    newTemplate.Add(baseVariantField);
                    baseVariantField.SetFormatListItemCallback(FormatDisplayVariant);
                    baseVariantField.SetFormatSelectedValueCallback(FormatDisplayVariant);
                    baseVariantField.RegisterValueChangedCallback(e =>
                    {
                        var variantConfig = container.userData as VariantConfig;
                        if (e.newValue == SettingSettings.DefaultVariantName)
                        {
                            variantConfig.baseVariant = null;
                        }
                        else
                        {
                            variantConfig.baseVariant = e.newValue;
                        }

                    });

                    VisualElement action = new VisualElement();
                    action.AddToClassList("variant-list-item_action");
                    newTemplate.Add(action);
                    var addButton = new Button();
                    addButton.text = "+";
                    action.Add(addButton);

                    addButton.clicked += () =>
                    {
                        var variantConfig = container.userData as VariantConfig;

                        variantConfig.variant = variantConfig.variant?.Trim();
                        if (string.IsNullOrEmpty(variantConfig.variant))
                            return;
                        var variants = SettingSettings.Variants;
                        if (SettingSettings.Variants.Any(o => o.variant == variantConfig.variant))
                            return;
                        ArrayUtility.Add(ref variants, variantConfig);
                        variants = variants.OrderBy(o => o.variant).ToArray();
                        SettingSettings.Variants = variants;
                        newVariant = null;
                        OnVariantChanged();
                    };

                    container.Add(newTemplate);
                }
                return container;
            };

            variantListView.bindItem = (view, index) =>
            {
                var variantConfig = variantListView.itemsSource[index] as VariantConfig;
                view.userData = variantConfig;
                var tpl = view.Q(className: "variant-list-tpl");
                var newTpl = view.Q(className: "variant-list-new-tpl");

                if (index == variantListView.itemsSource.Count - 1)
                {
                    tpl.style.display = DisplayStyle.None;
                    newTpl.style.display = DisplayStyle.Flex;

                    var variantField = newTpl.Q<TextField>(className: "variant-list-item_name");
                    variantField.SetValueWithoutNotify(variantConfig.variant);

                    var baseVariantField = newTpl.Q<DropdownField>(className: "variant-list-item_base");
                    baseVariantField.choices.Clear();
                    baseVariantField.choices.Add(string.Empty);
                    baseVariantField.choices.AddRange(SettingSettings.Variants.Select(o => o.variant));
                    baseVariantField.SetValueWithoutNotify(variantConfig.baseVariant);


                }
                else
                {
                    tpl.style.display = DisplayStyle.Flex;
                    newTpl.style.display = DisplayStyle.None;

                    var variantField = tpl.Q<Label>(className: "variant-list-item_name");
                    variantField.text = variantConfig.variant;

                    var baseVariantField = tpl.Q<Label>(className: "variant-list-item_base");
                    baseVariantField.text = FormatDisplayVariant(variantConfig.baseVariant);

                    if (!string.IsNullOrEmpty(variantConfig.baseVariant) && !SettingSettings.Variants.Any(o => o.variant == variantConfig.baseVariant))
                    {
                        baseVariantField.AddToClassList("variant-list-item_base-error");
                    }
                    else
                    {
                        baseVariantField.RemoveFromClassList("variant-list-item_base-error");
                    }
                }
            };


            envVariableListView = root.Q<ListView>("env-variable-list");
            envVariableListView.AddToClassList("dictionary-view");

            envVariableListView.makeItem = () =>
            {
                VisualElement container = new VisualElement();
                container.AddToClassList("dictionary-view-item");

                Image menu = new Image();
                menu.AddToClassList("dictionary-view-item_menu");
                menu.image = EditorGUIUtility.IconContent("d_PauseButton")?.image;
                menu.AddManipulator(new MenuManipulator(e =>
                {
                    e.menu.AppendAction("Copy",
                       e =>
                       {
                           var item = (KeyValuePair<string, string>)container.userData;
                           EditorGUIUtility.systemCopyBuffer = $"{item.Key}={item.Value}";
                       });

                    e.menu.AppendAction("Copy Name",
                        e =>
                        {
                            var item = (KeyValuePair<string, string>)container.userData;

                            EditorGUIUtility.systemCopyBuffer = item.Key;
                        });

                    e.menu.AppendAction("Log All ",
                        e =>
                        {
                            StringBuilder builder = new StringBuilder();
                            builder.AppendLine("Environment Variables");
                            var dic = Environment.GetEnvironmentVariables();
                            foreach (var key in dic.Keys)
                            {
                                object value = dic[key];
                                //builder.AppendLine($"{key}={value}");
                                Debug.Log($"{key}={value}");
                            }
                            //Debug.Log(builder.ToString());
                        });
                    e.menu.AppendSeparator();
                    e.menu.AppendAction("Delete",
                        e =>
                        {
                            var item = (KeyValuePair<string, string>)container.userData;

                            if (string.IsNullOrEmpty(item.Key))
                                return;

                            if (SettingSettings.UserEnvironmentVariables.ContainsKey(item.Key))
                            {
                                SettingSettings.UserEnvironmentVariables.Remove(item.Key);
                                SettingSettings.UserEnvironmentVariables = SettingSettings.UserEnvironmentVariables;
                                OnEnvVariableChanged();
                            }

                        });

                }));
                container.Add(menu);

                {
                    VisualElement template = new VisualElement();
                    template.AddToClassList("dictionary-view-item-tpl");

                    Label nameLabel = new Label();
                    nameLabel.AddToClassList("dictionary-view-item_key");
                    template.Add(nameLabel);

                    TextField valueField = new TextField();
                    valueField.AddToClassList("dictionary-view-item_value");
                    valueField.isDelayed = true;
                    template.Add(valueField);
                    valueField.RegisterValueChangedCallback(e =>
                    {
                        var item = (KeyValuePair<string, string>)container.userData;
                        string value = e.newValue;
                        if (item.Value != value)
                        {
                            if (SettingSettings.UserEnvironmentVariables.ContainsKey(item.Key))
                            {
                                SettingSettings.UserEnvironmentVariables[item.Key] = value;
                                SettingSettings.UserEnvironmentVariables = SettingSettings.UserEnvironmentVariables;
                                OnEnvVariableChanged();
                            }
                        }
                    });

                    container.Add(template);
                }

                {
                    VisualElement template = new VisualElement();
                    template.AddToClassList("dictionary-view-item-new-tpl");

                    TextField nameField = new TextField();
                    nameField.AddToClassList("dictionary-view-item_key");
                    nameField.isDelayed = true;
                    template.Add(nameField);
                    nameField.RegisterValueChangedCallback(e =>
                    {
                        var item = (KeyValuePair<string, string>)container.userData;
                        string name = e.newValue;
                        if (!string.IsNullOrEmpty(name))
                        {
                            if (!SettingSettings.UserEnvironmentVariables.ContainsKey(name))
                            {
                                SettingSettings.UserEnvironmentVariables.Add(name, null);
                                SettingSettings.UserEnvironmentVariables = SettingSettings.UserEnvironmentVariables;
                                OnEnvVariableChanged();
                            }
                            else
                            {
                                Debug.LogError($"Already exists environment variable key: {name}");
                            }
                        }
                    });

                    TextField valueField = new TextField();
                    valueField.AddToClassList("dictionary-view-item_value");
                    valueField.style.visibility = Visibility.Hidden;

                    template.Add(valueField);

                    container.Add(template);
                }
                return container;
            };

            envVariableListView.bindItem = (view, index) =>
            {
                var item = (KeyValuePair<string, string>)envVariableListView.itemsSource[index];
                view.userData = item;
                var tpl = view.Q(className: "dictionary-view-item-tpl");
                var newTpl = view.Q(className: "dictionary-view-item-new-tpl");
                var menu = view.Q(className: "dictionary-view-item_menu");

                if (index == envVariableListView.itemsSource.Count - 1)
                {
                    tpl.style.display = DisplayStyle.None;
                    newTpl.style.display = DisplayStyle.Flex;
                    menu.style.visibility = Visibility.Hidden;

                    var nameField = newTpl.Q<TextField>(className: "dictionary-view-item_key");
                    nameField.SetValueWithoutNotify(item.Key);
                }
                else
                {
                    tpl.style.display = DisplayStyle.Flex;
                    newTpl.style.display = DisplayStyle.None;
                    menu.style.visibility = Visibility.Visible;

                    var nameField = tpl.Q<Label>(className: "dictionary-view-item_key");
                    nameField.text = item.Key;

                    var valueField = tpl.Q<TextField>(className: "dictionary-view-item_value");
                    valueField.SetValueWithoutNotify(item.Value);
                }
            };


            OnVariantChanged();
            OnEnvVariableChanged();
            //variantListField.label = "Variant";
            //variantListField.choices.Clear();
            //variantListField.choices.Add(string.Empty);
            //variantListField.choices.Add(ExampleSettings.debugVariant);
            //variantListField.choices.Add(ExampleSettings.demoVariant);
            //variantListField.choices.Add(ExampleSettings.demoDebugVariant);
            //variantListField.RegisterValueChangedCallback(e =>
            //{
            //    switch (e.newValue)
            //    {
            //        case ExampleSettings.debugVariant:
            //            SettingsUtility.SetVariant(ExampleSettings.debugVariant);
            //            break;
            //        case ExampleSettings.demoDebugVariant:
            //            SettingsUtility.SetVariant(ExampleSettings.demoDebugVariant, ExampleSettings.demoVariant);
            //            break;
            //        case ExampleSettings.demoVariant:
            //            SettingsUtility.SetVariant(ExampleSettings.demoVariant);
            //            break;
            //        default:
            //            SettingsUtility.SetVariant(null);
            //            break;
            //    }
            //});



            EditorSettingsUtility.CreateSettingView(root, typeof(SettingSettings));
        }

        public override void OnDeactivate()
        {
            SettingsUtility.VariantChanged -= OnVariantChanged;
            base.OnDeactivate();
        }

        string FormatDisplayVariant(string variant)
        {
            if (string.IsNullOrEmpty(variant))
                return SettingSettings.DefaultVariantName;
            return variant;
        }
        //解决 DropdownField 设置 choices 后意外触发事件
        bool isSetValue;
        void OnVariantChanged()
        {
            isSetValue = true;
            variantField.choices.Clear();
            variantField.choices.AddRange(SettingsUtility.GetDisplayVariantNames());
            variantField.SetValueWithoutNotify(SettingsUtility.GetDisplayVariantName(SettingsUtility.Variant));

            EditorApplication.delayCall += () =>
            {
                isSetValue = false;
            };

            variantPriorityField.text = string.Join(", ", SettingsUtility.Variants.Select(o =>
            {
                if (o == null)
                    return SettingSettings.DefaultVariantName;
                return o;
            }));

            List<VariantConfig> variantConfigs = new List<VariantConfig>();
            variantConfigs.AddRange(SettingSettings.Variants);
            if (newVariant == null)
                newVariant = new VariantConfig();
            variantConfigs.Add(newVariant);
            variantListView.itemsSource = variantConfigs;
            variantListView.style.height = variantListView.fixedItemHeight * variantListView.itemsSource.Count + 3;
            variantListView.RefreshItems();
        }

        void OnEnvVariableChanged()
        {
            List<KeyValuePair<string, string>> list = new();
            list.AddRange(SettingSettings.UserEnvironmentVariables);
            list.Add(new KeyValuePair<string, string>());
            envVariableListView.itemsSource = list;
            envVariableListView.style.height = envVariableListView.fixedItemHeight * envVariableListView.itemsSource.Count + 3;
            envVariableListView.RefreshItems();
        }

        private VariantConfig newVariant;

    }
}