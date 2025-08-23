using UnityEditor;
using UnityEngine;

namespace Game.Inventory
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
            // Before drawing, force IsEnabled default if unset
            var isEnabledProp = property.FindPropertyRelative("IsEnabled");
 
                isEnabledProp.boolValue = true;
            

            EditorGUI.PropertyField(position, property, label, true);
        }
    }
}
