using SettingsManagement.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SettingsManagement.Editor
{
   

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
            Type viewType = SettingsViewUtility.GetInputViewType(elementType);
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
