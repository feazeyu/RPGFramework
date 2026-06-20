using Feazeyu.RPGSystems.Core.Utilities;
using Feazeyu.RPGSystems.Items;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>
    /// A shop that renders exactly like InventoryGrid. Items are pre-populated from a ShopInventory.
    /// Dragging an item out charges the player via IShopCurrency; if they cannot afford it, the drag is blocked.
    /// Requires an InventoryGridGenerator on the same GameObject (added automatically via OnValidate).
    /// </summary>
    public class ShopGridUI : InventoryGrid, IPositionalItemContainer
    {
        [Header("Shop")]
        public ShopInventory shopInventory;

        [Header("Currency")]
        [Tooltip("MonoBehaviour implementing IShopCurrency. Falls back to PlayerWallet singleton if null.")]
        [SerializeField] private MonoBehaviour _currencyProvider;

        [Header("Selling")]
        [Tooltip("Fraction of buy price refunded when selling an item to this shop (0 = no refund, 1 = full price).")]
        [Range(0f, 1f)]
        [SerializeField] private float _sellRatio = 0.5f;

        [Header("Price Label")]
        [Tooltip("Optional prefab for the price tag. Must contain a TMP_Text (or a child named 'Price' with one). If null, a procedural label is generated.")]
        public GameObject priceLabelPrefab;

        private IShopCurrency Currency => (_currencyProvider as IShopCurrency) ?? PlayerWallet.Instance;

        private int _pendingRefundItemId = -1;
        private int _pendingRefundPrice = 0;

        // The working copy the shop actually runs against. Buying/selling mutates ShopSlot.stock,
        // so we never operate on the authored asset (see ShopInventory.CloneForRuntime).
        private ShopInventory _runtimeInventory;

        /// <summary>
        /// Swaps <see cref="shopInventory"/> for a runtime clone if it currently points at an
        /// authored asset. Called from the populate path so every entry point (Awake, Setup,
        /// or a direct shopInventory assignment) is covered.
        /// </summary>
        private void EnsureRuntimeInventory()
        {
            if (shopInventory == null || shopInventory == _runtimeInventory) return;
            if (_runtimeInventory != null) Destroy(_runtimeInventory);
            _runtimeInventory = shopInventory.CloneForRuntime();
            shopInventory = _runtimeInventory;
        }

        private void OnDestroy()
        {
            if (_runtimeInventory != null) Destroy(_runtimeInventory);
        }

        private void ConsumePendingRefund(int itemId)
        {
            if (_pendingRefundItemId != itemId) return;
            // The stack count is the authoritative stock; returning the item re-stacks it (see
            // ReturnItem), so the refund only needs to give the player's money back.
            Currency.Add(_pendingRefundPrice);
            _pendingRefundItemId = -1;
            _pendingRefundPrice = 0;
        }

        protected override void Awake()
        {
            if (shopInventory != null)
                PopulateItems();
            base.Awake();
            CloseInventory();
        }

        public void Setup(ShopInventory inventory)
        {
            shopInventory = inventory;
            Clear();
            PopulateItems();
            RedrawContents();
        }

        public override void RedrawContents()
        {
            base.RedrawContents();
            AddPriceLabels();
        }

        private void AddPriceLabels()
        {
            if (shopInventory == null) return;
            var gen = GetComponent<InventoryGridGenerator>();
            if (gen?.lastGeneratedRoot == null) return;
            int baseOrder = gen.target != null ? gen.target.sortingOrder : 0;
            int labelOrder = baseOrder + rows * columns + 1;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    var itemGo = GetItem(new Vector2Int(x, y));
                    if (itemGo == null) continue;

                    var bottomRight = GetBottomRightCell(new Vector2Int(x, y), itemGo);
                    var cell = gen.lastGeneratedRoot.transform.Find($"Cell_{bottomRight.x}_{bottomRight.y}");
                    if (cell == null) continue;

                    int itemId = itemGo.GetComponent<Item>()?.info?.id ?? -1;
                    AddPriceLabel(cell.gameObject, GetPrice(itemId), labelOrder);
                }
            }
        }

        private Vector2Int GetBottomRightCell(Vector2Int anchorGridPos, GameObject itemGo)
        {
            var item = itemGo.GetComponent<Item>();
            var center = item.GetAnchorSlot();
            var bottomRight = anchorGridPos;
            foreach (var shapePos in item.info.Shape.Positions)
            {
                var gridPos = new Vector2Int(anchorGridPos.x + shapePos.x - center.x, anchorGridPos.y + shapePos.y - center.y);
                if (gridPos.y > bottomRight.y || (gridPos.y == bottomRight.y && gridPos.x > bottomRight.x))
                    bottomRight = gridPos;
            }
            return bottomRight;
        }

        private void AddPriceLabel(GameObject slotGo, int price, int sortingOrder)
        {
            if (priceLabelPrefab != null)
            {
                var instance = Instantiate(priceLabelPrefab, slotGo.transform, false);
                instance.name = "PriceLabel";
                var canvas = instance.GetComponent<Canvas>() ?? instance.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = sortingOrder;
                var priceText = instance.transform.Find("Price")?.GetComponent<TMP_Text>()
                    ?? instance.GetComponentInChildren<TMP_Text>();
                if (priceText != null)
                    priceText.text = $"{price}g";
                return;
            }

            var labelGo = new GameObject("PriceLabel");
            labelGo.transform.SetParent(slotGo.transform, false);
            var rt = labelGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var labelCanvas = labelGo.AddComponent<Canvas>();
            labelCanvas.overrideSorting = true;
            labelCanvas.sortingOrder = sortingOrder;
            var text = labelGo.AddComponent<TextMeshProUGUI>();
            text.text = $"{price}g";
            text.fontSize = 10;
            text.color = Color.yellow;
            text.alignment = TextAlignmentOptions.BottomRight;
            text.raycastTarget = false;
        }

        void IPositionalItemContainer.ReturnItem(Vector2Int position, GameObject item)
        {
            // Item is returning to its slot in the shop after a failed drop elsewhere — undo the purchase.
            ConsumePendingRefund(item.GetComponent<Item>()?.info?.id ?? -1);
            base.PutItem(position, item);
        }

        public override bool PutItem(Vector2Int position, GameObject item)
        {
            int itemId = item.GetComponent<Item>()?.info?.id ?? -1;

            if (_pendingRefundItemId == itemId)
            {
                // Intentional return of an item just bought from this shop — full refund.
                bool placed = base.PutItem(position, item);
                if (placed) ConsumePendingRefund(itemId);
                return placed;
            }

            return TrySellToShop(item, itemId);
        }

        protected override bool TryAddItem(GameObject item)
        {
            int itemId = item.GetComponent<Item>()?.info?.id ?? -1;

            if (_pendingRefundItemId == itemId)
            {
                bool placed = base.TryAddItem(item);
                if (placed) ConsumePendingRefund(itemId);
                return placed;
            }

            return TrySellToShop(item, itemId);
        }

        private bool TrySellToShop(GameObject item, int itemId)
        {
            int buyPrice = GetPrice(itemId);
            if (buyPrice <= 0) return false; // item not in this shop's listings — reject

            // Route through base.TryAddItem (not a position-specific put) so the sold item always
            // merges into the listing's existing stack instead of spawning a second slot —
            // regardless of which cell it was dropped on.
            bool placed = base.TryAddItem(item);
            if (placed)
            {
                // The shop's stock (stack count) grows by one instead of duplicating the offering.
                Currency.Add(Mathf.FloorToInt(buyPrice * _sellRatio));
                RedrawContents();
            }
            return placed;
        }

        public override int RemoveItem(Vector2Int position)
        {
            if (!Cells.TryGet(position.x, position.y, out var cell) || cell.Item == null)
                return base.RemoveItem(position);

            int itemId = cell.Item.GetComponent<Item>()?.info?.id ?? -1;
            int price = GetPrice(itemId);

            if (!Currency.TrySpend(price))
                return -1;

            _pendingRefundItemId = itemId;
            _pendingRefundPrice = price;

            // Stock is the stack count: buying takes one off the stack. The base decrements a
            // deep stack (or clears the last one), and never depletes an infinite stack — so the
            // offering stays on display with its remaining count until it actually runs out.
            return base.RemoveItem(position);
        }

        private void PopulateItems()
        {
            EnsureRuntimeInventory();
            allowStacking = true;                          // a listing's stock is its stack depth
            Cells = new Array2D<InventorySlot>(0, 0);      // force fresh StackableInventorySlot cells
            ResizeIfNecessary();
            foreach (var listing in shopInventory.listings)
            {
                if (listing.stock == 0) continue;          // nothing in stock

                // Stock the shop with its own inventory. This must NOT go through the
                // overridden TryAddItem, which treats an incoming item as a player
                // *selling* to the shop and pays out via Currency.Add. Place directly
                // through the base grid so populating the shop costs the player nothing.
                var prefab = InventoryManager.Instance?.GetItemById(listing.itemId);
                if (prefab == null) continue;

                var instance = Instantiate(prefab);
                if (!base.TryAddItem(instance))
                {
                    Destroy(instance);
                    continue;
                }
                SeedStock(listing);
            }
        }

        /// <summary>
        /// Sets the stack count of the just-placed listing to its stock (negative stock = infinite).
        /// </summary>
        private void SeedStock(ShopSlot listing)
        {
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (Cells.TryGet(x, y, out var cell)
                        && cell is StackableInventorySlot s
                        && s.anchorPosition == new Vector2Int(-1, -1)
                        && s.ItemId == listing.itemId)
                    {
                        if (listing.stock < 0)
                        {
                            s.infinite = true;
                        }
                        else
                        {
                            s.infinite = false;
                            s.itemCount = listing.stock;
                        }
                        return;
                    }
                }
            }
        }

        private int GetPrice(int itemId)
        {
            if (shopInventory == null) return 0;
            return shopInventory.listings.FirstOrDefault(s => s.itemId == itemId)?.price ?? 0;
        }
    }
}
