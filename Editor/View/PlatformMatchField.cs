using Codice.Client.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using SettingsManagement;
using SettingsManagement.Editor;
using UnityEditor.UIElements;

namespace SettingsManagement.Editor
{
    [CustomInputView(typeof(PlatformMatch))]
    public class PlatformMatchField : InputView
    {
        private VisualElement view;
        private PlatformMatch value;

        private string[] platforms;

        const string Field_ClassName = "platform-match-field";
        const string FieldAny_ClassName = "platform-match-field__any";
        const string PlatformsContainer_ClassName = "platform-match-field__platforms";
        const string PlatformList_ClassName = "platform-match-field__platform-list";
        const string SelectedPlatform_ClassName = "platform-match-field__platform_selected";

        public string[] Platforms
        {
            get
            {

                if (platforms != null)
                {
                    return platforms;
                }
                return PlatformNames.AllPlatforms2;
            }
            set => platforms = value;
        }


        public override VisualElement CreateView()
        {
            VisualElement view = new VisualElement();
            view.AddToClassList("unity-base-field");
            view.AddToClassList(Field_ClassName);

            Label label = new Label();
            label.AddToClassList("unity-base-field__label");
            label.text = DisplayName;
            view.Add(label);

            VisualElement input = new VisualElement();
            input.AddToClassList("unity-base-field__input");
            Toggle anyField = new Toggle();
            anyField.AddToClassList(FieldAny_ClassName);
            anyField.text = "Any";
            anyField.RegisterValueChangedCallback(e =>
            {
                if (value.isAny != e.newValue)
                {
                    value.isAny = e.newValue;
                    if (e.newValue)
                    {
                        value.exclude = null;
                    }
                    else
                    {
                        IncludeAllPlatform();
                    }
                    OnValueChanged(value);
                    SetValue(value);
                }
            });
            input.Add(anyField);
            VisualElement platformsContainer = new VisualElement();
            platformsContainer.AddToClassList(PlatformsContainer_ClassName);

            input.Add(platformsContainer);

            view.Add(input);

            this.view = view;
            return view;
        }

