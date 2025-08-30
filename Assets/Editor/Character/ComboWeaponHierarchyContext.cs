using Game.Character;
using Game.Core;
using UnityEditor;
using UnityEngine;

public static class ComboWeaponHierarchyContext
{
    [MenuItem("GameObject/RPGFramework/Character/Create Combo Weapon", false, 10)]
    private static void CreateComboWeapon(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("WeaponPivot");
        Undo.RegisterCreatedObjectUndo(go, "Create WeaponPivot");

        // If right-clicked on another GameObject, parent under it
        GameObject context = menuCommand.context as GameObject;
        if (context != null)
        {
            go.transform.SetParent(context.transform);
        }
        go.AddComponent<RotateTowardsPoint>();
        go.transform.localPosition = Vector3.zero;
        Selection.activeGameObject = go;
        GameObject off = new GameObject("WeaponOffset");
        off.transform.SetParent(go.transform);
        off.transform.localPosition = Vector3.zero;
        off.AddComponent<WeaponOffsetController>();
        off.AddComponent<ComboWeapon>();
        GameObject weapon = new GameObject("Weapon");
        weapon.transform.SetParent(off.transform);
        weapon.transform.localPosition = Vector3.zero;
    }
}
