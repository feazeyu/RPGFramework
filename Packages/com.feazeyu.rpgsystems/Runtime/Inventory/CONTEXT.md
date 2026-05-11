# Inventory System Context

## Overview

The inventory system lives in `Assets/Scripts/Inventory/` and `Assets/Editor/Inventory/`. It is split into three sub-systems: **Grid** (spatial, shape-aware), **List** (linear, stackable), and **Shopping** (stub). All three share a common slot/container interface hierarchy and a central `InventoryManager` singleton that maps item IDs to prefabs.

Assembly definition: `Inventory.asmdef` (`Game.Inventory` namespace, `Game.Items` and `Game.Core.Utilities` references).

---

## InventoryManager

**File:** `InventoryManager.cs`

Singleton `MonoBehaviour`. Holds:
- `items`: `SerializableDictionary<int, GameObject>` — item ID → prefab mapping.
- `cellSize`: `Vector2` — global grid slot size used by `InventoryGridGenerator`.

Key API:
- `ReloadItems(string path)` — scans `Resources/<path>/` for prefabs with an `Item` component and repopulates `items`.
- `GetItemById(int id)` — returns the prefab or `null`.

The singleton is lazy: first access calls `FindFirstObjectByType<InventoryManager>()` and auto-creates one if absent. `ExecuteInEditMode` keeps it alive in editor.

---

## Item Registration

Items are `GameObject` prefabs with an `Item` component (`Game.Items` namespace). `Item.info` is an `ItemInfo` that carries:
- `id` — unique int key used everywhere as the canonical item reference.
- `Name` — display string.
- `Shape` — `ItemShape`: list of `Vector2Int` positions describing the 2D footprint for grid placement.
- `Item.GetAnchorSlot()` — returns the slot within the shape that acts as the placement origin.

The grid stores only the item ID, not the `GameObject` directly. `InventorySlot.Item` performs a live lookup via `InventoryManager.Instance.GetItemById(ItemId)` every call — there is no cached reference.

---

## Slot Hierarchy

**File:** `InventorySlot.cs`

```
InventorySlot                  — base, single item, id = -1 when empty
├── LockedInventorySlot        — rejects all Put/Remove; colored red in editor
└── StackableInventorySlot     — accumulates multiple of the same id; itemCount tracks depth
```

All are `[Serializable]`. Key fields:
- `ItemId` (int, -1 = empty)
- `position` (`Vector2Int`) — logical grid coordinate
- `anchorPosition` (`Vector2Int`, default `(-1,-1)`) — for multi-cell items; non-anchor cells point to the anchor. The UI skips rendering items on non-anchor cells.
- `IsEnabled` (bool)

Key methods:
- `PutItem(GameObject)` — checks `AcceptsItem()` first; returns bool.
- `RemoveItem()` — returns the removed ID or -1.
- `AcceptsItem()` — false if occupied, disabled, or anchorPosition ≠ (-1,-1).

`StackableInventorySlot` additionally has `stackSize` (-1 = unlimited) and `itemCount`.

`IHideInSelections` marker interface excludes a slot type from editor slot-type dropdowns.

---

## Container Interface Hierarchy

**File:** `InventoryHelper.cs`

```
IItemContainer
├── ISingleItemContainer        — adds: GameObject? Item { get; }
├── IPositionalItemContainer    — adds: PutItem/RemoveItem/GetItem with Vector2Int
│   └── IUIPositionalItemContainer — adds: RedrawContents()
└── IUIItemContainer            — adds: RedrawContents()
```

`IPositionalItemContainer` provides default implementations of the non-positional `IItemContainer` members (delegates to position `(0,0)` or `(-1,-1)`).

---

## Grid System

### InventoryGrid

**File:** `Grid/InventoryGrid.cs` — `MonoBehaviour`, implements `IUIPositionalItemContainer`

Data: `Array2D<InventorySlot> Cells` (serialized, indexed `[x, y]`). Dimensions: `rows` / `columns` (1–20).

