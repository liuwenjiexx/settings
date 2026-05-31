using SettingsManagement.UIElements;
using System.IO;
using UnityEditor;
using UnityEngine.UIElements;

namespace SettingsManagement.Editor
{



    [CustomInputView(typeof(PathFieldAttribute))]
    public class PathField : InputView
    {
        private VisualElement view;
        private string value;
        public override VisualElement CreateView()
        {


            VisualElement view = new VisualElement();

            view.AddToClassList("path-field");
            TextField valueField = new TextField();
            valueField.AddToClassList("path-field__value");
            valueField.label = DisplayName;
            valueField.isDelayed = true;
            valueField.RegisterValueChangedCallback(e =>
            {
                OnValueChanged(e.newValue);
            });
            valueField.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.ctrlKey && e.button == 0)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (Directory.Exists(value))
                        {
                            EditorUtility.RevealInFinder(value);
                        }
                    }
                }
            }, TrickleDown.TrickleDown);

            view.Add(valueField);

            Label pickButton = new Label();
            pickButton.AddToClassList("path-field__pick");
            pickButton.text = "...";
            pickButton.RegisterCallback<MouseDownEvent>(e =>
            {
                string path;
                var attr = FieldAttribue as PathFieldAttribute;
                if (attr != null && attr.IsFolder)
                {
                    path = EditorUtility.OpenFolderPanel("Open Foelder", value, null);
                }
                else
                {
                    path = EditorUtility.OpenFilePanel("Open File", value, null);
                }

                if (!string.IsNullOrEmpty(path))
                {
                    path = path.Replace("\\", "/");
                    if (path.StartsWith(Path.GetFullPath(".").Replace("\\", "/") + "/"))
                    {
                        path = path.Substring(Path.GetFullPath(".").Length + 1);
                    }
                    if (value != path)
                    {
                        value = path;
                        OnValueChanged(value);
                    }
                }
            });
            view.Add(pickButton);

            this.view = view;
            return view;
        }

        public override void SetValue(object newValue)
        {
            value = newValue as string;
            if (view != null)
            {
                TextField valueField = view.Q<TextField>(className: "path-field__value");
                valueField.SetValueWithoutNotify(value);
            }
        }
    }
}