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

        [Header("Price Label")]
        [Tooltip("Optional prefab for the price tag. Must contain a TMP_Text (or a child named 'Price' with one). If null, a procedural label is generated.")]
        public GameObject priceLabelPrefab;

        private IShopCurrency Currency => (_currencyProvider as IShopCurrency) ?? PlayerWallet.Instance;

        private int _pendingRefundItemId = -1;
        private int _pendingRefundPrice = 0;

        private void ConsumePendingRefund(int itemId)
        {
            if (_pendingRefundItemId != itemId) return;
            Currency.Add(_pendingRefundPrice);
            shopInventory?.listings.FirstOrDefault(s => s.itemId == itemId)?.UndoSell();
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
            ConsumePendingRefund(item.GetComponent<Item>()?.info?.id ?? -1);
            PutItem(position, item);
        }

        public override bool PutItem(Vector2Int position, GameObject item)
        {
            bool placed = base.PutItem(position, item);
            if (placed)
                ConsumePendingRefund(item.GetComponent<Item>()?.info?.id ?? -1);
            return placed;
        }

        protected override bool AutoPlaceItem(GameObject item)
        {
            bool placed = base.AutoPlaceItem(item);
            if (placed)
                ConsumePendingRefund(item.GetComponent<Item>()?.info?.id ?? -1);
            return placed;
        }

        public override int RemoveItem(Vector2Int position)
        {
            if (!Cells.TryGet(position.x, position.y, out var cell) || cell.Item == null)
                return base.RemoveItem(position);

            var item = cell.Item.GetComponent<Item>();
            int itemId = item?.info?.id ?? -1;
            int price = GetPrice(itemId);

            if (!Currency.TrySpend(price))
                return -1;

            _pendingRefundItemId = itemId;
            _pendingRefundPrice = price;
            shopInventory?.listings.FirstOrDefault(s => s.itemId == itemId)?.TrySell();
            return base.RemoveItem(position);
        }

        private void PopulateItems()
        {
            ResizeIfNecessary();
            foreach (var listing in shopInventory.listings)
            {
                if (listing.IsAvailable)
                    TryAddItem(listing.itemId);
            }
        }

        private int GetPrice(int itemId)
        {
            if (shopInventory == null) return 0;
            return shopInventory.listings.FirstOrDefault(s => s.itemId == itemId)?.price ?? 0;
        }
    }
}
