using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SettingsManagement.Editor
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
            Type viewType = EditorSettingsUtility.GetInputViewType(elementType);
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



    [CustomPropertyDrawer(typeof(NullableValueAttribute))]
    class NullablePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            Type valueType = fieldInfo.FieldType;
            Action refreshLabel = null;
            var view = new VisualElement();
            view.AddToClassList("unity-base-field");
            var label = new Label();
            label.AddToClassList("unity-base-field__label");
            label.text = property.displayName;
            var hasValueProperty = property.FindPropertyRelative("hasValue");
            InputView inputView = null;
            label.AddManipulator(new MenuManipulator(e =>
            {
                e.menu.AppendAction("Unset",
                    act =>
                    {
                        var value = NullableField.NullableGetNullValue(valueType);
                        SerializedPropertyUtility.SetObjectOfProperty(property, value);
                        property.serializedObject.Update();
                        inputView.SetValue(NullableField.NullableGetValue(valueType, value));
                        foreach (var t in property.serializedObject.targetObjects)
                        {
                            EditorUtility.SetDirty(t);
                        }
                        refreshLabel();
                    },
                    act =>
                    {
                        if (!hasValueProperty.boolValue)
                        {
                            return DropdownMenuAction.Status.Disabled;
                        }
                        return DropdownMenuAction.Status.Normal;
                    });
            }));

            view.Add(label);

            Type elementType = valueType.GetGenericArguments()[0];
            Type viewType = EditorSettingsUtility.GetInputViewType(elementType);
            inputView = Activator.CreateInstance(viewType) as InputView;
            inputView.ValueType = elementType;
            inputView.ValueChanged += (newValue) =>
            {
                var nullable = Activator.CreateInstance(valueType, newValue);
                SerializedPropertyUtility.SetObjectOfProperty(property, nullable);
                property.serializedObject.Update();
                foreach (var t in property.serializedObject.targetObjects)
                {
                    EditorUtility.SetDirty(t);
                }
                refreshLabel();
            };

            var inputElem = inputView.CreateView();
            inputElem.style.flexGrow = 1f;
            inputView.SetValue(NullableField.NullableGetValue(valueType, SerializedPropertyUtility.GetObjectOfProperty(property)));
            view.Add(inputElem);

            refreshLabel = () =>
            {
                if (hasValueProperty.boolValue)
                {
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;
                }
                else
                {
                    label.style.unityFontStyleAndWeight = FontStyle.Normal;
                }
            };
            refreshLabel();
            return view;
        }


    }


}