Key API:
- `PutItem(Vector2Int, GameObject)` — validates all cells covered by the item's `ItemShape` via `IsPlacementValid`, then sets the item ID in the anchor cell and writes `anchorPosition` into every other covered cell.
- `RemoveItem(Vector2Int)` — clears anchor references across all shape cells, then removes from anchor.
- `GetItem(Vector2Int)` — returns the item prefab at that cell.
- `ResizeIfNecessary()` — non-destructive resize: preserves existing cells, fills new ones with empty `InventorySlot`.
- `RedrawContents()` — delegates to `InventoryGridGenerator.GenerateUI()`.
- `ToggleInventory() / OpenInventory() / CloseInventory()` — delegate to generator.

`OnValidate` auto-adds `InventoryGridGenerator` via `Undo.AddComponent` (deferred, edit-mode only). The `suppressAutoAddUI` flag prevents duplicate adds.

### InventoryGridGenerator

**File:** `Grid/InventoryGridGenerator.cs` — `MonoBehaviour`, `ExecuteInEditMode`, `ISerializationCallbackReceiver`

Owns a `Dictionary<string, SlotUIDefinition> slotDefinitions` mapping slot type name → `{cellPrefab, disabledCellPrefab}`. This dictionary is serialized via a parallel `SerializableDictionary` field through `OnBeforeSerialize`/`OnAfterDeserialize`.

`GenerateUI()`:
1. Calls `inventoryGrid.ResizeIfNecessary()`.
2. Destroys the previous `lastGeneratedRoot`.
3. Creates a new root `GameObject` under `target` (Canvas), attaches a `GridLayoutGroup` using `InventoryManager.Instance.cellSize` and `spacing`.
4. Iterates `[y][x]` to call `GenerateSlot` for each cell.
5. Calls `InventoryHelper.GenerateDragLayer(target)`.

Per slot: instantiates the correct prefab (enabled vs disabled), adds `GridUISlot` component, calls `CreateSlotItem` to instantiate the item visual and add a `UIDragHandler`.

`SlotUIDefinition` is a `[Serializable]` struct with `cellPrefab` and `disabledCellPrefab`.

### GridUISlot

**File:** `Grid/GridUISlot.cs` — extends `PositionalUISlot`

Overrides `Item` to resolve through `anchorSlotPosition` when set (multi-cell redirect).

---

## List System

### InventoryList

**File:** `List/InventoryList.cs` — `MonoBehaviour`

Data: `List<StackableInventorySlot> contents`. All slots are stackable.

Settings: `EnableSlotCapacity` (bool), `capacity` (max slots when enabled), `scrollSensitivity`.

`OnValidate` auto-adds `InventoryListGenerator` (same deferred pattern as grid).

Provides `ToggleInventory() / OpenInventory() / CloseInventory()` delegating to `uiGenerator`.

### InventoryListUI

**File:** `List/InventoryListUI.cs` — `MonoBehaviour`, `IScrollHandler`, `IUIPositionalItemContainer`

Owns an `InventoryList` reference (`list`). Renders each `StackableInventorySlot` as a `slotPrefab` instance parented to an `OriginPoint` child, positioned vertically.

Key API:
- `PutItem(Vector2Int, GameObject)` — if `EnableSlotCapacity`, tries to stack into existing slot at `position.y`; otherwise searches all slots. Always creates a new `StackableInventorySlot` if no existing slot accepted the item.
- `RemoveItem(Vector2Int)` — removes one from `position.y` slot; drops the slot from `contents` when `itemCount` reaches 0.
- `GetItem(Vector2Int)` — reads `contents[position.y].Item`.
- `RedrawContents()` — clears all slot UI children, recreates origin point, calls `GenerateUI()`.
- `OnScroll` — scrolls the `OriginPoint` transform; clamps to overflow height.

Slot UI rendering uses `TextCountItemRenderer` (if present on prefab) to show item name and count.

### InventoryListGenerator

**File:** `List/InventoryListGenerator.cs` — editor helper to configure and generate the list UI.

---

## Drag and Drop

**File:** `InventoryItemUIHandler.cs`

`MonoBehaviour` implementing `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`, `IPointerEnterHandler`, `IPointerExitHandler`.

Flow:
1. `OnBeginDrag` — calls `_slot.RemoveItem()`, reparents self to `DragLayer`, instantiates a visual of the dragged item.
2. `OnDrag` — follows pointer via `RectTransformUtility.ScreenPointToLocalPointInRectangle`.
3. `OnEndDrag` — raycasts all UI elements, finds the first non-handler hit, resolves `IItemContainer` from it (or via `EventRedirector` for redirects). Calls `_target.PutItem(...)` or `_slot.ReturnItem(...)` on failure. Destroys self.

