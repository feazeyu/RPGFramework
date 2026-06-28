using UnityEditor;
using UnityEngine;

namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>Custom inspector for <see cref="ShopGridUI"/>, extending the grid editor with shop fields.</summary>
    [CustomEditor(typeof(ShopGridUI))]
    public class ShopGridUIEditor : InventoryGridEditor
    {
        private static readonly string[] ShopProperties =
        {
            "shopInventory", "_currencyProvider", "priceLabelPrefab"
        };

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Shop", EditorStyles.boldLabel);
            foreach (var propName in ShopProperties)
            {
                var prop = serializedObject.FindProperty(propName);
                if (prop != null)
                    EditorGUILayout.PropertyField(prop, true);
            }

            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(10);
            base.OnInspectorGUI();
        }
    }
}
