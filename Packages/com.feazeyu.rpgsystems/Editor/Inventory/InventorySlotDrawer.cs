using UnityEditor;
using UnityEngine;

namespace Feazeyu.RPGSystems.Inventory
{
    [CustomPropertyDrawer(typeof(InventorySlot), true)]
    public class InventorySlotDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }
}
