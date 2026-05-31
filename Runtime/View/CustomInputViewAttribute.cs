using System;

namespace SettingsManagement.UIElements
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class CustomInputViewAttribute : Attribute
    {

        public CustomInputViewAttribute(Type targetType)
        {
            TargetType = targetType;
        }

        public Type TargetType { get; set; }


    }
}
