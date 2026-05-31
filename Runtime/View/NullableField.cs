using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace SettingsManagement.UIElements
{
    [CustomInputView(typeof(NullableValue<>))]
    public class NullableField : InputView
    {
        private Type elementType;
        private InputView inputView;
        private VisualElement view;
        private object value;

        static Dictionary<Type, NullableValueAccessor> accessors;
        public class NullableValueAccessor
        {
            public FieldInfo hasValueField;
            public FieldInfo valueField;
            public object nullValue;
        }

        public override bool IsBoldLabel(bool isBold)
        {
            return isBold && NullableHasValue(ValueType, value);
        }


        internal static NullableValueAccessor GetAccessor(Type type)
        {

            if (accessors == null)
                accessors = new();
            if (accessors.TryGetValue(type, out var accessor))
                return accessor;
            accessor = new();
            accessor.hasValueField = type.GetField("hasValue", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            accessor.valueField = type.GetField("value", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            accessor.nullValue = type.GetField("Null", BindingFlags.Static | BindingFlags.Public).GetValue(null);
            accessors[type] = accessor;
            return accessor;
        }

        internal static bool NullableHasValue(Type valueType, object nullable)
        {
            if (nullable == null) return false;
            var accessor = GetAccessor(valueType);
            return (bool)accessor.hasValueField.GetValue(nullable);
        }
        internal static object NullableGetValue(Type valueType, object nullable)
        {
            var accessor = GetAccessor(valueType);
            return accessor.valueField.GetValue(nullable);
        }
        internal static void NullableSetValue(Type valueType, object nullable, object newValue)
        {
            var accessor = GetAccessor(valueType);
            accessor.valueField.SetValue(nullable, newValue);
            accessor.hasValueField.SetValue(nullable, true);

        }

        internal static object NullableGetNullValue(Type valueType)
        {
            var accessor = GetAccessor(valueType);

            return accessor.nullValue;
        }

        internal static void NullableClearValue(Type valueType, object nullable)
        {
            var accessor = GetAccessor(valueType);

            accessor.valueField.SetValue(nullable, NullableGetValue(valueType, accessor.nullValue));
            accessor.hasValueField.SetValue(nullable, false);

        }

        public override VisualElement CreateView()
        {
            elementType = ValueType.GetGenericArguments()[0];
            Type viewType = SettingsViewUtility.GetInputViewType(elementType);
            if (viewType != null)
            {
                inputView = Activator.CreateInstance(viewType) as InputView;
                inputView.ValueType = elementType;
                inputView.ValueChanged += InputView_ValueChanged;
                view = inputView.CreateView();
                view.style.flexGrow = 1f;
            }
            return view;
        }

        private void InputView_ValueChanged(object newValue)
        {
            NullableSetValue(ValueType, value, newValue);
            OnValueChanged(value);
        }

        public override void SetValue(object value)
        {
            if (value == null)
            {
                value = GetAccessor(ValueType).nullValue;
            }
            this.value = value;
            object elementValue = null;

            Type valueType = value.GetType();
            if (valueType == this.ValueType)
            {
                elementValue = NullableGetValue(ValueType, value);
            }
            inputView.SetValue(elementValue);
        }

        public override void OnMenu(DropdownMenu menu)
        {
            menu.AppendAction("Null",
                act =>
                {

                    if (NullableHasValue(ValueType, value))
                    {
                        NullableClearValue(ValueType, value);
                    }
                    else
                    {
                        NullableSetValue(ValueType, value, NullableGetValue(ValueType, value));
                    }
                    OnValueChanged(value);
                },
                act =>
                {
                    if (!NullableHasValue(ValueType, value))
                    {
                        return DropdownMenuAction.Status.Checked;
                    }
                    return DropdownMenuAction.Status.Normal;
                });
            base.OnMenu(menu);
            inputView?.OnMenu(menu);
        }
    }


}
