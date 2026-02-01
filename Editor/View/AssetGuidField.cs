using SettingsManagement.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SettingsManagement.Editor
{
    [UnityEditor.CustomPropertyDrawer(typeof(AssetGuid))]
    class AssetGuidPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            ObjectField objectField = new ObjectField();
            objectField.label = property.displayName;

            objectField.RegisterValueChangedCallback(e =>
            {
                AssetGuid assetPath = new AssetGuid(objectField.value);
                SerializedPropertyUtility.SetObjectOfProperty(property, assetPath);
                property.serializedObject.Update();
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            });

            AssetGuid value = (AssetGuid)SerializedPropertyUtility.GetObjectOfProperty(property);
            objectField.SetValueWithoutNotify(value.Target);
            return objectField;
        }
    }


    [CustomInputView(typeof(AssetGuid))]
    public class AssetGuidInputView : InputView
    {
        ObjectField input;

        public override VisualElement CreateView()
        {
            input = new ObjectField();
            input.label = DisplayName;
            input.objectType = typeof(UnityEngine.Object);

            input.RegisterValueChangedCallback(e =>
            {
                AssetGuid assetGuid = new AssetGuid(input.value);
                OnValueChanged(assetGuid);
            });
             
            return input;
        }

        public override void SetValue(object value)
        {
            AssetGuid v = (AssetGuid)value;
            input.SetValueWithoutNotify(v.Target);
        }
    }

}
