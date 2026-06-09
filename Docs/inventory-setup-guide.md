# Inventory System — Developer Guide

---

## Part 1: Implementing New Inventories and Item Types

### 1.1 Create the ItemInfo ScriptableObject

Right-click in the Project window → **Create → RPGFramework → Items → ItemInfo**.

Fill in every field:

| Field | Notes |
|---|---|
| `id` | Must be **globally unique** across all items. The InventoryManager indexes by this int at runtime. |
| `_name` | Display name shown in tooltips. |
| `_icon` | Sprite used in list UI and tooltips. |
| `_tier` | 1–5. Drives colour tints in generated UI. |
| `_description` | Tooltip body text. |
| `_shape` | Click cells in the custom 5×5 grid drawer to define footprint. Hit **Normalize** to trim dead space. |
| `_statEffects` | Add `StatEffectF` entries if the item should modify stats on equip/use. |
| `_target` | Leave as `Player` unless you extend `ItemTarget`. |

### 1.2 Create the Item Prefab

1. Create a UI `GameObject` with a `RectTransform`.
2. Add a **`Sprite Renderer`** (or `Image`) sized to `InventoryManager.cellSize × shape.Positions.Length` roughly.
3. Add the **`Item`** MonoBehaviour. Drag your new `ItemInfo` asset into the `ItemInfo` slot.
4. The `Item` component computes `GetAnchorSlot()` automatically from the shape, but verify it in Play mode with a grid.
5. If you want a custom tooltip body, create a description override prefab and assign it to `Item._descriptionOverridePrefab`.
6. Save the prefab under **`Assets/Resources/Items/`**. The folder name and location are hardcoded in `InventoryManager.cs` (`Resources.LoadAll<Item>("Items/")`).

That is all that is required. No subclassing of `Item` or `ItemInfo` is needed for pure data variation.

### 1.3 Differentiating Item "Types" (Weapons, Consumables, Armour, etc.)

The framework is intentionally content-driven — there is no `WeaponItem` subclass. You have two patterns to choose from:

**Pattern A — Extend `ItemInfo` with a subclass (recommended for structured data)**

```csharp
[CreateAssetMenu(menuName = "RPGFramework/Items/WeaponInfo")]
public class WeaponInfo : ItemInfo
{
    public float attackSpeed;
    public DamageType damageType;
    public GameObject projectilePrefab; // for ranged
}
```

Then extend `Item` to read it:

```csharp
public class WeaponItem : Item
{
    public WeaponInfo WeaponInfo => (WeaponInfo)ItemInfo;

    public void Equip(Entity entity)
    {
        // apply WeaponInfo.statEffects via entity.Stats, spawn weapon, etc.
    }
}
```

Attach `WeaponItem` instead of plain `Item` to weapon prefabs.

**Pattern B — Tag via a separate component (recommended for behaviour-only variation)**

Add a lightweight MonoBehaviour (`ConsumableComponent`, `EquippableComponent`, …) to the prefab alongside `Item`. The inventory slot or an equipment system can query `GetComponent<ConsumableComponent>()` on the retrieved `GameObject`.

### 1.4 Custom ItemShape

Open the `ItemInfo` in the Inspector. The shape drawer shows a 5×5 grid — click to toggle cells. For shapes larger than 5 cells in any axis you will need to widen `GetBoolGrid(dimension)` in `ItemShape.cs` and resize the property drawer in `ItemShapePropertyDrawer`. The internal `Vector2Int[] Positions` array supports any size; the drawer is the only 5×5 constraint.

---

## Part 2: Adding a New Grid Inventory

### 2.1 Scene Setup

1. Create a `Canvas` (or reuse an existing one).
2. Inside it, add an empty `GameObject` — name it `ChestGrid`.
3. Add the **`InventoryGrid`** MonoBehaviour.
4. Set `rows` and `columns` (1–20 each).
5. On **OnValidate** (i.e., immediately in the Inspector), Unity automatically adds an `InventoryGridGenerator` sibling component. Configure it:
   - `SlotUIDefinition`: map slot type names (`"InventorySlot"`, `"LockedInventorySlot"`) to enabled/disabled slot prefabs.
   - Use the provided sample prefabs from the Inventory Demo package as a starting point.
6. Enter Play mode — `InventoryGridGenerator.GenerateGrid()` runs and creates the `GridLayoutGroup` with one cell per slot.

### 2.2 Customising Slot Types Per Position

After creating the grid you can change individual slot types before runtime:

```csharp
InventoryGrid grid = GetComponent<InventoryGrid>();
// Lock the bottom-right corner:
grid.Cells[rows-1, cols-1] = new LockedInventorySlot { position = new Vector2Int(cols-1, rows-1) };
```

Or subclass `InventorySlot` for specialised acceptance logic:

```csharp
public class WeaponOnlySlot : InventorySlot
{
    public override bool AcceptsItem(GameObject item)
    {
        return item.GetComponent<WeaponItem>() != null;
    }
}
```