Tooltip: shown after 0.5 s hover via coroutine. Calls `Item.DisplayTooltip(canvas.transform)` / `HideTooltip()`. Auto-adjusts pivot to stay within canvas bounds.

`InventoryItemUIRedirectingHandler` extends this for non-anchor cells: `GetOriginalParent` walks up to the real slot, and `targetPosition` redirects drops to the anchor cell.

The **DragLayer** is a full-canvas transparent `RectTransform` that sits as the last sibling. Created by `InventoryHelper.GenerateDragLayer(Canvas)`. Items are reparented here during drag so they render above everything.

`InventoryHelper.CreateUIDragHandler(parent, redirector)` — creates a transparent, raycast-blocking `RectTransform` + `CanvasGroup` (ignores parent groups) child on a slot. `redirector = true` adds `InventoryItemUIRedirectingHandler` instead.

---

## PlayerInventoryService

**File:** `PlayerInventoryService.cs`

Singleton `MonoBehaviour` bridge between quest/game code and the player's `InventoryListUI`. Add to any persistent `GameObject`; auto-finds `InventoryListUI` in scene on `Start` if not assigned.

API:
- `CountItem(int itemId)` → total stack count across all slots.
- `HasItem(int itemId, int count = 1)` → bool.
- `TakeItem(int itemId, int count = 1)` → removes `count` from inventory; no-op and returns false if insufficient.
- `GiveItem(int itemId, int count = 1)` → looks up prefab via `InventoryManager`, calls `PutItem` once per count.

---

## Shopping

**File:** `Shopping/Shopkeep.cs`

Stub. Extends `Interactable`, exposes `public IItemContainer inventory`. No behaviour implemented yet.

---

## Editor Tools

| File | Purpose |
|------|---------|
| `Editor/Inventory/InventoryManagerContext.cs` | Context menu / inspector actions for InventoryManager |
| `Editor/Inventory/InventoryGridHierarchyContext.cs` | Hierarchy context menu for InventoryGrid GameObjects |
| `Editor/Inventory/InventoryListHierarchyContext.cs` | Hierarchy context menu for InventoryList GameObjects |
| `Editor/Inventory/InventorySlotDrawer.cs` | `PropertyDrawer` for `InventorySlot` in the Inspector |
| `Grid/InventoryGridEditor.cs` | Custom Editor for `InventoryGrid` |
| `Grid/InventoryGridGeneratorEditor.cs` | Custom Editor for `InventoryGridGenerator` |
| `List/InventoryListGenerator.cs` | Runtime/editor list UI generator |
| `List/InventoryListGeneratorEditor.cs` | Custom Editor for `InventoryListGenerator` |
| `InventoryManagerEditor.cs` | Custom Editor for `InventoryManager` |

---

## Key Constraints

- **Item ID is the only cross-system key.** Nothing stores a direct `GameObject` reference at runtime; all lookups go through `InventoryManager.GetItemById`.
- **`InventoryManager.ReloadItems(path)` must be called before any inventory can display items.** Forgetting this causes `GetItemById` to return null for all IDs.
- **Multi-cell items use anchor + satellite pattern.** The item ID and the item visual live only in the anchor slot; satellite slots hold `anchorPosition` and render a redirect drag handler. Never call `RemoveItem` on a satellite slot directly.
- **`InventoryGridGenerator.slotDefinitions` must have entries for every `InventorySlot` subclass** that appears in a grid, otherwise `GenerateUI` logs a warning and skips the cell.
- **`InventoryList` always uses `StackableInventorySlot`.** The grid can use any `InventorySlot` subtype per cell.
- **`PlayerInventoryService` operates on `InventoryListUI` only**, not on the grid. Quest nodes that grant/require items must use the list-based inventory or interact via `PlayerInventoryService`.
- **`DragLayer` must exist under the Canvas** for drag-and-drop to work. `InventoryHelper.GenerateDragLayer` is idempotent (returns existing layer if present). It is created automatically by `InventoryGridGenerator.GenerateUI`; for list-based UIs the drag layer must exist beforehand.
