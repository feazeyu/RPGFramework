using UnityEngine;
using UnityEditor;
namespace Feazeyu.RPGSystems.Inventory
{

    /// <summary>Adds a GameObject menu entry for creating an inventory manager in the scene.</summary>
    public static class InventoryManagerHierarchyContext
    {
        [MenuItem("GameObject/RPGFramework/Inventory/Create Inventory Manager", false, 10)]
        private static void CreateInventoryManager(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("InventoryManager");
            Undo.RegisterCreatedObjectUndo(go, "Create InventoryManager");

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
