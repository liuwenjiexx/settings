using System;
using System.Collections;
using System.Collections.Generic;
using SettingsManagement.Editor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Windows;

namespace SettingsManagement.Examples
{
    [Serializable]
    public class CustomType
    {
        public int i;
        public string s;
    }


    [CustomInputView(typeof(CustomType))]
    class CustomTypeView : InputView
    {
        private IntegerField intInput;
        private TextField strInput;
        private CustomType _value;

        public override VisualElement CreateView()
        {
            VisualElement container = new VisualElement();

            intInput = new IntegerField();
            intInput.label = "Int";
            intInput.isDelayed = true;
            intInput.RegisterValueChangedCallback(e =>
            {
                var value = GetValue();
                if (value.i != e.newValue)
                {
                    value.i = e.newValue;
                    OnValueChanged(value);
                }
            });
            container.Add(intInput);

            strInput = new TextField();
            strInput.label = "string";
            strInput.isDelayed = true;
            strInput.RegisterValueChangedCallback(e =>
            {
                var value = GetValue();
                if (value.s != e.newValue)
                {
                    value.s = e.newValue;
                    OnValueChanged(value);
                }
            });
            container.Add(strInput);
            return container;
        }

        public override void SetValue(object value)
        {
            _value = value as CustomType;
            if (_value != null)
            {
                intInput.SetValueWithoutNotify(_value.i);
                strInput.SetValueWithoutNotify(_value.s);
            }

        }
        CustomType GetValue()
        {
            if (_value == null)
            {
                _value = new CustomType();
            }
            return _value;
        }
    }
}