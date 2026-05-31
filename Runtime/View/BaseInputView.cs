using SettingsManagement;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor.UIElements;
#endif

namespace SettingsManagement.UIElements
{

    class BaseInputView<TView> : InputView
    {
        /*
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
        */
        static MethodInfo registerValueChangedCallbackMethod;

        static MethodInfo RegisterValueChangedCallbackMethod => registerValueChangedCallbackMethod ??= typeof(INotifyValueChangedExtensions).GetMethod("RegisterValueChangedCallback");
        Type viewType => typeof(TView);
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
            //viewType = baseInputFieldTypes[ValueType]; 

            input = Activator.CreateInstance(viewType) as VisualElement;

            viewType.GetProperty("isDelayed")?.SetValue(input, true);
            viewType.GetProperty("label").SetValue(input, DisplayName);

            viewValueType = viewType.GetMethod("SetValueWithoutNotify")?.GetParameters()[0].ParameterType;
            if (viewValueType == null)
            {
                viewValueType = ValueType;
            }

            Delegate del;
            Type delType;
            var ValueChangedCallbackMethod = GetType().GetMethod(nameof(ValueChangedCallback), BindingFlags.NonPublic | BindingFlags.Instance);
             

            delType = typeof(EventCallback<>).MakeGenericType(typeof(ChangeEvent<>).MakeGenericType(viewValueType));
            del = Delegate.CreateDelegate(delType, this, ValueChangedCallbackMethod.MakeGenericMethod(viewValueType));
          

            RegisterValueChangedCallbackMethod.MakeGenericMethod(viewValueType)
                .Invoke(null, new object[] { input, del });

            return input;
        }

        protected void ValueChangedCallback<T>(ChangeEvent<T> e)
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

    [CustomInputView(typeof(string))]
    class TextInputView : BaseInputView<TextField> { }

    [CustomInputView(typeof(short))]

    [CustomInputView(typeof(int))]
    //class ShortInputView : BaseInputView<IntegerField> { }
    class IntInputView : BaseInputView<IntegerField> { }

    [CustomInputView(typeof(long))]
    class LongInputView : BaseInputView<LongField> { }

    [CustomInputView(typeof(float))]
    class FloatInputView : BaseInputView<FloatField> { }

    [CustomInputView(typeof(double))]
    class DoubleInputView : BaseInputView<DoubleField> { }

    [CustomInputView(typeof(bool))]
    class BooleanInputView : BaseInputView<Toggle> { }

    [CustomInputView(typeof(Vector2))]
    class Vector2InputView : BaseInputView<Vector2Field> { }

    [CustomInputView(typeof(Vector2Int))]
    class Vector2IntInputView : BaseInputView<Vector2IntField> { }

    [CustomInputView(typeof(Vector3))]
    class Vector3InputView : BaseInputView<Vector3Field> { }

    [CustomInputView(typeof(Vector3Int))]
    class Vector3IntInputView : BaseInputView<Vector3IntField> { }

    [CustomInputView(typeof(Vector4))]
    class Vector4InputView : BaseInputView<Vector4Field> { }


    [CustomInputView(typeof(Rect))]
    class RectInputView : BaseInputView<RectField> { }


    [CustomInputView(typeof(Bounds))]
    class BoundsInputView : BaseInputView<BoundsField> { }


#if UNITY_2022_1_OR_NEWER

    [CustomInputView(typeof(ushort))]

    [CustomInputView(typeof(uint))]
    class UShortInputView : BaseInputView<UnsignedIntegerField> { }
    //class UIntInputView : BaseInputView< UnsignedIntegerField> { }

    [CustomInputView(typeof(ulong))]
    class ULongInputView : BaseInputView<UnsignedLongField> { }


#endif

#if UNITY_EDITOR
    [CustomInputView(typeof(Color))]
    class ColorInputView : BaseInputView<ColorField> { }

    [CustomInputView(typeof(AnimationCurve))]
    class CurveInputView : BaseInputView<CurveField> { }

#endif
}