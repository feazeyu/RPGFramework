using Game.Inventory;
using UnityEditor;
using UnityEngine;

public static class InventoryListHierarchyContext
{
    [MenuItem("GameObject/RPGFramework/Inventory/Create Inventory List", false, 10)]
    private static void CreateInventoryGrid(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("InventoryList");
        Undo.RegisterCreatedObjectUndo(go, "Create InventoryList");

        // If right-clicked on another GameObject, parent under it
        GameObject context = menuCommand.context as GameObject;
        if (context != null)
        {
            go.transform.SetParent(context.transform);
        }
        go.AddComponent<InventoryList>();
        Selection.activeGameObject = go;
    }
}