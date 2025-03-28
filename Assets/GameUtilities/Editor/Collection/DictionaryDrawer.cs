using System;
using System.Collections.Generic;
using System.Linq;
using GameUtilities.Runtime.Collection;
using Unity.Mathematics;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
// ReSharper disable CollectionNeverQueried.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable LocalVariableHidesMember
// ReSharper disable UnassignedField.Global

namespace GameUtilities.Editor.Collection
{
    #if UNITY_EDITOR
    /// <summary>
    /// Custom property drawer cho Dictionary, hiển thị UI để thêm, sửa và xóa các cặp key-value.
    /// Sử dụng với các lớp được đánh dấu bằng DictionaryDrawerAttribute.
    /// </summary>
    [CustomPropertyDrawer(typeof(DictionaryDrawerAttribute), true)]
    public class DictionaryDrawer : PropertyDrawer
    {
        /// <summary>
        /// Visual tree asset cho foldout container chính
        /// </summary>
        public VisualTreeAsset FoldoutVisualTree;
        
        /// <summary>
        /// Style sheet cho foldout container
        /// </summary>
        public StyleSheet FoldoutStyleSheet;
        
        /// <summary>
        /// Visual tree asset cho dictionary container
        /// </summary>
        public VisualTreeAsset DictionaryVisualTree;
        
        /// <summary>
        /// Style sheet cho dictionary container
        /// </summary>
        public StyleSheet DictionaryStyleSheet;
        
        /// <summary>
        /// Visual tree asset cho mỗi item trong dictionary
        /// </summary>
        public VisualTreeAsset DictionaryItemVisualTree;
        
        /// <summary>
        /// Style sheet cho mỗi item trong dictionary
        /// </summary>
        public StyleSheet DictionaryItemStyleSheet;
        
        /// <summary>
        /// Visual tree asset cho group thêm item mới
        /// </summary>
        public VisualTreeAsset AddGroupVisualTree;
        
        /// <summary>
        /// Lớp biểu diễn một mục trong dictionary với key và value
        /// </summary>
        [Serializable]
        private class Item
        {
            /// <summary>
            /// SerializedProperty cho key
            /// </summary>
            public SerializedProperty Key;
            
            /// <summary>
            /// SerializedProperty cho value
            /// </summary>
            public SerializedProperty Value;
        }
        
        /// <summary>
        /// Danh sách các items để hiển thị và làm việc với UI
        /// </summary>
        private static List<Item> _persistentItems = new List<Item>();
        
        /// <summary>
        /// Chiều cao của mỗi item để sử dụng cho việc điều chỉnh UI
        /// </summary>
        private float itemHeight;

        /// <summary>
        /// Tạo UI cho property được đánh dấu bằng DictionaryDrawerAttribute
        /// </summary>
        /// <param name="property">SerializedProperty cần vẽ UI</param>
        /// <returns>VisualElement chứa tất cả UI của dictionary</returns>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Lấy các property cần thiết
            var keyProperty = property.FindPropertyRelative("key");
            var valueProperty = property.FindPropertyRelative("value");
            var keyProperties = property.FindPropertyRelative("keys");
            var valueProperties = property.FindPropertyRelative("values");
            
            // Tạo UI cảnh báo
            var infoLabel = new Label("Key đã tồn tại!");
            
            // Tạo container chính
            var container = CreateMainContainer();
            container.RegisterCallback<PointerLeaveEvent>(evt => {
                if (container.Contains(infoLabel))
                {
                    container.Remove(infoLabel);
                }
            });
            
            // Tạo foldout
            var foldout = CreateFoldout(property.displayName);
            
            // Tạo counter field
            var countField = CreateCountField(keyProperties.arraySize);
            
            // Tạo và cấu hình dictionary view
            var dictionary = CreateDictionaryView(keyProperties, valueProperties);
            
            // Tạo các control để thêm item mới
            var addGroupBox = CreateAddControls(keyProperty, valueProperty, keyProperties, valueProperties, 
                dictionary, countField, container, infoLabel, property);
            
