using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using SettingsManagement.Editor;
using UnityEditor.UIElements;

namespace Unity.Text.Editor
{
    [CustomInputView(typeof(EncodingRule))]
    public class EncodingRuleView : InputView
    {
        string EmptyEncoding = "(None)";
        VisualElement view;
        PathField pathField;
        private EncodingRule value;
        static string MemberClassNamePrefix = "encoding-rule-field__";
        static string encodingClassName = $"{MemberClassNamePrefix}encoding";
        static string filterClassName = $"{MemberClassNamePrefix}filter";
        static string extensionClassName = $"{MemberClassNamePrefix}extension";

        public override VisualElement CreateView()
        {
            VisualElement view = new VisualElement();
            view.AddToClassList("encoding-rule-field");

            var encodingNames = EncodingNames.AllEncodingNames.ToList();
            encodingNames.Insert(0, EmptyEncoding);
            PopupField<string> encodingField = new PopupField<string>(encodingNames, -1, formatSelectedValueCallback: FormatEncodingName, formatListItemCallback: FormatEncodingName);
            encodingField.AddToClassList(encodingClassName);
            encodingField.label = "Encoding";
            encodingField.RegisterValueChangedCallback(e =>
            {
                string value2 = e.newValue;
                if (value2 == EmptyEncoding)
                {
                    value2 = string.Empty;
                }
                if (value2 != value.EncodingName)
                {
                    value.EncodingName = value2;
                    OnValueChanged(value);
                }
            });
            view.Add(encodingField);


            TextField filterField = new TextField();
            filterField.AddToClassList(filterClassName);
            filterField.label = "Filter";
            filterField.isDelayed = true;
            filterField.tooltip = "过滤，正则表达式格式";
            filterField.RegisterValueChangedCallback(e =>
            {
                value.Filter = e.newValue;
                OnValueChanged(value);
            });
            view.Add(filterField);


            TextField extensionField = new TextField();
            extensionField.AddToClassList(extensionClassName);
            extensionField.label = "Extension";
            extensionField.isDelayed = true;
            extensionField.tooltip = "过滤扩展名，多个 '|' 分隔";
            extensionField.RegisterValueChangedCallback(e =>
            {
                value.Extension = e.newValue;
                OnValueChanged(value);
            });
            view.Add(extensionField);


            this.view = view;
            return view;
        }

        string FormatEncodingName(string value)
        {
            if (value == null || value == EmptyEncoding)
                return EmptyEncoding;
            string name = EncodingNames.GetDisplayName(value);
            if (name == null)
            {
                return value;
            }
            return name;
        }

        public override void SetValue(object newValue)
        {
            if (newValue == null)
            {
                newValue = new EncodingRule()
                {

                };
            }

            value = newValue as EncodingRule;

            if (view != null)
            {
                var encodingField = view.Q<PopupField<string>>(className: encodingClassName);
                var filterField = view.Q<TextField>(className: filterClassName);
                var extensionField = view.Q<TextField>(className: extensionClassName);
                if (string.IsNullOrEmpty(value.EncodingName))
                {
                    encodingField.SetValueWithoutNotify(EmptyEncoding);
                }
                else
                {
                    encodingField.SetValueWithoutNotify(value.EncodingName);
                }

                filterField.SetValueWithoutNotify(value.Filter);
                extensionField.SetValueWithoutNotify(value.Extension);


            }
        }
    }
}