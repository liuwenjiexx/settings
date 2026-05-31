using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SettingsManagement.UIElements
{

    public class ArrayView : InputView
    {
        private VisualElement root;

        private object value;
        private int selectedIndex = -1;
        private int count;
        private VisualElement contentContainer;

        public Type ItemType { get; set; }

        public event Action<int, object> onChanged;

        public event Action onAddItem;
        public event Action<int> onRemoveItem;
        public event Action<int, int> onMoveItem;
        public Action<ContextualMenuPopulateEvent> addMenu;

        internal const string SettingsArrayPanel_ClassName = "settings-array-panel";
        internal const string SettingsArrayPanel_Border_ClassName = "settings-array-panel_border";

        internal const string SettingsArrayPanel_Label_ClassName = "settings-array-panel_label";

        internal const string SettingsArrayPanel_ContentContainer_ClassName = "settings-array-panel_content_container";
        internal const string SettingsArrayPanel_ContentEmpty_ClassName = "settings-array-panel_content_empty";
        internal const string SettingsArrayPanel_Item_ClassName = "settings-array-panel_item";
        internal const string SettingsArrayPanel_Item_Active_ClassName = "settings-array-panel_item_active";
        internal const string SettingsArrayPanel_Item_Draggable_ClassName = "settings-array-panel_item_draggable";
        internal const string SettingsArrayPanel_Item_Content_ClassName = "settings-array-panel_item_content";
        internal const string SettingsArrayPanel_ButtonBorder_ClassName = "settings-array-panel_button_border";
        internal const string SettingsArrayPanel_ButtonContainer_ClassName = "settings-array-panel_button_container";
        internal const string SettingsArrayPanel_Button_ClassName = "settings-array-panel_button";
        internal const string SettingsArrayPanel_Button_Add_ClassName = "settings-array-panel_button_add";
        internal const string SettingsArrayPanel_Button_AddMenu_ClassName = "settings-array-panel_button_add_menu";
        internal const string SettingsArrayPanel_Button_Remove_ClassName = "settings-array-panel_button_remove";

        static VisualElement lastSelectedItem;

        public override VisualElement CreateView()
        {
            root = new VisualElement();
            root.AddToClassList(SettingsArrayPanel_ClassName);

            VisualElement border = new VisualElement();
            border.AddToClassList(SettingsArrayPanel_Border_ClassName);
            root.Add(border);

            Label lblLabel = new Label();
            lblLabel.AddToClassList(SettingsArrayPanel_Label_ClassName);
            lblLabel.text = DisplayName ?? "XXX";
            border.Add(lblLabel);

            selectedIndex = -1;
            count = 0;

            contentContainer = new VisualElement();
            contentContainer.AddToClassList(SettingsArrayPanel_ContentContainer_ClassName);
            //contentContainer.AddManipulator(new DragManipulator(this));
            border.Add(contentContainer);

            VisualElement buttonContainer = new VisualElement();
            buttonContainer.AddToClassList(SettingsArrayPanel_ButtonContainer_ClassName);
            root.Add(buttonContainer);

            VisualElement buttonBorder = new VisualElement();
            buttonBorder.AddToClassList(SettingsArrayPanel_ButtonBorder_ClassName);
            buttonContainer.Add(buttonBorder);


            Button btnAdd = new Button();
            btnAdd.AddToClassList(SettingsArrayPanel_Button_ClassName);
            buttonBorder.Add(btnAdd);
            if (addMenu != null)
            {
                btnAdd.AddToClassList(SettingsArrayPanel_Button_AddMenu_ClassName);
                btnAdd.RegisterCallback<ContextualMenuPopulateEvent>(e =>
                {
                    int oldCount = 0;
                    addMenu(e);
                    int count = GetCount();
                    if (oldCount != count)
                    {
                        selectedIndex = count - 1;
                        Refresh();
                        Focus(selectedIndex);
                    }
                    OnValueChanged(value);
                });
            }
            else
            {
                btnAdd.AddToClassList(SettingsArrayPanel_Button_Add_ClassName);
                //d_CreateAddNew
                btnAdd.text = "+";
                btnAdd.clicked += () =>
                {
                    int oldCount = 0;
                    onAddItem?.Invoke();
                    int count = GetCount();
                    if (oldCount != count)
                    {
                        selectedIndex = count - 1;
                        Refresh();
                        Focus(selectedIndex);
                    }
                    OnValueChanged(value);
                };
            }

            Button btnRemove = new Button();
            btnRemove.AddToClassList(SettingsArrayPanel_Button_ClassName);
            btnRemove.AddToClassList(SettingsArrayPanel_Button_Remove_ClassName);
            btnRemove.text = "-";
            buttonBorder.Add(btnRemove);
            btnRemove.clicked += () =>
            {
                if (selectedIndex == -1)
                    return;

                onRemoveItem(selectedIndex);
                int count = GetCount();
                if (selectedIndex >= count - 1)
                {
                    selectedIndex = count - 1;
                }
                OnValueChanged(value);
                Refresh();
            };
            return root;
        }

        IEnumerable<VisualElement> GetItemViews()
        {
            return contentContainer.Children().Where(o => o.ClassListContains(SettingsArrayPanel_Item_ClassName));
        }

        void Focus(int index)
        {
            int i = 0;
            foreach (var item in GetItemViews())
            {
                if (i == index)
                {
                    var input = item.Q(className: "unity-base-text-field__input");
                    if (input != null)
                    {
                        input.Focus();
                    }
                    else
                    {
                        item.Focus();
                    }
                    break;
                }
                i++;
            }
        }

        int GetCount()
        {
            return GetItemViews().Count();
        }

        private void Refresh()
        {
            int index = 0;
            foreach (var item in GetItemViews())
            {
                if (selectedIndex == index)
                {
                    item.AddToClassList(SettingsArrayPanel_Item_Active_ClassName);
                    if (lastSelectedItem != null && lastSelectedItem != item)
                    {
                        lastSelectedItem.RemoveFromClassList(SettingsArrayPanel_Item_Active_ClassName);
                        lastSelectedItem = item;
                    }
                    lastSelectedItem = item;
                }
                else
                {
                    item.RemoveFromClassList(SettingsArrayPanel_Item_Active_ClassName);
                }
                index++;
            }

            if (selectedIndex >= count)
                selectedIndex = -1;
        }

        Action onSetValue;

        public override void SetValue(object newValue)
        {
            if (contentContainer == null)
                return;
            contentContainer.Clear();
            count = 0;
            value = newValue;
            var items = newValue as IEnumerable;

            if (items == null)
            {
                items = new object[0];
                //Label empty = new Label();
                //empty.AddToClassList(SettingsArrayPanel_ContentEmpty_ClassName);
                //empty.text = "List is Empty";
                //contentContainer.Add(empty);
                //selectedIndex = -1;
                //return;
            }

            Type inputViewType = null;

            if (FieldAttribue != null)
            {
                inputViewType = SettingsViewUtility.GetInputViewType(FieldAttribue.GetType());
            }

            if (inputViewType == null)
            {
                inputViewType = SettingsViewUtility.GetInputViewType(ItemType);
            }

            var dragManipulator = new DragManipulator(this);
            int index = 0;



            foreach (var item in items)
            {
                int itemIndex = index;

                VisualElement itemContainer = new VisualElement();
                itemContainer.AddToClassList(SettingsArrayPanel_Item_ClassName);
                itemContainer.RegisterCallback<PointerDownEvent>(e =>
                {
                    selectedIndex = itemIndex;
                    Refresh();
                }, TrickleDown.TrickleDown);

                Image draggable = new Image();
                draggable.AddToClassList(SettingsArrayPanel_Item_Draggable_ClassName);
                draggable.userData = itemIndex;
                draggable.AddManipulator(dragManipulator);
#if UNITY_EDITOR
                draggable.image = EditorGUIUtility.IconContent("d_PauseButton")?.image;
#endif
                itemContainer.Add(draggable);

                VisualElement itemContent = new VisualElement();
                itemContent.AddToClassList(SettingsArrayPanel_Item_Content_ClassName);
                itemContainer.Add(itemContent);


                if (inputViewType != null)
                {
                    var inputView = Activator.CreateInstance(inputViewType) as InputView;
                    inputView.FieldAttribue = FieldAttribue;
                    inputView.ValueType = ItemType;
                    var view = inputView.CreateView();
                    if (view != null)
                    {
                        itemContent.Add(view);
                    }
                    inputView.SetValue(item);
                    inputView.ValueChanged += (newValue) =>
                    {
                        onChanged?.Invoke(itemIndex, newValue);
                    };
                }

                contentContainer.Add(itemContainer);

                index++;
                count++;
            }

            if (count == 0)
            {
                //Label empty = new Label();
                //empty.AddToClassList(SettingsArrayPanel_ContentEmpty_ClassName);
                //empty.text = "List is Empty";
                //contentContainer.Add(empty);
                selectedIndex = -1;
            }

            Refresh();
            onSetValue?.Invoke();
        }

        //public static ArrayView CreateFromSetting(ISetting setting, SettingsPlatform platform, InputFieldAttribue fieldAttribue = null, bool autoSave = false)
        //{
        //    return CreateFromSetting(setting, platform.ToString(), setting.GetValue(platform.ToString()), fieldAttribue, autoSave = false);
        //}

        public static ArrayView CreateFromSetting(ISetting setting, InputFieldAttribue fieldAttribue = null, bool autoSave = false)
        {
            ArrayView arrayView = new ArrayView();
            arrayView.FieldAttribue = fieldAttribue;
            arrayView.ValueType = setting.ValueType;
            Type itemType;
            bool isList = false;
            if (setting.ValueType.IsArray)
            {
                itemType = setting.ValueType.GetElementType();
                arrayView.ItemType = itemType;
            }
            else
            {
                isList = true;
                itemType = setting.ValueType.GetGenericArguments()[0];
                arrayView.ItemType = itemType;

            }

            IList list = null;
            Array array = null;
            arrayView.onSetValue = () =>
            {
                if (isList)
                {
                    list = arrayView.value as IList;
                    if (list == null)
                    {
                        list = Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType)) as IList;
                        //setting.SetValue(platform, list);
                    }
                }
                else
                {
                    array = arrayView.value as Array;
                    if (array == null)
                    {
                        array = Array.CreateInstance(arrayView.ItemType, 0);
                        //setting.SetValue(platform, array);
                    }
                }
            };

            //arrayView.SetValue(setting.GetValue(platform));

            arrayView.onChanged += (index, newValue) =>
            {
                if (isList)
                {
                    list[index] = newValue;
                    //if (autoSave)
                    //setting.SetValue(platform, list, true);
                    arrayView.OnValueChanged(list);
                }
                else
                {
                    array.SetValue(newValue, index);
                    //if (autoSave)
                    //setting.SetValue(platform, array, true);
                    arrayView.OnValueChanged(array);
                }
            };

            arrayView.onAddItem += () =>
            {
                object newValue = null;
                if (itemType.IsValueType)
                {
                    newValue = Activator.CreateInstance(itemType);
                }

                if (isList)
                {
                    list.Add(newValue);
                    //if (autoSave)
                    //setting.SetValue(platform, list, true);
                    arrayView.SetValue(list);
                    arrayView.OnValueChanged(list);
                }
                else
                {
                    Array newArray = Array.CreateInstance(itemType, array.Length + 1);
                    array.CopyTo(newArray, 0);
                    newArray.SetValue(newValue, newArray.Length - 1);
                    //setting.SetValue(platform, newArray, true);
                    arrayView.SetValue(newArray);
                    arrayView.OnValueChanged(newArray);
                }
            };

            arrayView.onMoveItem += (fromIndex, toIndex) =>
            {
                if (isList)
                {
                    //IList list = setting.GetValue(platform) as IList;
                    var tmp = list[fromIndex];

                    if (toIndex < fromIndex)
                    {
                        for (int i = fromIndex - 1; i >= toIndex; i--)
                        {
                            list[i + 1] = list[i];
                        }
                    }
                    else
                    {
                        for (int i = fromIndex + 1; i <= toIndex; i++)
                        {
                            list[i - 1] = list[i];
                        }
                    }
                    list[toIndex] = tmp;
                    //setting.SetValue(platform, list, true);
                    arrayView.SetValue(list);
                    arrayView.OnValueChanged(list);
                }
                else
                {
                    //Array array = setting.GetValue(platform) as Array;

                    var tmp = array.GetValue(fromIndex);

                    if (toIndex < fromIndex)
                    {
                        for (int i = fromIndex - 1; i >= toIndex; i--)
                        {
                            array.SetValue(array.GetValue(i), i + 1);
                        }
                    }
                    else
                    {
                        for (int i = fromIndex + 1; i <= toIndex; i++)
                        {
                            array.SetValue(array.GetValue(i), i - 1);
                        }
                    }

                    //array.SetValue(array.GetValue(toIndex), fromIndex);
                    array.SetValue(tmp, toIndex);

                    //setting.SetValue(platform, array, true);
                    arrayView.SetValue(array);
                    arrayView.OnValueChanged(array);
                }
            };

            arrayView.onRemoveItem += (index) =>
            {
                if (isList)
                {
                    //IList list = setting.GetValue(platform) as IList;
                    if (index < 0 || index >= list.Count)
                        return;
                    list.RemoveAt(index);
                    //setting.SetValue(platform, list, true);
                    arrayView.SetValue(list);
                    arrayView.OnValueChanged(list);
                }
                else
                {
                    //Array array = setting.GetValue(platform) as Array;
                    if (index < 0 || index >= array.Length)
                        return;
                    Array newArray = Array.CreateInstance(itemType, array.Length - 1);

                    Array.Copy(array, newArray, index);
                    if (array.Length - index > 1)
                    {
                        Array.Copy(array, index + 1, newArray, index, (array.Length - index) - 1);
                    }

                    //setting.SetValue(platform, newArray, true);
                    arrayView.SetValue(newArray);
                    arrayView.OnValueChanged(newArray);
                }
            };

            return arrayView;
        }


        class DragManipulator : MouseManipulator
        {
            private bool isDraging;
            private ArrayView arrayView;
            private VisualElement targetContainer;
            private Vector2 downMousePos;
            private int originIndex;
            private Vector2 originPos;
            private Vector2 originSize;

            private VisualElement dragTarget;

            private VisualElement dragTargetWrapper;
            private VisualElement dragTargetPlaceholder;
            private Vector2 mouseOffset;
            private Vector2 currentMousePos;

            public DragManipulator(ArrayView arrayView)
            {
                this.arrayView = arrayView;
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<MouseDownEvent>(OnMouseDownEvent, TrickleDown.TrickleDown);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<MouseDownEvent>(OnMouseDownEvent);
                if (targetContainer != null)
                {
                    targetContainer.UnregisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
                    targetContainer.UnregisterCallback<MouseUpEvent>(OnMouseUpEvent);
                }
            }

            VisualElement GetContainer()
            {
                return arrayView.root.Q(className: SettingsArrayPanel_ContentContainer_ClassName);
            }

            private bool captureMouse;

            void OnMouseDownEvent(MouseDownEvent e)
            {
                if (!CanStartManipulation(e))
                    return;

                var elem = e.currentTarget as VisualElement;
                if (e.button == 0)
                {

                    if (!elem.ClassListContains(SettingsArrayPanel_Item_Draggable_ClassName))
                    {
                        return;
                    }


                    if (!isDraging)
                    {
                        dragTarget = elem.parent;

                        targetContainer = GetContainer();
                        originPos = dragTarget.ChangeCoordinatesTo(targetContainer, Vector2.zero);

                        originSize = dragTarget.layout.size;
                        mouseOffset = e.localMousePosition;
                        downMousePos = ((VisualElement)e.currentTarget).ChangeCoordinatesTo(targetContainer, e.localMousePosition);
                        currentMousePos = downMousePos;


                        targetContainer.RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
                        targetContainer.RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
                        MouseCaptureController.CaptureMouse(targetContainer);


                    }

                }

            }


            void OnMouseMoveEvent(MouseMoveEvent e)
            {
                if (!isDraging)
                {
                    InitDrag();
                }

                if (isDraging)
                {
                    currentMousePos = ((VisualElement)e.currentTarget).ChangeCoordinatesTo(targetContainer, e.localMousePosition);
                    Vector2 newPos = ((VisualElement)e.currentTarget).ChangeCoordinatesTo(targetContainer, e.localMousePosition - mouseOffset);
                    if (newPos.y <= 0)
                        newPos.y = 0;
                    if (newPos.y + originSize.y > targetContainer.layout.height)
                        newPos.y = targetContainer.layout.height - originSize.y;
                    dragTargetWrapper.style.top = newPos.y;
                    UpdatePosition();
                }

            }

            void OnMouseUpEvent(MouseUpEvent e)
            {
                if (targetContainer != null)
                {
                    targetContainer.UnregisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
                    targetContainer.UnregisterCallback<MouseUpEvent>(OnMouseUpEvent);
                    MouseCaptureController.ReleaseMouse(targetContainer);
                }

                if (isDraging)
                {
                    isDraging = false;

                    if (dragTargetWrapper != null)
                    {
                        // dragTarget.RemoveFromClassList(SettingsArrayPanel_Item_Active_ClassName);
                        int index = targetContainer.IndexOf(dragTargetPlaceholder);
                        targetContainer.Insert(index, dragTarget);
                        targetContainer.Remove(dragTargetPlaceholder);

                        targetContainer.Remove(dragTargetWrapper);
                        dragTargetWrapper = null;
                        dragTargetPlaceholder = null;

                        if (index != originIndex)
                        {
                            arrayView.selectedIndex = index;
                            arrayView.onMoveItem?.Invoke(originIndex, index);
                        }
                    }
                }
            }

            void InitDrag()
            {
                isDraging = true;


                originIndex = targetContainer.IndexOf(dragTarget);

                //dragTarget.AddToClassList(SettingsArrayPanel_Item_Active_ClassName);

                dragTargetPlaceholder = new VisualElement();
                dragTargetPlaceholder.style.width = originSize.x;
                dragTargetPlaceholder.style.height = originSize.y;
                targetContainer.Insert(originIndex, dragTargetPlaceholder);

                dragTargetWrapper = new VisualElement();
                dragTargetWrapper.style.backgroundColor = Color.red;
                dragTargetWrapper.style.position = Position.Absolute;
                dragTargetWrapper.style.left = originPos.x;
                dragTargetWrapper.style.top = originPos.y;
                dragTargetWrapper.style.width = originSize.x;
                dragTargetWrapper.style.height = originSize.y;

                dragTargetWrapper.Add(dragTarget);
                targetContainer.Add(dragTargetWrapper);
            }


            void UpdatePosition()
            {
                int index = targetContainer.IndexOf(dragTargetPlaceholder);

                int newIndex = -1;
                int i = 0;
                foreach (VisualElement item in targetContainer.Children())
                {
                    if (item == dragTargetWrapper)
                        continue;
                    var layout = item.layout;
                    if (currentMousePos.y >= layout.yMin && currentMousePos.y <= layout.yMax)
                    {
                        newIndex = i;
                        break;
                    }
                    i++;
                }

                if (newIndex >= 0 && index != newIndex)
                {
                    targetContainer.Insert(newIndex, dragTargetPlaceholder);
                }

            }


        }
    }
}
