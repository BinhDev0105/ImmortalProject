using System;
using UnityEngine;
// ReSharper disable InconsistentNaming
// ReSharper disable MemberInitializerValueIgnored

namespace GameUtilities.Runtime.Collection
{
    /// <summary>
    /// Attribute để hiển thị Dictionary trong Inspector
    /// Sử dụng cùng với DictionaryDrawer
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class DictionaryDrawerAttribute : PropertyAttribute
    {
        // Có thể thêm các tuỳ chọn hiển thị ở đây
        public string headerName = "Dictionary";

        public DictionaryDrawerAttribute(string name = "Dictionary")
        {
            headerName = name;
        }
    }
}