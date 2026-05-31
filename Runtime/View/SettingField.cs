using System;
using System.Collections;
using System.Reflection;
using UnityEngine.UIElements;


namespace SettingsManagement.UIElements
{
    public class SettingField
    {
        public SettingField(ISetting setting)
        {
            if (setting == null)
                throw new ArgumentNullException(nameof(setting));
            this.Setting = setting;
        }

        internal const string SettingField_ClassName = "setting-field";
        internal const string SettingLabel_ClassName = "setting-field__label";
        internal const string SettingLabelOverride_ClassName = "setting-field__label-override";

        public InputView InputView { get; private set; }
        public VisualElement View;
        public MemberInfo Member { get; internal set; }


        public string DisplayName { get; internal set; }

        public string Tooltip { get; internal set; }

        public ISetting Setting { get; internal set; }

        public string Platform { get; internal set; }

        public string Variant { get => Settings.Variant; }

        public Type ValueType => Setting.ValueType;

        internal SettingMetadata SettingMember { get; set; }

        public bool IsMultiPlatform { get; set; }

        public bool CanDelete { get; set; }

        public Action<ISetting> OnDeleteSetting { get; set; }

        public Action<ISetting, int, int> OnMoveSetting { get; set; }

        public MemberSettings OwnerSettings { get; set; }

        Label label;
        private Action updateLabel;

        public VisualElement CreateView()
        {
            Type viewType = null;

            if (SettingMember != null)
            {
                viewType = SettingMember.ViewType;
            }

            if (viewType == null)
            {
                viewType = SettingsViewUtility.GetInputViewType(ValueType);
            }

            View = new VisualElement();
            View.AddToClassList(SettingField_ClassName);

            VisualElement inputViewElem = null;
            if (viewType != null)
            {
                var inputView = Activator.CreateInstance(viewType) as InputView;
                inputView.DisplayName = DisplayName;
                inputView.ValueType = ValueType;
                this.InputView = inputView;
                InputView.DisplayName = null;
                View.AddToClassList("unity-base-field");
                label = new Label();
                label.AddToClassList("unity-base-field__label");
                label.text = DisplayName;
                label.AddToClassList(SettingLabel_ClassName);

                View.Add(label);
                //VisualElement fieldContainer2=new VisualElement();
                //View.Add(fieldContainer2);
                inputViewElem = InputView.CreateView();
                inputViewElem.style.flexGrow = 1f;

            }
            else if (ValueType.IsArray || typeof(IList).IsAssignableFrom(ValueType))
            {
                var inputView = ArrayView.CreateFromSetting(Setting, SettingMember?.ElementAttribute);
                inputView.DisplayName = DisplayName;
                InputView = inputView;
                this.InputView = inputView;
                inputViewElem = InputView.CreateView();
                label = inputViewElem.Q<Label>(className: ArrayView.SettingsArrayPanel_Label_ClassName);
            }

            if (InputView == null)
            {
                return View;
            }

            View.Add(inputViewElem);
            inputViewElem.tooltip = Tooltip;

            InputView.ValueChanged += OnValueChanged;
            Settings.VariantChanged += SettingsUtility_VariantChanged;
            View.RegisterCallback<DetachFromPanelEvent>(e =>
            {
                Settings.VariantChanged -= SettingsUtility_VariantChanged;
            });

            if (label != null)
            {
                Action<ISetting> deleteSetting = null;
                Action<ISetting, bool> moveSetting = null;
                if (CanDelete)
                {
                    deleteSetting = (setting) =>
                    {
                        //if (View != null)
                        //{
                        //    if (View.parent != null)
                        //    {
                        //        View.parent.Remove(View);
                        //    }
                        //}

                        var memberSetting = setting as IMemberSetting;
                        if (memberSetting != null && OwnerSettings != null)
                        {
                            if (OwnerSettings.DeleteSetting(memberSetting))
                            {
                                this.OnDeleteSetting?.Invoke(setting);
                            }
                        }
                    };

                    moveSetting = (setting, isUp) =>
                    {
                        var memberSetting = setting as IMemberSetting;
                        if (memberSetting != null && OwnerSettings != null)
                        {
                            var settingList = OwnerSettings.SettingList;
                            int index = -1;
                            for (int i = 0; i < settingList.Count; i++)
                            {
                                if (settingList[i] == memberSetting)
                                {
                                    index = i;
                                    break;
                                }
                            }
                            if (index == -1)
                                return;
                            int newIndex;
                            if (isUp)
                            {
                                newIndex = index - 1;
                                if (newIndex < 0)
                                    return;
                            }
                            else
                            {
                                newIndex = index + 1;
                                if (newIndex >= settingList.Count)
                                    return;
                            }
                            OwnerSettings.MoveSetting(memberSetting, newIndex);
                            OnMoveSetting?.Invoke(setting, index, newIndex);
                        }
                    };
                }

                updateLabel = SettingsViewUtility.InitializeSettingFieldLabel(
                    Setting,
                    label,
                    hasValue: (setting) =>
                    {
                        return setting.Contains(Platform, Variant);
                    },
                    setAsValue: (setting) =>
                    {
                        if (!setting.Contains(Platform, Variant))
                        {
                            object newValue;
                            newValue = setting.CopyValue(Platform, Variant);
                            setting.SetValue(Platform, Variant, newValue, true);
                            Refresh();
                        }
                    },
                    unsetValue: (setting) =>
                    {
                        if (setting.Contains(Platform, Variant))
                        {
                            setting.Delete(Platform, Variant, true);
                            Refresh();
                        }
                    },
                    deleteSetting: deleteSetting,
                    moveSetting: moveSetting,
                    onMenu: (setting, menu) =>
                    {
                        InputView.OnMenu(menu);
                    });
            }


            Refresh();
            return View;
        }

        bool IsBoldLabel()
        {
            bool b = Setting.Contains(Platform, Variant);
            b = InputView.IsBoldLabel(b);
            return b;
        }

        private void SettingsUtility_VariantChanged()
        {
            Refresh();
        }

        private void OnValueChanged(object newValue)
        {
            Setting.SetValue(Platform, Variant, newValue, true);
            Refresh();
        }

        private object GetOrDefaultValue()
        {
            //object value;
            //if (Platform != PlatformNames.Default)
            //{
            //    if (Setting.Contains(Platform))
            //    {
            //        value = Setting.GetValue(Platform);
            //    }
            //    else
            //    {
            //        value = Setting.GetCopyValue(PlatformNames.Default);
            //    }
            //}
            //else
            //{
            //    value = Setting.GetValue(Platform);
            //}
            //return value;

            if (Setting.IsCombine || Setting is ICombineSetting)
            {
                return Setting.DirectGetValue(Platform, Variant);
            }

            return Setting.GetValue(Platform, Variant);
        }

        private void Refresh()
        {
            object value = GetOrDefaultValue();
            InputView.SetValue(value);

            if (label != null)
            {
                SettingsViewUtility.UpdateSettingFieldLabel(label, IsBoldLabel());
            }

        }


    }



}
