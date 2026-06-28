
using UnityEditor;
using UnityEngine;
namespace Feazeyu.RPGSystems.Inventory
{
    [CustomEditor(typeof(InventoryManager))]
    class InventoryManagerEditor : Editor
    {
        /// <summary>Resource path.</summary>
        [Tooltip("Path to the folder containing item prefabs")]
        public string resourcePath = "Items";
        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button($"Reload items in Resources/{resourcePath}"))
            {
                ((InventoryManager)target).ReloadItems(resourcePath);
            }
        }
    }
}

