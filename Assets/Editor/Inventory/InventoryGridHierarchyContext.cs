using Game.Inventory;
using UnityEditor;
using UnityEngine;

public static class InventoryGridHierarchyContext
{
    [MenuItem("GameObject/RPGFramework/Inventory/Create Inventory Grid", false, 10)]
    private static void CreateInventoryGrid(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("InventoryGrid");
        Undo.RegisterCreatedObjectUndo(go, "Create InventoryGrid");

        // If right-clicked on another GameObject, parent under it
        GameObject context = menuCommand.context as GameObject;
        if (context != null)
        {
            go.transform.SetParent(context.transform);
        }
        go.AddComponent<InventoryGrid>();
        Selection.activeGameObject = go;
    }
}
