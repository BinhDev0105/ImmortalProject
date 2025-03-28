using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameUtilities.Runtime;
using UnityEditor;
using UnityEngine.UIElements;

namespace GameUtilities.Editor
{
    [CustomEditor(typeof(SerializedScriptableObject), true)]
    public class SerializedScriptableObjectEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            
            var serializedScriptableObject = target as SerializedScriptableObject;

            if (serializedScriptableObject == null) return root;
            
            var titleLabel = new Label(serializedScriptableObject.name)
            {
                style =
                {
                    fontSize = 20
                }
            };
            root.Add(titleLabel);
            return root;
        }
    }
}