Register this type in `InventoryGridGenerator.SlotUIDefinition` with its own prefab.

### 2.3 Putting Items In Programmatically

```csharp
// By position:
grid.PutItem(new Vector2Int(0, 0), itemGameObject);

// Auto-place (scans for first free space):
grid.TryAddItem(itemId, count: 1);

// Retrieve:
GameObject go = grid.GetItem(new Vector2Int(0, 0));

// Remove:
grid.RemoveItem(new Vector2Int(0, 0));
```

`TryAddItem` resolves the prefab via `InventoryManager.GetItem(id)`, instantiates it, and calls `PutItem` internally.

---

## Part 3: Adding a New List Inventory

### 3.1 Scene Setup

1. Add an empty `GameObject` → `InventoryList`.
2. Attach the **`InventoryList`** MonoBehaviour.
3. Optionally set `stackSize` per slot, or leave `-1` for unlimited stacking.
4. `InventoryListGenerator` is auto-added via `OnValidate`. Assign a scroll container prefab if you want a scrollable list; otherwise a basic vertical layout is generated.
5. Play mode generates `InventoryListUI` which manages slot rendering.

### 3.2 Programmatic Use

```csharp
InventoryList list = GetComponent<InventoryList>();

list.PutItem(itemGameObject);            // appends or stacks
list.RemoveItem(itemId, count: 1);
int count = list.CountItem(itemId);
list.TryAddItem(itemId, count: 5);
```

---

## Part 4: Adding a New Equipment / Specialised Inventory

For equipment slots (Head, Chest, Legs, Weapon, Off-hand), the cleanest approach is to compose `PositionalUISlot` instances rather than reusing `InventoryGrid`.

```csharp
public class EquipmentPanel : MonoBehaviour
{
    [SerializeField] PositionalUISlot headSlot;
    [SerializeField] PositionalUISlot weaponSlot;

    void OnEnable()
    {
        headSlot.OnItemDropped   += OnEquip;
        weaponSlot.OnItemDropped += OnEquip;
    }

    void OnEquip(GameObject item)
    {
        var equippable = item.GetComponent<EquippableComponent>();
        equippable?.Equip(player);
    }
}
```

`PositionalUISlot` implements `IDropHandler`, so it automatically participates in the existing drag-drop system. Assign each slot a `PositionalUISlot` prefab in the scene and wire its `targetContainer` to the `ISingleItemContainer` you want to own it.

---

## Part 5: Integrating a New Inventory with the Shop

The shop system (`ShopInventory` ScriptableObject + `ShopGridUI`/`ShopListUI`) is independent. To sell items from a new inventory:

1. Create a `ShopInventory` asset. Add `ShopSlot` entries (each references an `ItemInfo` and a price).
2. Add `ShopGridUI` or `ShopListUI` to a `Canvas`. Reference a `PlayerWallet` component on the player.
3. Open the shop by calling `shopUI.Open(shopInventory)`. Items purchased fire the standard `TryAddItem` path into the player's inventory.

---

## Part 6: Common Pitfalls

| Pitfall | Fix |
|---|---|
| Item not found at runtime | Prefab must be inside `Assets/Resources/Items/`. The subfolder name is hardcoded. |
| Duplicate item IDs | IDs are `int` keys in a `SerializableDictionary`. Check existing assets before assigning a new ID. |
| Multi-cell item renders ghost copies | Non-anchor cells must have `InventoryItemUIRedirectingHandler`, not `InventoryItemUIHandler`. `InventoryGridGenerator` does this automatically; only manually-built prefabs may miss it. |
| Shape looks wrong in grid | Hit **Normalize** in the `ItemShape` drawer to strip empty rows/columns. The anchor slot is the cell closest to the shape centre. |
| Drag drops item onto wrong canvas layer | `DragLayer` is generated once per `Canvas` root. If you have nested canvases, call `InventoryHelper.GenerateDragLayer(canvas)` for each one. |
| `InventoryGrid` rows/columns not reflected in UI | `InventoryGridGenerator.GenerateGrid()` only runs in Play mode (via `Start`). Changes to dimensions require re-entering Play mode. |
| Custom `InventorySlot` subclass not rendered | Register the type name and its prefabs in `InventoryGridGenerator.SlotUIDefinition`. |

---

## Part 7: Setting Up Inventories in a New Project

There are three things every scene needs before any inventory works, then separate steps for each inventory type.

### Prerequisites (every scene)

#### Canvas

Add a standard Unity UI Canvas to your scene. Both generators fall back to `FindFirstObjectByType<Canvas>()` if you don't assign one explicitly, but they'll log a warning every time. One Canvas is enough for any number of inventories.

Make sure the Canvas has:
- A **Graphic Raycaster** (needed for drag-drop event routing)
- An **EventSystem** in the scene (created automatically when you add a Canvas via the menu)

#### InventoryManager

