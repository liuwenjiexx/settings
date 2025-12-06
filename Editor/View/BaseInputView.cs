using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SettingsManagement.Editor
{

    class BaseInputView : InputView
    {
        static Dictionary<Type, Type> baseInputFieldTypes = new()
        {
            { typeof(string),typeof(TextField)},
            { typeof(short),typeof(IntegerField)},
            { typeof(int),typeof(IntegerField)},
            { typeof(long),typeof(LongField)},
            { typeof(float),typeof(FloatField)},
            { typeof(double),typeof(DoubleField)},
            { typeof(bool),typeof(Toggle)},
            { typeof(Vector2),typeof(Vector2Field)},
            { typeof(Vector2Int),typeof(Vector2IntField)},
            { typeof(Vector3),typeof(Vector3Field)},
            { typeof(Vector3Int),typeof(Vector3IntField)},
            { typeof(Vector4),typeof(Vector4Field)},
            { typeof(Color),typeof(ColorField)},
            { typeof(Rect),typeof(RectField)},
            { typeof(AnimationCurve),typeof(CurveField)},
            { typeof(Bounds),typeof(BoundsField)},
#if UNITY_2022_1_OR_NEWER
            { typeof(ushort),typeof(UnsignedIntegerField)},
            { typeof(uint),typeof(UnsignedIntegerField)},
            { typeof(ulong),typeof(UnsignedLongField)},
#endif
        };

        public static bool IsBaseField(Type type)
        {
            return baseInputFieldTypes.ContainsKey(type);
        }

        static MethodInfo registerValueChangedCallbackMethod;

        static MethodInfo RegisterValueChangedCallbackMethod => registerValueChangedCallbackMethod ??= typeof(INotifyValueChangedExtensions).GetMethod("RegisterValueChangedCallback");
        Type viewType;
        Type viewValueType;
        VisualElement input;

        public override void SetValue(object value)
        {
            //fieldType.GetMethod("SetValueWithoutNotify").Invoke(input, new object[] { Setting.GetValue(Platform) });
            if (ValueType != viewValueType)
            {
                value = Convert.ChangeType(value, viewValueType);
            }

            viewType.GetMethod("SetValueWithoutNotify").Invoke(input, new object[] { value });

        }

        public override VisualElement CreateView()
        {
            viewType = baseInputFieldTypes[ValueType];

            input = Activator.CreateInstance(viewType) as VisualElement;

            viewType.GetProperty("isDelayed")?.SetValue(input, true);
            viewType.GetProperty("label").SetValue(input, DisplayName);

            viewValueType = viewType.GetMethod("SetValueWithoutNotify")?.GetParameters()[0].ParameterType;
            if (viewValueType == null)
            {
                viewValueType = ValueType;
            }

            var ValueChangedCallbackMethod = GetType().GetMethod(nameof(ValueChangedCallback), BindingFlags.NonPublic | BindingFlags.Instance);

            var delType = typeof(EventCallback<>).MakeGenericType(typeof(ChangeEvent<>).MakeGenericType(viewValueType));

            Delegate del;
            del = Delegate.CreateDelegate(delType, this, ValueChangedCallbackMethod.MakeGenericMethod(viewValueType));
            RegisterValueChangedCallbackMethod.MakeGenericMethod(viewValueType)
                .Invoke(null, new object[] { input, del });

            return input;
        }

        void ValueChangedCallback<T>(ChangeEvent<T> e)
        {
            //Setting.SetValue(Platform, e.newValue, true);
            object newValue = e.newValue;
            if (ValueType != viewValueType)
            {
                newValue = Convert.ChangeType(newValue, ValueType);
            }
            OnValueChanged(newValue);

        }
    }


    [CustomInputView(typeof(Enum))]
    public class EnumInputView : InputView
    {
        EnumField input;
        public override VisualElement CreateView()
        {
            input = new EnumField();
            input.label = DisplayName;
            input.Init((Enum)Activator.CreateInstance(ValueType));
            input.RegisterValueChangedCallback(e =>
            {
                OnValueChanged(e.newValue);
            });

            return input;
        }

        public override void SetValue(object value)
        {
            Enum @enum = (Enum)Convert.ChangeType(value, typeof(Enum));
            input.SetValueWithoutNotify(@enum);
        }
    }

}