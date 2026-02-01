using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Unity;

namespace SettingsManagement.Editor
{

    [CustomInputView(typeof(AssetPath))]
    public class AssetPathField : InputView
    {
        ObjectField input;
        public override VisualElement CreateView()
        {
            input = new ObjectField();
            input.label = DisplayName;
            //input.objectType = typeof(GameObject);
            input.objectType = typeof(UnityEngine.Object);
            input.RegisterValueChangedCallback(e =>
            {
                /*  var prefab = e.newValue as GameObject;
                  AssetPath newValue;
                  newValue = new AssetPath(prefab);
                  OnValueChanged(newValue);*/
                AssetGuid assetGuid = new AssetGuid(input.value);
                OnValueChanged(assetGuid);
            });

            return input;
        }

        public override void SetValue(object value)
        {
            /*  GameObject prefab = null;
              if (value is AssetPath assetPath)
              {
                  prefab = assetPath.Asset as GameObject;
              }
              input.SetValueWithoutNotify(prefab);
              */
            AssetGuid v = (AssetGuid)value;
            input.SetValueWithoutNotify(v.Target);
        }
    }

    [UnityEditor.CustomPropertyDrawer(typeof(AssetPath))]
    class AssetPathPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            ObjectField objectField = new ObjectField();
            objectField.label = property.displayName;

            objectField.RegisterValueChangedCallback(e =>
            {
                AssetPath assetPath = new AssetPath(objectField.value);
                SerializedPropertyUtility.SetObjectOfProperty(property, assetPath); 
                property.serializedObject.Update();
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            });

            AssetPath value = (AssetPath)SerializedPropertyUtility.GetObjectOfProperty(property);
            objectField.SetValueWithoutNotify(value.Target);
            return objectField;
        }
    }

}