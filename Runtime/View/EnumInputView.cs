using System;
using UnityEngine.UIElements;

namespace SettingsManagement.UIElements
{


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
