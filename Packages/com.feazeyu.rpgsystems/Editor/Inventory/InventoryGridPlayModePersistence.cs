using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>
    /// Captures and restores inventory grid contents across play-mode transitions in the
    /// editor, so entering and exiting play mode does not lose authored grid state.
    /// </summary>
    [InitializeOnLoad]
    public static class InventoryGridPlayModePersistence
    {
        private static readonly Dictionary<string, string> s_Captures = new();

        static InventoryGridPlayModePersistence()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode) CaptureAll();
            else if (change == PlayModeStateChange.EnteredEditMode) RestoreAll();
        }

        private static void CaptureAll()
        {
            s_Captures.Clear();
            foreach (var grid in UnityEngine.Object.FindObjectsByType<InventoryGrid>(FindObjectsSortMode.None))
            {
                if (grid.persistPlayModeChanges)
                    s_Captures[HierarchyKey(grid)] = Serialize(grid);
            }
        }

        private static void RestoreAll()
        {
            if (s_Captures.Count == 0) return;
            foreach (var grid in UnityEngine.Object.FindObjectsByType<InventoryGrid>(FindObjectsSortMode.None))
            {
                if (s_Captures.TryGetValue(HierarchyKey(grid), out var json))
                {
                    Undo.RecordObject(grid, "Restore inventory from play mode");
                    Restore(grid, json);
                    EditorUtility.SetDirty(grid);
                }
            }
            s_Captures.Clear();
        }

        private static string HierarchyKey(InventoryGrid grid)
        {
            var parts = new List<string>();
            var t = grid.transform;
            while (t != null) { parts.Add(t.name); t = t.parent; }
            parts.Reverse();
            return grid.gameObject.scene.path + ":" + string.Join("/", parts);
        }


        [Serializable]
        private class GridCapture
        {
            /// <summary>Serializable snapshot of a single grid cell.</summary>
            [Serializable]
            public struct SlotState
            {
                /// <summary>Cell coordinates.</summary>
                public int x, y;
                /// <summary>Item id in the cell, or -1.</summary>
                public int itemId;
                /// <summary>Anchor coordinates for multi-cell items.</summary>
                public int anchorX, anchorY;
            }
            /// <summary>Slots.</summary>
            public List<SlotState> slots = new();
        }

        private static string Serialize(InventoryGrid grid)
        {
            var cap = new GridCapture();
            for (int x = 0; x < grid.columns; x++)
                for (int y = 0; y < grid.rows; y++)
                    if (grid.Cells.TryGet(x, y, out var slot))
                        cap.slots.Add(new GridCapture.SlotState
                        {
                            x = x, y = y,
                            itemId   = slot.ItemId,
                            anchorX  = slot.anchorPosition.x,
                            anchorY  = slot.anchorPosition.y,
                        });
            return JsonUtility.ToJson(cap);
        }

        private static void Restore(InventoryGrid grid, string json)
        {
            var cap = JsonUtility.FromJson<GridCapture>(json);
            foreach (var s in cap.slots)
                if (grid.Cells.TryGet(s.x, s.y, out var slot))
                    slot.EditorOnlySetState(s.itemId, new Vector2Int(s.anchorX, s.anchorY));
        }
    }
}