            // Kết hợp các thành phần UI
            container.Add(countField);
            foldout.Add(dictionary);
            foldout.Add(addGroupBox);
            container.Add(foldout);
            
            return container;
        }

        /// <summary>
        /// Tạo container chính cho toàn bộ UI
        /// </summary>
        private VisualElement CreateMainContainer()
        {
            return new VisualElement
            {
                style =
                {
                    backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.1f),
                }
            };
        }

        /// <summary>
        /// Tạo foldout container
        /// </summary>
        /// <param name="displayName">Tên hiển thị</param>
        private Foldout CreateFoldout(string displayName)
        {
            var foldout = FoldoutVisualTree.CloneTree().Q<Foldout>("FoldoutContainer");
            foldout.styleSheets.Add(FoldoutStyleSheet);
            foldout.text = displayName;
            foldout.style.fontSize = 16;
            foldout.style.unityFontStyleAndWeight = FontStyle.Bold;
            return foldout;
        }

        /// <summary>
        /// Tạo field hiển thị số lượng items
        /// </summary>
        /// <param name="count">Số lượng ban đầu</param>
        private IntegerField CreateCountField(int count)
        {
            return new IntegerField
            {
                value = count,
                style =
                {
                    position = new StyleEnum<Position>(Position.Absolute),
                    alignSelf = new StyleEnum<Align>(Align.FlexEnd),
                }
            };
        }

        /// <summary>
        /// Tạo và cấu hình MultiColumnListView cho dictionary
        /// </summary>
        /// <param name="keyProperties">SerializedProperty của keys</param>
        /// <param name="valueProperties">SerializedProperty của values</param>
        private MultiColumnListView CreateDictionaryView(SerializedProperty keyProperties, SerializedProperty valueProperties)
        {
            var dictionary = DictionaryVisualTree.CloneTree().Q<MultiColumnListView>("DictionaryContainer");
            dictionary.styleSheets.Add(DictionaryStyleSheet);
            dictionary.style.fontSize = 14;
            dictionary.style.unityFontStyleAndWeight = FontStyle.BoldAndItalic;
            
            // Khôi phục lại danh sách items
            InitializePersistentItems(keyProperties, valueProperties);
            
            // Cấu hình các columns
            ConfigureDictionaryColumns(dictionary, keyProperties, valueProperties);
            
            // Cấu hình và gán itemsSource
            dictionary.itemsSource = _persistentItems;
            dictionary.Rebuild();
            
            return dictionary;
        }

        /// <summary>
        /// Khởi tạo danh sách các items từ SerializedProperties
        /// </summary>
        private void InitializePersistentItems(SerializedProperty keyProperties, SerializedProperty valueProperties)
        {
            _persistentItems.Clear();
            for (int i = 0; i < keyProperties.arraySize; i++)
            {
                _persistentItems.Add(new Item
                {
                    Key = keyProperties.GetArrayElementAtIndex(i),
                    Value = valueProperties.GetArrayElementAtIndex(i)
                });
            }
        }

        /// <summary>
        /// Cấu hình các cột cho MultiColumnListView
        /// </summary>
        private void ConfigureDictionaryColumns(MultiColumnListView dictionary, 
            SerializedProperty keyProperties, SerializedProperty valueProperties)
        {
            // Cấu hình cột 0 (key-value)
            dictionary.columns[0].makeCell = () =>
            {
                var foldoutItem = DictionaryItemVisualTree.CloneTree().Q<Foldout>("DictionaryItemContainer");
                foldoutItem.styleSheets.Add(DictionaryItemStyleSheet);
                foldoutItem.style.alignSelf = new StyleEnum<Align>(Align.Stretch);
                return foldoutItem;
            };
            
            // Cấu hình cột 1 (delete button)
            dictionary.columns[1].makeCell = () =>
            {
                var deleteButton = DictionaryItemVisualTree.CloneTree().Q<Button>("DeleteButton");
                return deleteButton;
            };
            
            // Bind dữ liệu cho cột 0
            dictionary.columns[0].bindCell = (element, index) =>
            {
                if (index >= keyProperties.arraySize) return;
                if (element is not Foldout foldoutItem) return;
                
                foldoutItem.text = $"Item {index}";
                
                var key = foldoutItem.Q<PropertyField>("KeyField");
                key.label = "Key";
                
                var value = foldoutItem.Q<PropertyField>("ValueField");
                value.label = "Value";
                
                if (_persistentItems.Count <= index) return;
                key.BindProperty(_persistentItems[index].Key);
                value.BindProperty(_persistentItems[index].Value);
                
                itemHeight = foldoutItem.style.height.value.value;
            };
            
            // Bind dữ liệu và sự kiện cho cột 1
            dictionary.columns[1].bindCell = (element, index) =>
            {
                if (index >= keyProperties.arraySize) return;
                if (element is not Button deleteButton) return;
                
                ConfigureDeleteButton(deleteButton, index, keyProperties, valueProperties, dictionary);
            };
        }

        /// <summary>
        /// Cấu hình nút xóa và sự kiện click
        /// </summary>
        private void ConfigureDeleteButton(Button deleteButton, int index, 
            SerializedProperty keyProperties, SerializedProperty valueProperties, 
            MultiColumnListView dictionary)
        {
            deleteButton.clicked += () =>
            {
                // Kiểm tra index hợp lệ trước khi xóa
                if (index < 0 || index >= _persistentItems.Count) return;

                try 
                {
                    // Xóa item khỏi danh sách persistent
                    _persistentItems.RemoveAt(index);

                    // Xóa phần tử tương ứng trong SerializedProperty
                    keyProperties.DeleteArrayElementAtIndex(index);
                    valueProperties.DeleteArrayElementAtIndex(index);

                    // Cập nhật kích thước mảng
                    keyProperties.arraySize = _persistentItems.Count;
                    valueProperties.arraySize = _persistentItems.Count;
                    
                    // Cập nhật UI
                    dictionary.itemsSource = _persistentItems;
                    dictionary.Rebuild();

                    // Lưu các thay đổi
                    keyProperties.serializedObject.ApplyModifiedProperties();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Lỗi khi xóa item: {ex.Message}");
                }
            };
        }

        /// <summary>
        /// Tạo các điều khiển để thêm item mới
        /// </summary>
        private VisualElement CreateAddControls(SerializedProperty keyProperty, SerializedProperty valueProperty,
            SerializedProperty keyProperties, SerializedProperty valueProperties,
            MultiColumnListView dictionary, IntegerField countField, 
            VisualElement container, Label infoLabel, SerializedProperty property)
        {
            var addGroupBox = AddGroupVisualTree.CloneTree().Q<GroupBox>("AddGroupContainer");
            var keyField = addGroupBox.Q<PropertyField>("KeyField");
            keyField.BindProperty(keyProperty);
            var valueField = addGroupBox.Q<PropertyField>("ValueField");
            valueField.BindProperty(valueProperty);
            var addButton = addGroupBox.Q<Button>("AddButton");

            // Cấu hình sự kiện click cho nút thêm
            addButton.clicked += () => AddNewItem(keyProperty, valueProperty, keyProperties, valueProperties,
                dictionary, countField, container, infoLabel, property);
            
            return addGroupBox;
        }

        /// <summary>
        /// Thêm một item mới vào dictionary
        /// </summary>
        private void AddNewItem(SerializedProperty keyProperty, SerializedProperty valueProperty,
            SerializedProperty keyProperties, SerializedProperty valueProperties,
            MultiColumnListView dictionary, IntegerField countField, 
            VisualElement container, Label infoLabel, SerializedProperty property)
        {
            keyProperties.arraySize++;
            valueProperties.arraySize++;

            var newKeyProperty = keyProperties.GetArrayElementAtIndex(keyProperties.arraySize - 1);
            var newValueProperty = valueProperties.GetArrayElementAtIndex(valueProperties.arraySize - 1);
            
            AssignValue(newKeyProperty, keyProperty);
            AssignValue(newValueProperty, valueProperty);
            
            var newItem = new Item
            {
                Key = newKeyProperty,
                Value = newValueProperty,
            };
            
            // Kiểm tra key đã tồn tại
            if (_persistentItems.Any(item => CheckValue(item.Key, newKeyProperty)))
            {
                keyProperties.arraySize = _persistentItems.Count;
                valueProperties.arraySize = _persistentItems.Count;
                container.Add(infoLabel);
                return;
            }

            // Xóa thông báo lỗi nếu có
            if (container.Contains(infoLabel))
            {
                container.Remove(infoLabel);
            }
            
            // Thêm item mới và cập nhật UI
            _persistentItems.Add(newItem);
            dictionary.itemsSource = _persistentItems;
            dictionary.Rebuild();
            countField.value = keyProperties.arraySize;
            
            // Lưu thay đổi
            property.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Kiểm tra xem hai giá trị có giống nhau không
        /// </summary>
        /// <param name="valueFirst">SerializedProperty thứ nhất</param>
        /// <param name="valueSecond">SerializedProperty thứ hai</param>
        /// <returns>True nếu hai giá trị giống nhau</returns>
        private static bool CheckValue(SerializedProperty valueFirst, SerializedProperty valueSecond)
        {
            switch (valueFirst.propertyType)
            {
                case SerializedPropertyType.Generic:
                    return false;
                case SerializedPropertyType.Integer:
                    return valueFirst.intValue == valueSecond.intValue;
                case SerializedPropertyType.Boolean:
                    return valueFirst.boolValue == valueSecond.boolValue;
                case SerializedPropertyType.Float:
                    return Mathf.Approximately(valueFirst.floatValue, valueSecond.floatValue);
                case SerializedPropertyType.String:
                    return valueFirst.stringValue == valueSecond.stringValue;
                case SerializedPropertyType.Color:
                    return valueFirst.colorValue == valueSecond.colorValue;
                case SerializedPropertyType.ObjectReference:
                    return valueFirst.objectReferenceValue == valueSecond.objectReferenceValue;
                case SerializedPropertyType.LayerMask:
                    return false;
                case SerializedPropertyType.Enum:
                    return valueFirst.enumValueIndex == valueSecond.enumValueIndex;
                case SerializedPropertyType.Vector2:
                    return valueFirst.vector2Value == valueSecond.vector2Value;
                case SerializedPropertyType.Vector3:
                    return valueFirst.vector3Value == valueSecond.vector3Value;
                case SerializedPropertyType.Vector4:
                    return valueFirst.vector4Value == valueSecond.vector4Value;
                case SerializedPropertyType.Rect:
                    return valueFirst.rectValue == valueSecond.rectValue;
                case SerializedPropertyType.ArraySize:
                    return valueFirst.arraySize == valueSecond.arraySize;
                case SerializedPropertyType.Character:
                    return false;
                case SerializedPropertyType.AnimationCurve:
                    return valueFirst.animationCurveValue.Equals(valueSecond.animationCurveValue);
                case SerializedPropertyType.Bounds:
                    return valueFirst.boundsValue == valueSecond.boundsValue;
                case SerializedPropertyType.Gradient:
                    return valueFirst.gradientValue.Equals(valueSecond.gradientValue);
                case SerializedPropertyType.Quaternion:
                    return valueFirst.quaternionValue == valueSecond.quaternionValue;
                case SerializedPropertyType.ExposedReference:
                    return valueFirst.exposedReferenceValue == valueSecond.exposedReferenceValue;
                case SerializedPropertyType.FixedBufferSize:
                    return valueFirst.fixedBufferSize == valueSecond.fixedBufferSize;
                case SerializedPropertyType.Vector2Int:
                    return valueFirst.vector2IntValue == valueSecond.vector2IntValue;
                case SerializedPropertyType.Vector3Int:
                    return valueFirst.vector3IntValue == valueSecond.vector3IntValue;
                case SerializedPropertyType.RectInt:
                    return valueFirst.rectIntValue == valueSecond.rectIntValue;
                case SerializedPropertyType.BoundsInt:
                    return valueFirst.boundsIntValue == valueSecond.boundsIntValue;
                case SerializedPropertyType.ManagedReference:
                    return valueFirst.managedReferenceValue == valueSecond.managedReferenceValue;
                case SerializedPropertyType.Hash128:
                    return valueFirst.hash128Value == valueSecond.hash128Value;
                case SerializedPropertyType.RenderingLayerMask:
                    return false;
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// Gán giá trị từ SerializedProperty nguồn sang đích
        /// </summary>
        /// <param name="valueFirst">SerializedProperty đích</param>
        /// <param name="valueSecond">SerializedProperty nguồn</param>
        private static void AssignValue(SerializedProperty valueFirst, SerializedProperty valueSecond)
        {
            switch (valueFirst.propertyType)
            {
                case SerializedPropertyType.Generic:
                    break;
                case SerializedPropertyType.Integer:
                    valueFirst.intValue = valueSecond.intValue;
                    break;
                case SerializedPropertyType.Boolean:
                    valueFirst.boolValue = valueSecond.boolValue;
                    break;
                case SerializedPropertyType.Float:
                    valueFirst.floatValue = valueSecond.floatValue;
                    break;
                case SerializedPropertyType.String:
                    valueFirst.stringValue = valueSecond.stringValue;
                    break;
                case SerializedPropertyType.Color:
                    valueFirst.colorValue = valueSecond.colorValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    valueFirst.objectReferenceValue = valueSecond.objectReferenceValue;
                    break;
                case SerializedPropertyType.LayerMask:
                    break;
                case SerializedPropertyType.Enum:
                    Debug.LogWarning($"{valueFirst.enumValueIndex}:{valueSecond.enumValueIndex}");
                    valueFirst.enumValueIndex = valueSecond.enumValueIndex;
                    break;
                case SerializedPropertyType.Vector2:
                    valueFirst.vector2Value = valueSecond.vector2Value;
                    break;
                case SerializedPropertyType.Vector3:
                    valueFirst.vector3Value = valueSecond.vector3Value;
                    break;
                case SerializedPropertyType.Vector4:
                    valueFirst.vector4Value = valueSecond.vector4Value;
                    break;
                case SerializedPropertyType.Rect:
                    valueFirst.rectValue = valueSecond.rectValue;
                    break;
                case SerializedPropertyType.ArraySize:
                    valueFirst.arraySize = valueSecond.arraySize;
                    break;
                case SerializedPropertyType.Character:
                    valueFirst.stringValue = valueSecond.stringValue;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    valueFirst.animationCurveValue = valueSecond.animationCurveValue;
                    break;
                case SerializedPropertyType.Bounds:
                    valueFirst.boundsValue = valueSecond.boundsValue;
                    break;
                case SerializedPropertyType.Gradient:
                    valueFirst.gradientValue = valueSecond.gradientValue;
                    break;
                case SerializedPropertyType.Quaternion:
                    valueFirst.quaternionValue = valueSecond.quaternionValue;
                    break;
                case SerializedPropertyType.ExposedReference:
                    valueFirst.exposedReferenceValue = valueSecond.exposedReferenceValue;
                    break;
                case SerializedPropertyType.FixedBufferSize:
                    break;
                case SerializedPropertyType.Vector2Int:
                    valueFirst.vector2IntValue = valueSecond.vector2IntValue;
                    break;
                case SerializedPropertyType.Vector3Int:
                    valueFirst.vector3IntValue = valueSecond.vector3IntValue;
                    break;
                case SerializedPropertyType.RectInt:
                    valueFirst.rectIntValue = valueSecond.rectIntValue;
                    break;
                case SerializedPropertyType.BoundsInt:
                    valueFirst.boundsIntValue = valueSecond.boundsIntValue;
                    break;
                case SerializedPropertyType.ManagedReference:
                    valueFirst.managedReferenceValue = valueSecond.managedReferenceValue;
                    break;
                case SerializedPropertyType.Hash128:
                    valueFirst.hash128Value = valueSecond.hash128Value;
                    break;
                case SerializedPropertyType.RenderingLayerMask:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    #endif
}