using System;
using System.Collections.Generic;
using System.Linq;
using GameUtilities.Runtime.Collection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
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
        /// Chiều cao của mỗi item để sử dụng cho việc điều chỉnh UI
        /// </summary>
        private float _itemHeight;
        
        /// <summary>
        /// Flag đánh dấu trạng thái kéo thả
        /// </summary>
        private bool _isDragging;
        
        /// <summary>
        /// Chỉ số của item đang được kéo
        /// </summary>
        private int _draggedItemIndex = -1;
        
        /// <summary>
        /// Item đang được kéo thả
        /// </summary>
        private Item _draggedItem;

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
            
            // Khởi tạo danh sách items từ properties
            var items = InitializeItems(keyProperties, valueProperties);
            
            // Tạo các thành phần UI và binding dữ liệu
            return CreateDictionaryUI(property, keyProperty, valueProperty, keyProperties, valueProperties, items);
        }

        /// <summary>
        /// Khởi tạo danh sách các items từ SerializedProperties
        /// </summary>
        private List<Item> InitializeItems(SerializedProperty keyProperties, SerializedProperty valueProperties)
        {
            var items = new List<Item>();
            for (int i = 0; i < keyProperties.arraySize; i++)
            {
                items.Add(new Item
                {
                    Key = keyProperties.GetArrayElementAtIndex(i),
                    Value = valueProperties.GetArrayElementAtIndex(i)
                });
            }
            return items;
        }

        /// <summary>
        /// Tạo toàn bộ UI cho Dictionary
        /// </summary>
        private VisualElement CreateDictionaryUI(
            SerializedProperty property,
            SerializedProperty keyProperty, 
            SerializedProperty valueProperty,
            SerializedProperty keyProperties, 
            SerializedProperty valueProperties,
            List<Item> items)
        {
            // Tạo UI cảnh báo
            var infoLabel = new Label("Key đã tồn tại!");
            
            // Tạo container chính
            var container = CreateMainContainer();
            container.RegisterCallback<PointerLeaveEvent>(OnPointerLeaveContainer);
            
            // Tạo foldout
            var foldout = CreateFoldout(property.displayName);
            
            // Tạo counter field
            var countField = CreateCountField(keyProperties.arraySize);
            
            // Tạo và cấu hình dictionary view
            var dictionary = CreateDictionaryView(container, infoLabel, keyProperties, valueProperties, items, countField);
            
            // Tạo các control để thêm item mới
            var addContext = new AddItemContext
            {
                KeyProperty = keyProperty,
                ValueProperty = valueProperty,
                KeyProperties = keyProperties,
                ValueProperties = valueProperties,
                Dictionary = dictionary,
                CountField = countField,
                Container = container,
                InfoLabel = infoLabel,
                Property = property,
                Items = items
            };
            
            var addGroupBox = CreateAddControls(addContext);
            
            // Kết hợp các thành phần UI
            container.Add(countField);
            foldout.Add(dictionary);
            foldout.Add(addGroupBox);
            container.Add(foldout);
            
            return container;
            
            void OnPointerLeaveContainer(PointerLeaveEvent evt)
            {
                if (container.Contains(infoLabel))
                {
                    container.Remove(infoLabel);
                }
            }
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
        private MultiColumnListView CreateDictionaryView(
            VisualElement container,
            Label infoLabel,
            SerializedProperty keyProperties, 
            SerializedProperty valueProperties,
            List<Item> items, IntegerField countField)
        {
            var dictionary = DictionaryVisualTree.CloneTree().Q<MultiColumnListView>("DictionaryContainer");
            dictionary.styleSheets.Add(DictionaryStyleSheet);
            dictionary.style.fontSize = 14;
            dictionary.style.unityFontStyleAndWeight = FontStyle.BoldAndItalic;
            
            // Cấu hình các columns
            ConfigureDictionaryColumns(container, infoLabel,dictionary, keyProperties, valueProperties, items, countField);
            
            // Cấu hình và gán itemsSource
            dictionary.itemsSource = items;
            dictionary.Rebuild();
            
            return dictionary;
        }

        /// <summary>
        /// Cấu hình các cột cho MultiColumnListView
        /// </summary>
        private void ConfigureDictionaryColumns(
            VisualElement container,
            Label infoLabel,
            MultiColumnListView dictionary, 
            SerializedProperty keyProperties, 
            SerializedProperty valueProperties,
            List<Item> items, IntegerField countField)
        {
            // Cấu hình cột 1 (key-value)
            dictionary.columns[1].makeCell = () =>
            {
                var foldoutItem = DictionaryItemVisualTree.CloneTree().Q<Foldout>("DictionaryItemContainer");
                foldoutItem.styleSheets.Add(DictionaryItemStyleSheet);
                foldoutItem.style.alignSelf = new StyleEnum<Align>(Align.Stretch);
                return foldoutItem;
            };
            
            // Cấu hình cột 2 (delete button)
            dictionary.columns[2].makeCell = () =>
            {
                var deleteButton = DictionaryItemVisualTree.CloneTree().Q<Button>("DeleteButton");
                return deleteButton;
            };
            
            // Bind dữ liệu cho cột 1
            dictionary.columns[1].bindCell = (element, index) =>
            {
                if (index >= keyProperties.arraySize) return;
                if (element is not Foldout foldoutItem) return;
                
                foldoutItem.text = $"Item {index}";
                
                var key = foldoutItem.Q<PropertyField>("KeyField");
                key.label = "Key";
                
                var value = foldoutItem.Q<PropertyField>("ValueField");
                value.label = "Value";
                
                if (items.Count <= index) return;
                key.BindProperty(items[index].Key);
                value.BindProperty(items[index].Value);
                
                _itemHeight = foldoutItem.style.height.value.value;
            };
            
            // Bind dữ liệu và sự kiện cho cột 2
            dictionary.columns[2].bindCell = (element, index) =>
            {
                if (index >= keyProperties.arraySize) return;
                if (element is not Button deleteButton) return;
                
                var deleteContext = new DeleteItemContext
                {
                    Index = index,
                    KeyProperties = keyProperties,
                    ValueProperties = valueProperties,
                    Container = container,
                    InfoLabel = infoLabel,
                    Dictionary = dictionary,
                    Items = items,
                    CountField = countField
                };
                
                ConfigureDeleteButton(deleteButton, deleteContext);
            };
        }

        /// <summary>
        /// Context cho thao tác xóa item
        /// </summary>
        private class DeleteItemContext
        {
            public int Index { get; set; }
            public SerializedProperty KeyProperties { get; set; }
            public SerializedProperty ValueProperties { get; set; }
            public VisualElement Container { get; set; }
            public Label InfoLabel { get; set; }
            public MultiColumnListView Dictionary { get; set; }
            public List<Item> Items { get; set; }
            public IntegerField CountField { get; set; }
        }

        /// <summary>
        /// Cấu hình nút xóa và sự kiện click
        /// </summary>
        private void ConfigureDeleteButton(Button deleteButton, DeleteItemContext context)
        {
            deleteButton.clicked += () =>
            {
                // Kiểm tra index hợp lệ trước khi xóa
                if (context.Index < 0 || context.Index >= context.KeyProperties.arraySize) return;
                try 
                {
                    DeleteItem(context);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Lỗi khi xóa item: {ex.Message}");
                }
            };
        }

        /// <summary>
        /// Xóa một item từ dictionary
        /// </summary>
        private void DeleteItem(DeleteItemContext context)
        {
            // Cập nhật serializedObject trước khi thực hiện thay đổi
            context.KeyProperties.serializedObject.Update();
            context.ValueProperties.serializedObject.Update();
            
            // Lưu trữ kích thước hiện tại
            int currentSize = context.KeyProperties.arraySize;
    
            if (context.Index < currentSize - 1)
            {
                // Chỉ di chuyển nếu không phải phần tử cuối cùng
                context.KeyProperties.MoveArrayElement(context.Index, currentSize - 1);
                context.ValueProperties.MoveArrayElement(context.Index, currentSize - 1);
        
                // Áp dụng thay đổi sau khi di chuyển
                context.KeyProperties.serializedObject.ApplyModifiedProperties();
                context.ValueProperties.serializedObject.ApplyModifiedProperties();
            }
            
            // Xóa phần tử cuối cùng
            context.KeyProperties.arraySize = currentSize - 1;
            context.ValueProperties.arraySize = currentSize - 1;
    
            // Áp dụng thay đổi sau khi xóa
            context.KeyProperties.serializedObject.ApplyModifiedProperties();
            context.ValueProperties.serializedObject.ApplyModifiedProperties();
    
            // Cập nhật lại danh sách items sau khi xóa
            context.Items.Clear();
            for (int i = 0; i < context.KeyProperties.arraySize; i++)
            {
                context.Items.Add(new Item
                {
                    Key = context.KeyProperties.GetArrayElementAtIndex(i),
                    Value = context.ValueProperties.GetArrayElementAtIndex(i)
                });
            }
    
            // Cập nhật UI
            // Xóa thông báo lỗi nếu có
            if (context.Container.Contains(context.InfoLabel))
            {
                context.Container.Remove(context.InfoLabel);
            }
            context.CountField.value = context.KeyProperties.arraySize;
            context.Dictionary.itemsSource = context.Items;
            context.Dictionary.Rebuild();
        }

        /// <summary>
        /// Context cho thao tác thêm item mới
        /// </summary>
        private class AddItemContext
        {
            public SerializedProperty KeyProperty { get; set; }
            public SerializedProperty ValueProperty { get; set; }
            public SerializedProperty KeyProperties { get; set; }
            public SerializedProperty ValueProperties { get; set; }
            public MultiColumnListView Dictionary { get; set; }
            public IntegerField CountField { get; set; }
            public VisualElement Container { get; set; }
            public Label InfoLabel { get; set; }
            public SerializedProperty Property { get; set; }
            public List<Item> Items { get; set; }
        }

        /// <summary>
        /// Tạo các điều khiển để thêm item mới
        /// </summary>
        private VisualElement CreateAddControls(AddItemContext context)
        {
            var addGroupBox = AddGroupVisualTree.CloneTree().Q<GroupBox>("AddGroupContainer");
            var keyField = addGroupBox.Q<PropertyField>("KeyField");
            keyField.label = "Key";
            keyField.BindProperty(context.KeyProperty);
            var valueField = addGroupBox.Q<PropertyField>("ValueField");
            valueField.label = "Value";
            valueField.BindProperty(context.ValueProperty);
            var addButton = addGroupBox.Q<Button>("AddButton");

            // Cấu hình sự kiện click cho nút thêm
            addButton.clicked += () => AddNewItem(context);
            
            return addGroupBox;
        }

        /// <summary>
        /// Thêm một item mới vào dictionary
        /// </summary>
        private void AddNewItem(AddItemContext context)
        {
            context.KeyProperties.arraySize++;
            context.ValueProperties.arraySize++;

            var newKeyProperty = context.KeyProperties.GetArrayElementAtIndex(context.KeyProperties.arraySize - 1);
            var newValueProperty = context.ValueProperties.GetArrayElementAtIndex(context.ValueProperties.arraySize - 1);
            
            CopyPropertyValue(context.KeyProperty, newKeyProperty);
            CopyPropertyValue(context.ValueProperty, newValueProperty);
            
            var newItem = new Item
            {
                Key = newKeyProperty,
                Value = newValueProperty,
            };
            
            // Kiểm tra key đã tồn tại
            if (context.Items.Any(item => ArePropertiesEqual(item.Key, newKeyProperty)))
            {
                context.KeyProperties.arraySize = context.Items.Count;
                context.ValueProperties.arraySize = context.Items.Count;
                context.Container.Add(context.InfoLabel);
                // Lưu thay đổi
                context.Property.serializedObject.ApplyModifiedProperties();
                return;
            }

            // Xóa thông báo lỗi nếu có
            if (context.Container.Contains(context.InfoLabel))
            {
                context.Container.Remove(context.InfoLabel);
            }
            
            // Thêm item mới và cập nhật UI
            context.Items.Add(newItem);
            context.Dictionary.itemsSource = context.Items;
            context.Dictionary.Rebuild();
            context.CountField.value = context.KeyProperties.arraySize;
            
            // Lưu thay đổi
            context.Property.serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// Kiểm tra xem một SerializedProperty có phải là con của SerializedProperty khác không
        /// </summary>
        private static bool IsChildOf(SerializedProperty childProperty, SerializedProperty parentProperty)
        {
            if (childProperty == null || parentProperty == null)
                return false;
            
            var childPath = childProperty.propertyPath;
            var parentPath = parentProperty.propertyPath;

            return childPath.StartsWith(parentPath + ".") && childPath != parentPath;
        }

        /// <summary>
        /// Kiểm tra xem hai giá trị có giống nhau không
        /// </summary>
        private static bool ArePropertiesEqual(SerializedProperty firstProperty, SerializedProperty secondProperty)
        {
            return firstProperty.propertyType switch
            {
                SerializedPropertyType.Integer => firstProperty.intValue == secondProperty.intValue,
                SerializedPropertyType.Boolean => firstProperty.boolValue == secondProperty.boolValue,
                SerializedPropertyType.Float => Mathf.Approximately(firstProperty.floatValue, secondProperty.floatValue),
                SerializedPropertyType.String => firstProperty.stringValue == secondProperty.stringValue,
                SerializedPropertyType.Color => firstProperty.colorValue == secondProperty.colorValue,
                SerializedPropertyType.ObjectReference => firstProperty.objectReferenceValue == secondProperty.objectReferenceValue,
                SerializedPropertyType.Enum => firstProperty.enumValueIndex == secondProperty.enumValueIndex,
                SerializedPropertyType.Vector2 => firstProperty.vector2Value == secondProperty.vector2Value,
                SerializedPropertyType.Vector3 => firstProperty.vector3Value == secondProperty.vector3Value,
                SerializedPropertyType.Vector4 => firstProperty.vector4Value == secondProperty.vector4Value,
                SerializedPropertyType.Rect => firstProperty.rectValue == secondProperty.rectValue,
                SerializedPropertyType.ArraySize => firstProperty.arraySize == secondProperty.arraySize,
                SerializedPropertyType.AnimationCurve => firstProperty.animationCurveValue.Equals(secondProperty.animationCurveValue),
                SerializedPropertyType.Bounds => firstProperty.boundsValue == secondProperty.boundsValue,
                SerializedPropertyType.Quaternion => firstProperty.quaternionValue == secondProperty.quaternionValue,
                SerializedPropertyType.ExposedReference => firstProperty.exposedReferenceValue == secondProperty.exposedReferenceValue,
                SerializedPropertyType.FixedBufferSize => firstProperty.fixedBufferSize == secondProperty.fixedBufferSize,
                SerializedPropertyType.Vector2Int => firstProperty.vector2IntValue == secondProperty.vector2IntValue,
                SerializedPropertyType.Vector3Int => firstProperty.vector3IntValue == secondProperty.vector3IntValue,
                SerializedPropertyType.RectInt => firstProperty.rectIntValue == secondProperty.rectIntValue,
                SerializedPropertyType.BoundsInt => firstProperty.boundsIntValue == secondProperty.boundsIntValue,
                SerializedPropertyType.Hash128 => firstProperty.hash128Value == secondProperty.hash128Value,
                _ => false
            };
        }
        
        /// <summary>
        /// Sao chép giá trị từ một SerializedProperty sang SerializedProperty khác
        /// </summary>
        private static void CopyPropertyValue(SerializedProperty sourceProperty, SerializedProperty targetProperty)
        {
            switch (sourceProperty.propertyType)
            {
                case SerializedPropertyType.Generic:
                    CopyGenericProperty(sourceProperty, targetProperty);
                    break;
                case SerializedPropertyType.Integer:
                    targetProperty.intValue = sourceProperty.intValue;
                    break;
                case SerializedPropertyType.Boolean:
                    targetProperty.boolValue = sourceProperty.boolValue;
                    break;
                case SerializedPropertyType.Float:
                    targetProperty.floatValue = sourceProperty.floatValue;
                    break;
                case SerializedPropertyType.String:
                    targetProperty.stringValue = sourceProperty.stringValue;
                    break;
                case SerializedPropertyType.Color:
                    targetProperty.colorValue = sourceProperty.colorValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    targetProperty.objectReferenceValue = sourceProperty.objectReferenceValue;
                    break;
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.RenderingLayerMask:
                    targetProperty.intValue = sourceProperty.intValue;
                    break;
                case SerializedPropertyType.Enum:
                    targetProperty.enumValueIndex = sourceProperty.enumValueIndex;
                    break;
                case SerializedPropertyType.Vector2:
                    targetProperty.vector2Value = sourceProperty.vector2Value;
                    break;
                case SerializedPropertyType.Vector3:
                    targetProperty.vector3Value = sourceProperty.vector3Value;
                    break;
                case SerializedPropertyType.Vector4:
                    targetProperty.vector4Value = sourceProperty.vector4Value;
                    break;
                case SerializedPropertyType.Rect:
                    targetProperty.rectValue = sourceProperty.rectValue;
                    break;
                case SerializedPropertyType.ArraySize:
                    if (sourceProperty.isArray && targetProperty.isArray)
                    {
                        targetProperty.arraySize = sourceProperty.arraySize;
                    }
                    break;
                case SerializedPropertyType.Character:
                    targetProperty.stringValue = sourceProperty.stringValue;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    targetProperty.animationCurveValue = sourceProperty.animationCurveValue;
                    break;
                case SerializedPropertyType.Bounds:
                    targetProperty.boundsValue = sourceProperty.boundsValue;
                    break;
                case SerializedPropertyType.Gradient:
                    try { targetProperty.gradientValue = sourceProperty.gradientValue; }
                    catch { /* Bỏ qua nếu có lỗi với Gradient */ }
                    break;
                case SerializedPropertyType.Quaternion:
                    targetProperty.quaternionValue = sourceProperty.quaternionValue;
                    break;
                case SerializedPropertyType.ExposedReference:
                    targetProperty.exposedReferenceValue = sourceProperty.exposedReferenceValue;
                    break;
                case SerializedPropertyType.Vector2Int:
                    targetProperty.vector2IntValue = sourceProperty.vector2IntValue;
                    break;
                case SerializedPropertyType.Vector3Int:
                    targetProperty.vector3IntValue = sourceProperty.vector3IntValue;
                    break;
                case SerializedPropertyType.RectInt:
                    targetProperty.rectIntValue = sourceProperty.rectIntValue;
                    break;
                case SerializedPropertyType.BoundsInt:
                    targetProperty.boundsIntValue = sourceProperty.boundsIntValue;
                    break;
                case SerializedPropertyType.ManagedReference:
                    try { targetProperty.managedReferenceValue = sourceProperty.managedReferenceValue; }
                    catch { /* Bỏ qua nếu có lỗi với ManagedReference */ }
                    break;
                case SerializedPropertyType.Hash128:
                    targetProperty.hash128Value = sourceProperty.hash128Value;
                    break;
            }
        }

        /// <summary>
        /// Xử lý sao chép đặc biệt cho kiểu Generic
        /// </summary>
        private static void CopyGenericProperty(SerializedProperty sourceProperty, SerializedProperty targetProperty)
        {
            if (sourceProperty.isArray && targetProperty.isArray)
            {
                // Xử lý mảng
                targetProperty.arraySize = sourceProperty.arraySize;
                
                for (int i = 0; i < sourceProperty.arraySize; i++)
                {
                    var sourceElement = sourceProperty.GetArrayElementAtIndex(i);
                    var targetElement = targetProperty.GetArrayElementAtIndex(i);
                    CopyPropertyValue(sourceElement, targetElement);
                }
                
                sourceProperty.serializedObject.ApplyModifiedProperties();
                targetProperty.serializedObject.ApplyModifiedProperties();
            }
            else
            {
                // Xử lý các thuộc tính con của kiểu generic
                var sourceIterator = sourceProperty.Copy();
                var startPath = sourceProperty.propertyPath;
                var endPath = startPath;
                
                // Đi đến property tiếp theo để bắt đầu duyệt con
                if (!sourceIterator.NextVisible(true)) return;
                
                do
                {
                    // Dừng nếu đã ra khỏi phạm vi của property cha
                    if (!sourceIterator.propertyPath.StartsWith(startPath + ".") && sourceIterator.propertyPath != startPath)
                        break;
                    
                    // Bỏ qua property ban đầu
                    if (sourceIterator.propertyPath == startPath)
                        continue;
                    
                    // Lấy đường dẫn tương đối
                    string relativePath = sourceIterator.propertyPath.Substring(startPath.Length + 1);
                    
                    // Tìm property tương ứng trong target
                    var targetSubProperty = targetProperty.FindPropertyRelative(relativePath);
                    if (targetSubProperty != null)
                    {
                        CopyPropertyValue(sourceIterator, targetSubProperty);
                    }
                } 
                while (sourceIterator.NextVisible(false));
            }
        }
    }
    #endif
}


