using System;
using UnityEngine;

namespace SettingsManagement
{
    [Serializable]
    public class SerializableType : ISerializationCallbackReceiver
    {
        public SerializableType()
        {
        }

        [SerializeField]
        private string typeName;

        private Type type;
        public Type Type
        {
            get
            {
                if (type == null)
                {
                    if (!string.IsNullOrEmpty(typeName))
                        type = Type.GetType(typeName);
                }
                return type;
            }
            set
            {
                if (Type != value)
                {
                    type = value;
                    typeName = null;
                }
            }
        }

        public string TypeName
        {
            get => typeName;
            set
            {
                if (typeName != value)
                {
                    type = null;
                    typeName = value;
                }
            }
        }

        public void OnBeforeSerialize()
        {
            if (type != null)
            {
                typeName = type.AssemblyQualifiedName;
            }
        }

        public void OnAfterDeserialize()
        {

        }
    }
}