using UnityEngine;
using UnityEditor;
namespace Game.Inventory
{

    public static class InventoryManagerHierarchyContext
    {
        [MenuItem("GameObject/RPGFramework/Inventory/Create Inventory Manager", false, 10)]
        private static void CreateInventoryManager(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("InventoryManager");
            Undo.RegisterCreatedObjectUndo(go, "Create InventoryManager");

            // If right-clicked on another GameObject, parent under it
            GameObject context = menuCommand.context as GameObject;
            if (context != null)
            {
                go.transform.SetParent(context.transform);
            }
            go.AddComponent<InventoryManager>();
            Selection.activeGameObject = go;
        }
    }
}
