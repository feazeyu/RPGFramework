using Feazeyu.RPGSystems.Items;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>
    /// A shop rendered as a scrollable list. It is an <see cref="InventoryList"/> pre-stocked from a
    /// <see cref="ShopInventory"/>, so it reuses the standard list rendering and drag-and-drop
    /// (<see cref="InventoryListGenerator"/> / <see cref="InventoryListUI"/>) — mirroring how
    /// <see cref="ShopGridUI"/> builds on <see cref="InventoryGrid"/>.
    ///
    /// A listing's stock is its slot's stack depth: dragging an item out buys one (charging the
    /// player), dragging an item in sells it back (paying the player). Infinite-stock listings never
    /// deplete and render as "∞". Each instance runs against a <see cref="ShopInventory.CloneForRuntime"/>
    /// copy, so buying/selling never mutates the authored asset.
    ///
    /// Requires an <see cref="InventoryListGenerator"/> on the same GameObject (added automatically via
    /// the base OnValidate); set its slotPrefab (with a <see cref="TextCountItemRenderer"/>) and canvas.
    /// </summary>
    public class ShopListUI : InventoryList
    {
        /// <summary>Shop inventory.</summary>
        [Header("Shop")]
        public ShopInventory shopInventory;

        [Header("Currency")]
        [Tooltip("MonoBehaviour implementing IShopCurrency. Falls back to PlayerWallet singleton if null.")]
        [SerializeField] private MonoBehaviour _currencyProvider;

        [Header("Selling")]
        [Tooltip("Fraction of buy price refunded when selling an item to this shop (0 = no refund, 1 = full price).")]
        [Range(0f, 1f)]
        [SerializeField] private float _sellRatio = 0.5f;

        private IShopCurrency Currency => (_currencyProvider as IShopCurrency) ?? PlayerWallet.Instance;

        private int _pendingRefundItemId = -1;
        private int _pendingRefundPrice = 0;

        private bool _isOpen;

        private ShopInventory _runtimeInventory;

        /// <inheritdoc/>
        protected override void OnValidate()
        {
            base.OnValidate();
        }

        private void Awake()
        {
            if (shopInventory != null)
                Rebuild();
        }

        private void OnDestroy()
        {
            if (_runtimeInventory != null) Destroy(_runtimeInventory);
        }

        private void Update()
        {
            if (_isOpen && Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame)
                CloseInventory();
        }

        /// <inheritdoc/>
        public override void OpenInventory() { base.OpenInventory(); _isOpen = true; }
        /// <inheritdoc/>
        public override void CloseInventory() { base.CloseInventory(); _isOpen = false; }
        /// <inheritdoc/>
        public override void ToggleInventory() { base.ToggleInventory(); _isOpen = !_isOpen; }

        /// <summary>Points the shop at a new inventory and rebuilds it. Called by <see cref="Shopkeep"/>.</summary>
        public void Setup(ShopInventory inventory)
        {
            shopInventory = inventory;
            Rebuild();
        }

        /// <summary>
        /// Swaps <see cref="shopInventory"/> for a runtime clone if it currently points at an authored asset.
        /// </summary>
        private void EnsureRuntimeInventory()
        {
            if (shopInventory == null || shopInventory == _runtimeInventory) return;
            if (_runtimeInventory != null) Destroy(_runtimeInventory);
            _runtimeInventory = shopInventory.CloneForRuntime();
            shopInventory = _runtimeInventory;
        }

        /// <summary>
        /// Clones the inventory, stocks the list from its listings, then generates the list UI (closed).
        /// </summary>
        private void Rebuild()
        {
            EnsureRuntimeInventory();
            StockListings();

            var gen = uiGenerator;
            if (gen == null)
            {
                Debug.LogWarning("[ShopListUI] No InventoryListGenerator found — cannot render the list shop.", this);
                return;
            }

            gen.DrawContents();
            if (gen.targetCanvas != null)
                InventoryHelper.GenerateDragLayer(gen.targetCanvas);
            CloseInventory();
        }

        /// <summary>
        /// Fills <see cref="InventoryList.contents"/> with one stack per in-stock listing. A listing's
        /// stock becomes the stack's <c>itemCount</c>; negative stock makes the stack infinite.
        /// </summary>
        private void StockListings()
        {
            contents = new();
            if (shopInventory == null) return;

            foreach (var listing in shopInventory.listings)
            {
                if (listing.stock == 0) continue;
                if (InventoryManager.Instance?.GetItemById(listing.itemId) == null) continue;

                var slot = new StackableInventorySlot(listing.itemId);
                if (listing.stock < 0)
                    slot.infinite = true;
                else
                    slot.itemCount = listing.stock;
                contents.Add(slot);
            }
        }


        /// <inheritdoc/>
        public override int RemoveItem(Vector2Int position)
        {
            if (contents == null || position.y < 0 || position.y >= contents.Count)
                return -1;

            int itemId = contents[position.y].ItemId;
            int price = GetPrice(itemId);

            if (!Currency.TrySpend(price))
                return -1;

            _pendingRefundItemId = itemId;
            _pendingRefundPrice = price;

            return base.RemoveItem(position);
        }

        /// <inheritdoc/>
        public override bool PutItem(Vector2Int position, GameObject item)
        {
            int itemId = item.GetComponent<Item>()?.info?.id ?? -1;

            if (_pendingRefundItemId == itemId)
            {
                bool placed = base.PutItem(position, item);
                if (placed) ConsumePendingRefund(itemId);
                return placed;
            }

            int buyPrice = GetPrice(itemId);
            if (buyPrice <= 0) return false;

            bool sold = base.PutItem(position, item);
            if (sold)
                Currency.Add(Mathf.FloorToInt(buyPrice * _sellRatio));
            return sold;
        }

        private void ConsumePendingRefund(int itemId)
        {
            if (_pendingRefundItemId != itemId) return;
            Currency.Add(_pendingRefundPrice);
            _pendingRefundItemId = -1;
            _pendingRefundPrice = 0;
        }


        /// <inheritdoc/>
        public override string GetItemLabel(StackableInventorySlot slot)
        {
            int price = GetPrice(slot.ItemId);
            string name = base.GetItemLabel(slot);
            return price > 0 ? $"{name}  -  {price}g" : name;
        }

        private int GetPrice(int itemId)
        {
            if (shopInventory == null) return 0;
            return shopInventory.listings.FirstOrDefault(s => s.itemId == itemId)?.price ?? 0;
        }
    }
}
