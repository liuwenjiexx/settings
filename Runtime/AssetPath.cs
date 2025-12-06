using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine
{

    [Serializable]
    public struct AssetPath
#if UNITY_EDITOR
        : ISerializationCallbackReceiver
#endif
    {
        [SerializeField]
        private string guid;
        [SerializeField]
        private string assetPath;

        public AssetPath(string assetGuid, string assetPath)
        {
            this.guid = assetGuid;
            this.assetPath = assetPath;
#if UNITY_EDITOR
            this.target = null;
#endif
        }


        public string Guid => guid;

        public string Path
        {
            get
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(guid))
                {
                    assetPath = AssetDatabase.GUIDToAssetPath(guid);
                }
#endif
                string value = this.assetPath;

                return value;
            }
        }

#if UNITY_EDITOR
        
        [NonSerialized]
        private UnityEngine.Object target;

        public AssetPath(UnityEngine.Object target)
        {
            guid = null;
            assetPath = null;
            this.target = target;
            if (target)
            {
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out guid, out long id))
                {
                    assetPath = AssetDatabase.GUIDToAssetPath(guid);
                }
            }
        }

        public UnityEngine.Object Asset
        {
            get => Target;
        }

        public UnityEngine.Object Target
        {
            get
            {
                if (target)
                    return target;
                if (!string.IsNullOrEmpty(guid))
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        target = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    }
                }
                else if (!string.IsNullOrEmpty(assetPath))
                {
                    target = AssetDatabase.LoadMainAssetAtPath(assetPath);
                }
                return target;
                Debug.LogError("Can't at runtime access AssetPath.Target");
                return null;

            }
        }
 
        public void OnBeforeSerialize()
        {
            if (!string.IsNullOrEmpty(guid))
            {
                assetPath = AssetDatabase.GUIDToAssetPath(guid);
            }
        }

        public void OnAfterDeserialize()
        {
   
        }
#endif

    }


}