        public override void SetValue(object newValue)
        {
            if (newValue == null)
            {
                newValue = new PlatformMatch();
            }
            value = (PlatformMatch)newValue;

            if (view != null)
            {
                Toggle anyField = view.Q<Toggle>(className: FieldAny_ClassName);
                VisualElement platformsContainer = view.Q(className: PlatformsContainer_ClassName);

                bool isAny = string.IsNullOrEmpty(value.include);

                anyField.SetValueWithoutNotify(value.isAny);
                platformsContainer.Clear();
                List<string> selectedPlatforms = new List<string>();
                List<string> popPlatforms = Platforms.ToList();
                if (value.isAny)
                {
                    //Label excludeLabel = new Label();
                    //excludeLabel.AddToClassList("platform-field__exclude-label");
                    //excludeLabel.text = "Exclude Platforms:";
                    //platformsContainer.Add(excludeLabel);
                    if (!string.IsNullOrEmpty(value.exclude))
                    {
                        selectedPlatforms.AddRange(value.exclude.Split(";", StringSplitOptions.RemoveEmptyEntries));
                    }
                }
                else
                {
                    //Label includeLabel = new Label();
                    //includeLabel.AddToClassList("platform-field__include-label");
                    //includeLabel.text = "Include Platforms:";
                    //platformsContainer.Add(includeLabel);
                    if (!string.IsNullOrEmpty(value.include))
                    {
                        selectedPlatforms.AddRange(value.include.Split(";"));
                    }
                }


                popPlatforms = popPlatforms.Except(selectedPlatforms).ToList();

                popPlatforms.Insert(0, "Select all");
                popPlatforms.Insert(1, "Deselect all");


                PopupField<string> popupField = new PopupField<string>(popPlatforms.Select(o => PlatformNames.GetShortDisplayName(o)).ToList(), -1);
                popupField.AddToClassList(PlatformList_ClassName);
                if (value.isAny)
                {
                    popupField.value = "Exclude Platforms";
                }
                else
                {
                    popupField.value = "Include Platforms";
                }

                popupField.RegisterValueChangedCallback(e =>
                {
                    string selectedPlatform = null;

                    if (e.newValue == "Select all")
                    {
                        if (value.isAny)
                        {
                            ExcludeAllPlatform();
                        }
                        else
                        {
                            IncludeAllPlatform();
                        }
                    }
                    else if (e.newValue == "Deselect all")
                    {
                        if (!string.IsNullOrEmpty(value.include) || !string.IsNullOrEmpty(value.exclude))
                        {
                            value.include = null;
                            value.exclude = null;
                            OnValueChanged(value);
                            SetValue(value);
                        }

                    }


                    foreach (var p in Platforms)
                    {
                        if (PlatformNames.GetShortDisplayName(p) == e.newValue)
                        {
                            selectedPlatform = p;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(selectedPlatform))
                    {
                        return;
                    }

                    if (value.isAny)
                    {
                        string[] platforms;
                        if (value.exclude == null)
                        {
                            platforms = new string[0];
                        }
                        else
                        {
                            platforms = value.exclude.Split(";", StringSplitOptions.RemoveEmptyEntries).ToArray();
                        }

                        if (!platforms.Contains(selectedPlatform))
                        {
                            value.exclude = string.Join(";", platforms.Concat(new string[] { selectedPlatform }));
                            OnValueChanged(value);
                            SetValue(value);
                        }
                    }
                    else
                    {
                        string[] platforms;
                        if (value.include == null)
                        {
                            platforms = new string[0];
                        }
                        else
                        {
                            platforms = value.include.Split(";", StringSplitOptions.RemoveEmptyEntries).ToArray();
                        }

                        if (!platforms.Contains(selectedPlatform))
                        {
                            value.include = string.Join(";", platforms.Concat(new string[] { selectedPlatform }));
                            OnValueChanged(value);
                            SetValue(value);
                        }
                    }

                });
                platformsContainer.Add(popupField);


                foreach (var platform in selectedPlatforms.OrderBy(o => o))
                {
                    Label platformLabel = new Label();
                    platformLabel.AddToClassList(SelectedPlatform_ClassName);
                    platformLabel.text = PlatformNames.GetShortDisplayName(platform);
                    platformLabel.RegisterCallback<MouseDownEvent>(e =>
                    {
                        if (value.isAny)
                        {

                            if (!string.IsNullOrEmpty(value.exclude))
                            {
                                string newExclude = string.Join(";", this.value.exclude.Split(";", StringSplitOptions.RemoveEmptyEntries).Where(o => o != platform));
                                if (value.exclude != newExclude)
                                {
                                    value.exclude = newExclude;
                                    OnValueChanged(value);
                                    SetValue(value);
                                }
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(value.include))
                            {
                                string newInclude = string.Join(";", value.include.Split(";", StringSplitOptions.RemoveEmptyEntries).Where(o => o != platform));
                                if (value.include != newInclude)
                                {
                                    value.include = newInclude;
                                    OnValueChanged(value);
                                    SetValue(value);
                                }
                            }
                        }

                    });
                    platformsContainer.Add(platformLabel);
                }
            }
        }

        void IncludeAllPlatform()
        {
            bool changed = false;
            List<string> selectedPlatforms = new List<string>();

            if (value.include != null)
            {
                selectedPlatforms.AddRange(value.include.Split(";", StringSplitOptions.RemoveEmptyEntries));
            }
            foreach (string platform in Platforms)
            {
                if (!selectedPlatforms.Contains(platform))
                {
                    selectedPlatforms.Add(platform);
                }
            }
            string newInclude = string.Join(";", selectedPlatforms);
            if (newInclude != value.include)
            {
                value.include = newInclude;
                changed = true;
            }

            if (changed)
            {
                OnValueChanged(value);
                SetValue(value);
            }
        }

        void ExcludeAllPlatform()
        {
            bool changed = false;
            List<string> selectedPlatforms = new List<string>();
            if (value.exclude != null)
            {
                selectedPlatforms.AddRange(value.exclude.Split(";", StringSplitOptions.RemoveEmptyEntries));
            }
            foreach (string platform in Platforms)
            {
                if (!selectedPlatforms.Contains(platform))
                {
                    selectedPlatforms.Add(platform);
                    changed = true;
                }
            }
            string newExclude = string.Join(";", selectedPlatforms);
            if (newExclude != value.exclude)
            {
                value.exclude = newExclude;
                changed = true;
            }

            if (changed)
            {
                OnValueChanged(value);
                SetValue(value);
            }
        }

    }
}