Add an empty `GameObject` to the scene, name it `InventoryManager`, and attach the **`InventoryManager`** component. It is a singleton with `DefaultExecutionOrder(-100)`, so it initialises before any inventory.

Two things to set in its Inspector:
- **Cell Size** — the pixel dimensions of one grid slot (e.g. `100 × 100`). Every grid in the scene shares this value.
- Items are loaded automatically from `Resources/Items/` on `Awake`. No further configuration needed here, but that folder must exist and be populated for `TryAddItem(id)` calls to work.

If you don't add it manually, the system creates one automatically (`new GameObject("InventoryManager")`), but it will have default cell size and an empty item dictionary until items are loaded.

#### Item Prefabs in Resources

Any prefab you want to retrieve by ID at runtime must live under `Assets/Resources/Items/` and carry an **`Item`** component whose `ItemInfo.id` is unique across all prefabs. The manager loads the entire folder on `Awake`.

---

### InventoryGrid Setup

#### Step 1 — Add the component

Add an empty `GameObject` inside (or near) your Canvas and attach **`InventoryGrid`**. The moment you do, `OnValidate` fires and automatically adds **`InventoryGridGenerator`** as a sibling component via Undo.

#### Step 2 — Configure dimensions

On the `InventoryGrid` Inspector, set **Rows** and **Columns** (1–20 each).

#### Step 3 — Configure the generator

On the **`InventoryGridGenerator`** Inspector:

| Field | What to do |
|---|---|
| **Slot Definitions** | A dictionary keyed by slot type name. At minimum you need an entry for `"InventorySlot"`. Each entry has a `cellPrefab` (enabled state) and a `disabledCellPrefab`. Assign prefabs for both. |
| **Target** | Drag in your Canvas. If left empty it auto-finds one, with a warning. |
| **Spacing** | Pixel gap between cells. |
| **UI Position** | Anchored position of the grid relative to the anchor point. |

**What the slot prefabs need:** A `RectTransform` sized to match `InventoryManager.cellSize`. The generator instantiates one per cell and adds a `GridUISlot` component to it at runtime — you don't need to add that yourself. A plain panel `Image` is enough as a starting point; use the sample prefabs from the Inventory Demo as a reference.

#### Step 4 — DragLayer

The `DragLayer` is created automatically by `GenerateUI()` (called from `Awake`). It's a full-stretch transparent child of your Canvas that items are re-parented to during a drag. You don't need to create it manually; just don't delete it at runtime.

#### Step 5 — Enter Play mode

`InventoryGrid.Awake()` calls `RedrawContents()` → `InventoryGridGenerator.GenerateUI()`, which builds the `GridLayoutGroup` and all slot cells under the Canvas. The grid is now live.

---

### InventoryList Setup

#### Step 1 — Add the component

Add an empty `GameObject` and attach **`InventoryList`**. `OnValidate` auto-adds **`InventoryListGenerator`** and sets its `list` reference.

#### Step 2 — Configure the list

On the `InventoryList` Inspector:

| Field | What to do |
|---|---|
| **Enable Slot Capacity** | Turn on if you want a per-slot item count limit. |
| **Capacity** | Max items per stack when the above is enabled. |
| **Scroll Sensitivity** | Pixels scrolled per scroll step. |

#### Step 3 — Configure the generator

On the **`InventoryListGenerator`** Inspector:

| Field | What to do |
|---|---|
| **Slot Prefab** | A prefab rendered for each item in the list. Needs a `RectTransform` and a `TextCountItemRenderer` component for name/count display. Required — the list won't draw without it. |
| **Target Canvas** | Drag in your Canvas. Falls back to auto-find with a warning. |
| **Inventory Container Override** | Optional. A prefab containing an `InventoryListUI` component — use this to wrap the list in a scroll rect or styled panel. If left empty, a plain `GameObject` is created. |
| **Inventory Name** | Name of the generated root object, useful for debugging. |
| **First Element Position** | Position of the first slot relative to the container. |
| **Margin** | Spacing between slots. |

#### Step 4 — Initialise the UI

Unlike `InventoryGrid`, the list UI is not drawn by `Awake`. It's drawn the first time `RedrawContents()` is called, which happens whenever items are added or removed. To draw an empty list at startup (so the container appears), call `uiGenerator.DrawContents()` from your own bootstrap script, or just add an item.

---

### Minimum Working Scene Checklist

```
Scene
├── EventSystem
├── Canvas  (Graphic Raycaster)
├── InventoryManager  (cellSize set, items in Resources/Items/)
├── InventoryGridObject  (InventoryGrid + InventoryGridGenerator)
│     └── Generator: slotDefinitions["InventorySlot"] → slotPrefab assigned
│                    target → Canvas
└── InventoryListObject  (InventoryList + InventoryListGenerator)
      └── Generator: slotPrefab → assigned
                     targetCanvas → Canvas
```

At runtime the grid draws itself on `Awake`. The list draws on first data change. Both support drag-drop between each other out of the box as long as they share the same Canvas (and therefore the same `DragLayer`).
