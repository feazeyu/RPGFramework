using Feazeyu.RPGSystems.Inventory;
using UnityEditor;
using UnityEngine;

public static class ShopHierarchyContext
{
    [MenuItem("GameObject/RPGFramework/Shop/Create Shop Grid", false, 10)]
    private static void CreateShopGrid(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("ShopGrid");
        Undo.RegisterCreatedObjectUndo(go, "Create ShopGrid");
        if (menuCommand.context is GameObject context)
            go.transform.SetParent(context.transform);
        go.AddComponent<ShopGridUI>();
        Selection.activeGameObject = go;
    }

    [MenuItem("GameObject/RPGFramework/Shop/Create Shop List", false, 10)]
    private static void CreateShopList(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("ShopList");
        Undo.RegisterCreatedObjectUndo(go, "Create ShopList");
        if (menuCommand.context is GameObject context)
            go.transform.SetParent(context.transform);
        go.AddComponent<ShopListUI>();
        Selection.activeGameObject = go;
    }

    [MenuItem("GameObject/RPGFramework/Shop/Create Shopkeep", false, 10)]
    private static void CreateShopkeep(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("Shopkeep");
        Undo.RegisterCreatedObjectUndo(go, "Create Shopkeep");
        if (menuCommand.context is GameObject context)
            go.transform.SetParent(context.transform);
        go.AddComponent<Shopkeep>();
        Selection.activeGameObject = go;
    }
}
