using Feazeyu.RPGSystems.Inventory;
using UnityEditor;
using UnityEngine;

/// <summary>Adds a GameObject menu entry for creating an inventory grid in the scene.</summary>
public static class InventoryGridHierarchyContext
{
    [MenuItem("GameObject/RPGFramework/Inventory/Create Inventory Grid", false, 10)]
    private static void CreateInventoryGrid(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("InventoryGrid");
        Undo.RegisterCreatedObjectUndo(go, "Create InventoryGrid");

        GameObject context = menuCommand.context as GameObject;
        if (context != null)
        {
            go.transform.SetParent(context.transform);
        }
        go.AddComponent<InventoryGrid>();
        Selection.activeGameObject = go;
    }
}