// using System;
// using System.Collections.Generic;
// using System.Linq;
// using GameUtilities.Runtime.Collection;
// using Unity.Mathematics;
// using Unity.Properties;
// using UnityEditor;
// using UnityEditor.UIElements;
// using UnityEngine;
// using UnityEngine.UIElements;
// // ReSharper disable CollectionNeverQueried.Local
// // ReSharper disable FieldCanBeMadeReadOnly.Local
// // ReSharper disable LocalVariableHidesMember
// // ReSharper disable UnassignedField.Global
//
// namespace GameUtilities.Editor.Collection
// {
//     #if UNITY_EDITOR
//     /// <summary>
//     /// Custom property drawer cho Dictionary, hiển thị UI để thêm, sửa và xóa các cặp key-value.
//     /// Sử dụng với các lớp được đánh dấu bằng DictionaryDrawerAttribute.
//     /// </summary>
//     [CustomPropertyDrawer(typeof(DictionaryDrawerAttribute), true)]
//     public class DictionaryDrawer : PropertyDrawer
//     {
//         /// <summary>
//         /// Visual tree asset cho foldout container chính
//         /// </summary>
//         public VisualTreeAsset FoldoutVisualTree;
//         
//         /// <summary>
//         /// Style sheet cho foldout container
//         /// </summary>
//         public StyleSheet FoldoutStyleSheet;
//         
//         /// <summary>
//         /// Visual tree asset cho dictionary container
//         /// </summary>
//         public VisualTreeAsset DictionaryVisualTree;
//         
//         /// <summary>
//         /// Style sheet cho dictionary container
//         /// </summary>
//         public StyleSheet DictionaryStyleSheet;
//         
//         /// <summary>
//         /// Visual tree asset cho mỗi item trong dictionary
//         /// </summary>
//         public VisualTreeAsset DictionaryItemVisualTree;
//         
//         /// <summary>
//         /// Style sheet cho mỗi item trong dictionary
//         /// </summary>
//         public StyleSheet DictionaryItemStyleSheet;
//         
//         /// <summary>
//         /// Visual tree asset cho group thêm item mới
//         /// </summary>
//         public VisualTreeAsset AddGroupVisualTree;
//         
//         /// <summary>
//         /// Lớp biểu diễn một mục trong dictionary với key và value
//         /// </summary>
//         [Serializable]
//         private class Item
//         {
//             /// <summary>
//             /// SerializedProperty cho key
//             /// </summary>
//             public SerializedProperty Key;
//             
//             /// <summary>
//             /// SerializedProperty cho value
//             /// </summary>
//             public SerializedProperty Value;
//         }
//         
//         /// <summary>
//         /// Danh sách các items để hiển thị và làm việc với UI
//         /// </summary>
//         private static List<Item> _persistentItems = new List<Item>();
//         
//         /// <summary>
//         /// Chiều cao của mỗi item để sử dụng cho việc điều chỉnh UI
//         /// </summary>
//         private float itemHeight;
//
//         /// <summary>
//         /// Tạo UI cho property được đánh dấu bằng DictionaryDrawerAttribute
//         /// </summary>
//         /// <param name="property">SerializedProperty cần vẽ UI</param>
//         /// <returns>VisualElement chứa tất cả UI của dictionary</returns>
//         public override VisualElement CreatePropertyGUI(SerializedProperty property)
//         {
//             // Lấy các property cần thiết
//             var keyProperty = property.FindPropertyRelative("key");
//             var valueProperty = property.FindPropertyRelative("value");
//             var keyProperties = property.FindPropertyRelative("keys");
//             var valueProperties = property.FindPropertyRelative("values");
//             
//             // Tạo UI cảnh báo
//             var infoLabel = new Label("Key đã tồn tại!");
//             
//             // Tạo container chính
//             var container = CreateMainContainer();
//             container.RegisterCallback<PointerLeaveEvent>(evt => {
//                 if (container.Contains(infoLabel))
//                 {
//                     container.Remove(infoLabel);
//                 }
//             });
//             
//             // Tạo foldout
//             var foldout = CreateFoldout(property.displayName);
//             
//             // Tạo counter field
//             var countField = CreateCountField(keyProperties.arraySize);
//             
//             // Tạo và cấu hình dictionary view
//             var dictionary = CreateDictionaryView(keyProperties, valueProperties);
//             
//             // Tạo các control để thêm item mới
//             var addGroupBox = CreateAddControls(keyProperty, valueProperty, keyProperties, valueProperties, 
//                 dictionary, countField, container, infoLabel, property);
//             
//             // Kết hợp các thành phần UI
//             container.Add(countField);
//             foldout.Add(dictionary);
//             foldout.Add(addGroupBox);
//             container.Add(foldout);
//             
//             return container;
//         }
//
//         /// <summary>
//         /// Tạo container chính cho toàn bộ UI
//         /// </summary>
//         private VisualElement CreateMainContainer()
//         {
//             return new VisualElement
//             {
//                 style =
//                 {
//                     backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.1f),
//                 }
//             };
//         }
//
//         /// <summary>
//         /// Tạo foldout container
//         /// </summary>
//         /// <param name="displayName">Tên hiển thị</param>
//         private Foldout CreateFoldout(string displayName)
//         {
//             var foldout = FoldoutVisualTree.CloneTree().Q<Foldout>("FoldoutContainer");
//             foldout.styleSheets.Add(FoldoutStyleSheet);
//             foldout.text = displayName;
//             foldout.style.fontSize = 16;
//             foldout.style.unityFontStyleAndWeight = FontStyle.Bold;
//             return foldout;
//         }
//
//         /// <summary>
//         /// Tạo field hiển thị số lượng items
//         /// </summary>
//         /// <param name="count">Số lượng ban đầu</param>
//         private IntegerField CreateCountField(int count)
//         {
//             return new IntegerField
//             {
//                 value = count,
//                 style =
//                 {
//                     position = new StyleEnum<Position>(Position.Absolute),
//                     alignSelf = new StyleEnum<Align>(Align.FlexEnd),
//                 }
//             };
//         }
//
//         /// <summary>
//         /// Tạo và cấu hình MultiColumnListView cho dictionary
//         /// </summary>
//         /// <param name="keyProperties">SerializedProperty của keys</param>
//         /// <param name="valueProperties">SerializedProperty của values</param>
//         private MultiColumnListView CreateDictionaryView(SerializedProperty keyProperties, SerializedProperty valueProperties)
//         {
//             var dictionary = DictionaryVisualTree.CloneTree().Q<MultiColumnListView>("DictionaryContainer");
//             dictionary.styleSheets.Add(DictionaryStyleSheet);
//             dictionary.style.fontSize = 14;
//             dictionary.style.unityFontStyleAndWeight = FontStyle.BoldAndItalic;
//             
//             // Khôi phục lại danh sách items
//             InitializePersistentItems(keyProperties, valueProperties);
//             
//             // Cấu hình các columns
//             ConfigureDictionaryColumns(dictionary, keyProperties, valueProperties);
//             
//             // Cấu hình và gán itemsSource
//             dictionary.itemsSource = _persistentItems;
//             dictionary.Rebuild();
//             
//             return dictionary;
//         }
//
//         /// <summary>
//         /// Khởi tạo danh sách các items từ SerializedProperties
//         /// </summary>
//         private void InitializePersistentItems(SerializedProperty keyProperties, SerializedProperty valueProperties)
//         {
//             _persistentItems.Clear();
//             for (int i = 0; i < keyProperties.arraySize; i++)
//             {
//                 _persistentItems.Add(new Item
//                 {
//                     Key = keyProperties.GetArrayElementAtIndex(i),
//                     Value = valueProperties.GetArrayElementAtIndex(i)
//                 });
//             }
//         }
//
//         /// <summary>
//         /// Cấu hình các cột cho MultiColumnListView
//         /// </summary>
//         private void ConfigureDictionaryColumns(MultiColumnListView dictionary, 
//             SerializedProperty keyProperties, SerializedProperty valueProperties)
//         {
//             // Cấu hình cột 0 (key-value)
//             dictionary.columns[0].makeCell = () =>
//             {
//                 var foldoutItem = DictionaryItemVisualTree.CloneTree().Q<Foldout>("DictionaryItemContainer");
//                 foldoutItem.styleSheets.Add(DictionaryItemStyleSheet);
//                 foldoutItem.style.alignSelf = new StyleEnum<Align>(Align.Stretch);
//                 return foldoutItem;
//             };
//             
//             // Cấu hình cột 1 (delete button)
//             dictionary.columns[1].makeCell = () =>
//             {
//                 var deleteButton = DictionaryItemVisualTree.CloneTree().Q<Button>("DeleteButton");
//                 return deleteButton;
//             };
//             
//             // Bind dữ liệu cho cột 0
//             dictionary.columns[0].bindCell = (element, index) =>
//             {
//                 if (index >= keyProperties.arraySize) return;
//                 if (element is not Foldout foldoutItem) return;
//                 
//                 foldoutItem.text = $"Item {index}";
//                 
//                 var key = foldoutItem.Q<PropertyField>("KeyField");
//                 key.label = "Key";
//                 
//                 var value = foldoutItem.Q<PropertyField>("ValueField");
//                 value.label = "Value";
//                 
//                 if (_persistentItems.Count <= index) return;
//                 key.BindProperty(_persistentItems[index].Key);
//                 value.BindProperty(_persistentItems[index].Value);
//                 
//                 itemHeight = foldoutItem.style.height.value.value;
//             };
//             
//             // Bind dữ liệu và sự kiện cho cột 1
//             dictionary.columns[1].bindCell = (element, index) =>
//             {
//                 if (index >= keyProperties.arraySize) return;
//                 if (element is not Button deleteButton) return;
//                 
//                 ConfigureDeleteButton(deleteButton, index, keyProperties, valueProperties, dictionary);
//             };
//         }
//
//         /// <summary>
//         /// Cấu hình nút xóa và sự kiện click
//         /// </summary>
//         private void ConfigureDeleteButton(Button deleteButton, int index, 
//             SerializedProperty keyProperties, SerializedProperty valueProperties, 
//             MultiColumnListView dictionary)
//         {
//             deleteButton.clicked += () =>
//             {
//                 // Kiểm tra index hợp lệ trước khi xóa
//                 if (index < 0 || index >= keyProperties.arraySize) return;
//
//                 try 
//                 {
//                     // Cập nhật serializedObject trước khi thực hiện thay đổi
//                     keyProperties.serializedObject.Update();
//                     valueProperties.serializedObject.Update();
//
//                     // Lưu trữ kích thước hiện tại
//                     int currentSize = keyProperties.arraySize;
//             
//                     if (index < currentSize - 1)
//                     {
//                         // Chỉ di chuyển nếu không phải phần tử cuối cùng
//                         keyProperties.MoveArrayElement(index, currentSize - 1);
//                         valueProperties.MoveArrayElement(index, currentSize - 1);
//                 
//                         // Áp dụng thay đổi sau khi di chuyển
//                         keyProperties.serializedObject.ApplyModifiedProperties();
//                         valueProperties.serializedObject.ApplyModifiedProperties();
//                     }
//             
//                     // Xóa phần tử cuối cùng
//                     keyProperties.arraySize = currentSize - 1;
//                     valueProperties.arraySize = currentSize - 1;
//             
//                     // Áp dụng thay đổi sau khi xóa
//                     keyProperties.serializedObject.ApplyModifiedProperties();
//                     valueProperties.serializedObject.ApplyModifiedProperties();
//             
//                     // Cập nhật lại danh sách items sau khi xóa
//                     InitializePersistentItems(keyProperties, valueProperties);
//             
//                     // Cập nhật UI
//                     dictionary.itemsSource = _persistentItems;
//                     dictionary.Rebuild();
//                 }
//                 catch (Exception ex)
//                 {
//                     Debug.LogError($"Lỗi khi xóa item: {ex.Message}");
//                 }
//
//             };
//         }
//
//         /// <summary>
//         /// Tạo các điều khiển để thêm item mới
//         /// </summary>
//         private VisualElement CreateAddControls(SerializedProperty keyProperty, SerializedProperty valueProperty,
//             SerializedProperty keyProperties, SerializedProperty valueProperties,
//             MultiColumnListView dictionary, IntegerField countField, 
//             VisualElement container, Label infoLabel, SerializedProperty property)
//         {
//             var addGroupBox = AddGroupVisualTree.CloneTree().Q<GroupBox>("AddGroupContainer");
//             var keyField = addGroupBox.Q<PropertyField>("KeyField");
//             keyField.label = "Key";
//             keyField.BindProperty(keyProperty);
//             var valueField = addGroupBox.Q<PropertyField>("ValueField");
//             valueField.label = "Value";
//             valueField.BindProperty(valueProperty);
//             var addButton = addGroupBox.Q<Button>("AddButton");
//
//             // Cấu hình sự kiện click cho nút thêm
//             addButton.clicked += () => AddNewItem(keyProperty, valueProperty, keyProperties, valueProperties,
//                 dictionary, countField, container, infoLabel, property);
//             
//             return addGroupBox;
//         }
//
//         /// <summary>
//         /// Thêm một item mới vào dictionary
//         /// </summary>
//         private void AddNewItem(SerializedProperty keyProperty, SerializedProperty valueProperty,
//             SerializedProperty keyProperties, SerializedProperty valueProperties,
//             MultiColumnListView dictionary, IntegerField countField, 
//             VisualElement container, Label infoLabel, SerializedProperty property)
//         {
//             keyProperties.arraySize++;
//             valueProperties.arraySize++;
//
//             var newKeyProperty = keyProperties.GetArrayElementAtIndex(keyProperties.arraySize - 1);
//             var newValueProperty = valueProperties.GetArrayElementAtIndex(valueProperties.arraySize - 1);
//             
//             
//             AssignValue(keyProperty, newKeyProperty);
//             AssignValue(valueProperty, newValueProperty);
//             
//             var newItem = new Item
//             {
//                 Key = newKeyProperty,
//                 Value = newValueProperty,
//             };
//             
//             // Kiểm tra key đã tồn tại
//             if (_persistentItems.Any(item => CheckValue(item.Key, newKeyProperty)))
//             {
//                 keyProperties.arraySize = _persistentItems.Count;
//                 valueProperties.arraySize = _persistentItems.Count;
//                 container.Add(infoLabel);
//                 return;
//             }
//
//             // Xóa thông báo lỗi nếu có
//             if (container.Contains(infoLabel))
//             {
//                 container.Remove(infoLabel);
//             }
//             
//             // Thêm item mới và cập nhật UI
//             _persistentItems.Add(newItem);
//             dictionary.itemsSource = _persistentItems;
//             dictionary.Rebuild();
//             countField.value = keyProperties.arraySize;
//             
//             // Lưu thay đổi
//             property.serializedObject.ApplyModifiedProperties();
//         }
//         
//         /// <summary>
//         /// Kiểm tra xem một SerializedProperty có phải là con của SerializedProperty khác không
//         /// </summary>
//         /// <param name="childProperty">SerializedProperty cần kiểm tra</param>
//         /// <param name="parentProperty">SerializedProperty cha để so sánh</param>
//         /// <returns>True nếu là con, ngược lại trả về False</returns>
//         private static bool IsChildOf(SerializedProperty childProperty, SerializedProperty parentProperty)
//         {
//             // Kiểm tra nếu một trong hai property là null
//             if (childProperty == null || parentProperty == null)
//                 return false;
//             
//             // So sánh đường dẫn SerializedProperty
//             var childPath = childProperty.propertyPath;
//             var parentPath = parentProperty.propertyPath;
//
//             // Kiểm tra xem đường dẫn con có bắt đầu bằng đường dẫn cha không
//             // Và đảm bảo nó không phải chính property cha
//             return childPath.StartsWith(parentPath + ".") && childPath != parentPath;
//         }
//
//         /// <summary>
//         /// Kiểm tra xem một SerializedProperty có phải là cha của SerializedProperty khác không
//         /// </summary>
//         /// <param name="parentProperty">SerializedProperty cha để kiểm tra</param>
//         /// <param name="childProperty">SerializedProperty con để so sánh</param>
//         /// <returns>True nếu là cha, ngược lại trả về False</returns>
//         private static bool IsParentOf(SerializedProperty parentProperty, SerializedProperty childProperty)
//         {
//             // Sử dụng phương thức IsChildOf với tham số đảo ngược
//             return IsChildOf(parentProperty, childProperty);
//         }
//
//         /// <summary>
//         /// Kiểm tra xem hai giá trị có giống nhau không
//         /// </summary>
//         /// <param name="valueFirst">SerializedProperty thứ nhất</param>
//         /// <param name="valueSecond">SerializedProperty thứ hai</param>
//         /// <returns>True nếu hai giá trị giống nhau</returns>
//         private static bool CheckValue(SerializedProperty valueFirst, SerializedProperty valueSecond)
//         {
//             switch (valueFirst.propertyType)
//             {
//                 case SerializedPropertyType.Generic:
//                     return false;
//                 case SerializedPropertyType.Integer:
//                     return valueFirst.intValue == valueSecond.intValue;
//                 case SerializedPropertyType.Boolean:
//                     return valueFirst.boolValue == valueSecond.boolValue;
//                 case SerializedPropertyType.Float:
//                     return Mathf.Approximately(valueFirst.floatValue, valueSecond.floatValue);
//                 case SerializedPropertyType.String:
//                     return valueFirst.stringValue == valueSecond.stringValue;
//                 case SerializedPropertyType.Color:
//                     return valueFirst.colorValue == valueSecond.colorValue;
//                 case SerializedPropertyType.ObjectReference:
//                     return valueFirst.objectReferenceValue == valueSecond.objectReferenceValue;
//                 case SerializedPropertyType.LayerMask:
//                     return false;
//                 case SerializedPropertyType.Enum:
//                     return valueFirst.enumValueIndex == valueSecond.enumValueIndex;
//                 case SerializedPropertyType.Vector2:
//                     return valueFirst.vector2Value == valueSecond.vector2Value;
//                 case SerializedPropertyType.Vector3:
//                     return valueFirst.vector3Value == valueSecond.vector3Value;
//                 case SerializedPropertyType.Vector4:
//                     return valueFirst.vector4Value == valueSecond.vector4Value;
//                 case SerializedPropertyType.Rect:
//                     return valueFirst.rectValue == valueSecond.rectValue;
//                 case SerializedPropertyType.ArraySize:
//                     return valueFirst.arraySize == valueSecond.arraySize;
//                 case SerializedPropertyType.Character:
//                     return false;
//                 case SerializedPropertyType.AnimationCurve:
//                     return valueFirst.animationCurveValue.Equals(valueSecond.animationCurveValue);
//                 case SerializedPropertyType.Bounds:
//                     return valueFirst.boundsValue == valueSecond.boundsValue;
//                 case SerializedPropertyType.Gradient:
//                     return valueFirst.gradientValue.Equals(valueSecond.gradientValue);
//                 case SerializedPropertyType.Quaternion:
//                     return valueFirst.quaternionValue == valueSecond.quaternionValue;
//                 case SerializedPropertyType.ExposedReference:
//                     return valueFirst.exposedReferenceValue == valueSecond.exposedReferenceValue;
//                 case SerializedPropertyType.FixedBufferSize:
//                     return valueFirst.fixedBufferSize == valueSecond.fixedBufferSize;
//                 case SerializedPropertyType.Vector2Int:
//                     return valueFirst.vector2IntValue == valueSecond.vector2IntValue;
//                 case SerializedPropertyType.Vector3Int:
//                     return valueFirst.vector3IntValue == valueSecond.vector3IntValue;
//                 case SerializedPropertyType.RectInt:
//                     return valueFirst.rectIntValue == valueSecond.rectIntValue;
//                 case SerializedPropertyType.BoundsInt:
//                     return valueFirst.boundsIntValue == valueSecond.boundsIntValue;
//                 case SerializedPropertyType.ManagedReference:
//                     return valueFirst.managedReferenceValue == valueSecond.managedReferenceValue;
//                 case SerializedPropertyType.Hash128:
//                     return valueFirst.hash128Value == valueSecond.hash128Value;
//                 case SerializedPropertyType.RenderingLayerMask:
//                     return false;
//                 default:
//                     throw new ArgumentException();
//             }
//         }
//         
//         /// <summary>
//         /// Gán giá trị từ SerializedProperty nguồn sang đích
//         /// </summary>
//         /// <param name="inputValue">SerializedProperty nguồn</param>
//         /// <param name="outputValue">SerializedProperty đích</param>
//         private static void AssignValue(SerializedProperty inputValue, SerializedProperty outputValue)
//         {
//             switch (inputValue.propertyType)
//             {
//                 case SerializedPropertyType.Generic:
//                     if (inputValue.isArray && outputValue.isArray)
//                     {
//                         // Xử lý khi cả hai đều là mảng
//                         Debug.Log($"Xử lý mảng: {inputValue.propertyPath}, số phần tử: {inputValue.arraySize}");
//                         
//                         // Đảm bảo mảng đích có cùng kích thước với mảng nguồn
//                         outputValue.arraySize = inputValue.arraySize;
//                         
//                         // Sao chép từng phần tử
//                         for (int i = 0; i < inputValue.arraySize; i++)
//                         {
//                             var inputElement = inputValue.GetArrayElementAtIndex(i);
//                             var outputElement = outputValue.GetArrayElementAtIndex(i);
//                             
//                             // Gọi đệ quy để gán giá trị cho phần tử
//                             AssignValue(inputElement, outputElement);
//                             Debug.Log($"Đã sao chép phần tử mảng thứ {i}: {inputElement.propertyPath}");
//                         }
//                         
//                         // Áp dụng thay đổi
//                         inputValue.serializedObject.ApplyModifiedProperties();
//                         outputValue.serializedObject.ApplyModifiedProperties();
//                     }
//                     else
//                     {
//                         // Giữ nguyên logic hiện tại cho trường hợp Generic không phải mảng
//                         // Lưu trữ tất cả các thuộc tính từ inputValues
//                         var inputPropertiesDict = new Dictionary<string, SerializedProperty>();
//                         var inputValues = inputValue.serializedObject.GetIterator();
//                         while (inputValues.NextVisible(true))
//                         {
//                             if (IsParentOf(inputValues, inputValue))
//                             {
//                                 // Lưu bản sao của thuộc tính với tên làm khóa
//                                 inputPropertiesDict[inputValues.name] = inputValues.Copy();
//                                 Debug.LogWarning($"in {inputValues.name}:{inputValues.propertyPath}: {inputValues.propertyType}");
//                             }
//                         }
//
//                         // Kiểm tra outputValues, so sánh với inputValues và gán giá trị nếu phù hợp
//                         var outputValues = outputValue.serializedObject.GetIterator();
//                         bool hasModified = false;
//
//                         while (outputValues.NextVisible(true))
//                         {
//                             if (IsParentOf(outputValues, outputValue))
//                             {
//                                 // Kiểm tra xem có thuộc tính cùng tên trong inputValues không
//                                 if (inputPropertiesDict.TryGetValue(outputValues.name, out var matchedInputProperty))
//                                 {
//                                     // Thuộc tính có cùng tên
//                                     Debug.Log($"Thuộc tính trùng khớp: {outputValues.name}");
//                                     
//                                     // Kiểm tra xem có cùng kiểu dữ liệu không
//                                     if (matchedInputProperty.propertyType == outputValues.propertyType)
//                                     {
//                                         // Sao chép giá trị từ input sang output dựa trên kiểu dữ liệu
//                                         AssignValue(matchedInputProperty, outputValues);
//                                         hasModified = true;
//                                         Debug.Log($"Đã sao chép giá trị cho thuộc tính: {outputValues.name}");
//                                     }
//                                     else
//                                     {
//                                         Debug.LogWarning($"Thuộc tính {outputValues.name} không thể sao chép vì khác kiểu: Input={matchedInputProperty.propertyType}, Output={outputValues.propertyType}");
//                                     }
//                                 }
//                                 Debug.LogWarning($"out {outputValues.name}:{outputValues.propertyPath}: {outputValues.propertyType}");
//                             }
//                         }
//                     }
//                     break;
//                 case SerializedPropertyType.Integer:
//                     outputValue.intValue = inputValue.intValue;
//                     break;
//                 case SerializedPropertyType.Boolean:
//                     outputValue.boolValue = inputValue.boolValue;
//                     break;
//                 case SerializedPropertyType.Float:
//                     outputValue.floatValue = inputValue.floatValue;
//                     break;
//                 case SerializedPropertyType.String:
//                     outputValue.stringValue = inputValue.stringValue;
//                     break;
//                 case SerializedPropertyType.Color:
//                     outputValue.colorValue = inputValue.colorValue;
//                     break;
//                 case SerializedPropertyType.ObjectReference:
//                     outputValue.objectReferenceValue = inputValue.objectReferenceValue;
//                     break;
//                 case SerializedPropertyType.LayerMask:
//                     // Xử lý LayerMask một cách an toàn
//                     outputValue.intValue = inputValue.intValue;
//                     break;
//                 case SerializedPropertyType.Enum:
//                     Debug.LogWarning($"{outputValue.enumValueIndex}:{inputValue.enumValueIndex}");
//                     outputValue.enumValueIndex = inputValue.enumValueIndex;
//                     break;
//                 case SerializedPropertyType.Vector2:
//                     outputValue.vector2Value = inputValue.vector2Value;
//                     break;
//                 case SerializedPropertyType.Vector3:
//                     outputValue.vector3Value = inputValue.vector3Value;
//                     break;
//                 case SerializedPropertyType.Vector4:
//                     outputValue.vector4Value = inputValue.vector4Value;
//                     break;
//                 case SerializedPropertyType.Rect:
//                     outputValue.rectValue = inputValue.rectValue;
//                     break;
//                 case SerializedPropertyType.ArraySize:
//                     // Kiểm tra xem property có phải là mảng không trước khi truy cập arraySize
//                     if (inputValue.isArray && outputValue.isArray)
//                     {
//                         try
//                         {
//                             outputValue.arraySize = inputValue.arraySize;
//                         }
//                         catch (Exception e)
//                         {
//                             Debug.LogError($"Lỗi khi gán kích thước mảng: {e.Message}");
//                         }
//                     }
//                     else
//                     {
//                         Debug.LogWarning("Không thể gán kích thước mảng vì một trong các thuộc tính không phải là mảng");
//                     }
//                     break;
//                 case SerializedPropertyType.Character:
//                     outputValue.stringValue = inputValue.stringValue;
//                     break;
//                 case SerializedPropertyType.AnimationCurve:
//                     outputValue.animationCurveValue = inputValue.animationCurveValue;
//                     break;
//                 case SerializedPropertyType.Bounds:
//                     outputValue.boundsValue = inputValue.boundsValue;
//                     break;
//                 case SerializedPropertyType.Gradient:
//                     // Xử lý Gradient một cách an toàn
//                     try
//                     {
//                         outputValue.gradientValue = inputValue.gradientValue;
//                     }
//                     catch (Exception e)
//                     {
//                         Debug.LogError($"Lỗi khi gán giá trị Gradient: {e.Message}");
//                     }
//                     break;
//                 case SerializedPropertyType.Quaternion:
//                     outputValue.quaternionValue = inputValue.quaternionValue;
//                     break;
//                 case SerializedPropertyType.ExposedReference:
//                     outputValue.exposedReferenceValue = inputValue.exposedReferenceValue;
//                     break;
//                 case SerializedPropertyType.FixedBufferSize:
//                     // FixedBufferSize thường chỉ đọc, bỏ qua
//                     Debug.LogWarning("FixedBufferSize thường chỉ đọc, không thể gán");
//                     break;
//                 case SerializedPropertyType.Vector2Int:
//                     outputValue.vector2IntValue = inputValue.vector2IntValue;
//                     break;
//                 case SerializedPropertyType.Vector3Int:
//                     outputValue.vector3IntValue = inputValue.vector3IntValue;
//                     break;
//                 case SerializedPropertyType.RectInt:
//                     outputValue.rectIntValue = inputValue.rectIntValue;
//                     break;
//                 case SerializedPropertyType.BoundsInt:
//                     outputValue.boundsIntValue = inputValue.boundsIntValue;
//                     break;
//                 case SerializedPropertyType.ManagedReference:
//                     try
//                     {
//                         outputValue.managedReferenceValue = inputValue.managedReferenceValue;
//                     }
//                     catch (Exception e)
//                     {
//                         Debug.LogError($"Lỗi khi gán ManagedReference: {e.Message}");
//                     }
//                     break;
//                 case SerializedPropertyType.Hash128:
//                     outputValue.hash128Value = inputValue.hash128Value;
//                     break;
//                 case SerializedPropertyType.RenderingLayerMask:
//                     // Xử lý tương tự như LayerMask
//                     outputValue.intValue = inputValue.intValue;
//                     break;
//                 default:
//                     Debug.LogWarning($"Không hỗ trợ kiểu: {inputValue.propertyType}");
//                     break;
//             }
//         }
//     }
//     #endif
// }