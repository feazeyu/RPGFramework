using UnityEditor;
using UnityEngine;

namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>Property drawer for <see cref="InventorySlot"/> and its subclasses.</summary>
    [CustomPropertyDrawer(typeof(InventorySlot), true)]
    public class InventorySlotDrawer : PropertyDrawer
    {
        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }
}
