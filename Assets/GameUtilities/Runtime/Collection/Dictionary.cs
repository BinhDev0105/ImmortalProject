using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameUtilities.Runtime.Collection
{
    /// <summary>
    /// Lớp SerializableDictionary giúp hiển thị Dictionary trong Inspector của Unity
    /// Sử dụng cùng với SerializableDictionaryDrawer có MultiColumnListView để hiển thị
    /// </summary>
    [Serializable]
    public class Dictionary<TKey, TValue> : ISerializationCallbackReceiver
    {
        // Dictionary thực tế lưu trữ dữ liệu trong runtime
        [NonSerialized]
        private System.Collections.Generic.Dictionary<TKey, TValue> dictionary = new();
        
        // Danh sách keys và values để Unity có thể serialize
        [SerializeField] private TKey key;
        [SerializeField] private TValue value;
        
        [SerializeField]
        private List<TKey> keys = new();

        [SerializeField]
        private List<TValue> values = new();

        // Phương thức truy cập Dictionary thông qua indexer
        public TValue this[TKey key]
        {
            get { return dictionary[key]; }
            set { dictionary[key] = value; }
        }

        // Trả về số lượng phần tử trong Dictionary
        public int Count => dictionary.Count;

        // Trả về danh sách các keys
        public List<TKey> Keys => keys;

        // Trả về danh sách các values
        public List<TValue> Values => values;

        // Thêm một cặp key-value vào Dictionary
        public void Add(TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
                throw new ArgumentException($"Key {key} đã tồn tại trong dictionary");

            dictionary.Add(key, value);
            keys.Add(key);
            values.Add(value);
        }

        // Xóa một phần tử theo key
        public bool Remove(TKey key)
        {
            if (!dictionary.ContainsKey(key))
                return false;

            int index = keys.IndexOf(key);
            if (index >= 0)
            {
                keys.RemoveAt(index);
                values.RemoveAt(index);
            }

            return dictionary.Remove(key);
        }

        // Xóa phần tử theo index
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= keys.Count)
                return;

            TKey key = keys[index];
            keys.RemoveAt(index);
            values.RemoveAt(index);

            if (dictionary.ContainsKey(key))
                dictionary.Remove(key);
        }

        // Kiểm tra xem key có tồn tại trong Dictionary không
        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        // Xử lý trước khi Unity serialize
        public void OnBeforeSerialize()
        {
            // Không cần làm gì ở đây vì danh sách keys và values
            // được cập nhật trực tiếp trong các phương thức Add và Remove
        }

        // Xử lý sau khi Unity deserialize
        public void OnAfterDeserialize()
        {
            // Tạo lại Dictionary từ danh sách keys và values đã được serialize
            dictionary.Clear();

            for (var i = 0; i < Math.Min(keys.Count, values.Count); i++)
            {
                // Bỏ qua key null và key trùng lặp
                if (keys[i] != null && !dictionary.ContainsKey(keys[i]))
                {
                    dictionary.Add(keys[i], values[i]);
                }
            }
        }
        
        // Xóa tất cả phần tử
        public void Clear()
        {
            dictionary.Clear();
            keys.Clear();
            values.Clear();
        }
    }
}