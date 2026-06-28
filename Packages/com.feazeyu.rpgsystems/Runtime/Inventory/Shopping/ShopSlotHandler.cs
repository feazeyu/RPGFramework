using Feazeyu.RPGSystems.Items;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>
    /// Attach to each shop slot UI element. Handles click-to-buy and slot display.
    /// The shop UI generators wire this up automatically; configure optional UI references
    /// in the slot prefab by naming children "Name", "Price", "Stock", "Icon".
    /// </summary>
    public class ShopSlotHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [HideInInspector] public ShopSlot slot;

        /// <summary>Name text.</summary>
        [Header("UI — auto-wired from named children if left null")]
        public TMP_Text nameText;
        /// <summary>Price text.</summary>
        public TMP_Text priceText;
        /// <summary>Stock text.</summary>
        public TMP_Text stockText;
        /// <summary>Icon image.</summary>
        public Image iconImage;
        /// <summary>Highlight.</summary>
        public Graphic highlight;

        /// <summary>On purchased.</summary>
        public UnityEvent<ShopSlot> OnPurchased;
        /// <summary>On purchase failed.</summary>
        public UnityEvent<ShopSlot> OnPurchaseFailed;

        private IShopCurrency _currency;
        private IItemContainer _buyerInventory;
        private Color _defaultHighlightColor;

        /// <summary>Setup.</summary>
        public void Setup(ShopSlot shopSlot, IShopCurrency currency, IItemContainer buyerInventory)
        {
            slot = shopSlot;
            _currency = currency;
            _buyerInventory = buyerInventory;
            if (highlight != null)
                _defaultHighlightColor = highlight.color;
            Refresh();
        }

        /// <summary>Refresh.</summary>
        public void Refresh()
        {
            if (slot == null) return;

            var prefab = InventoryManager.Instance?.GetItemById(slot.itemId);
            var item = prefab?.GetComponent<Item>();

            if (nameText != null)
                nameText.text = item != null ? item.info.Name : $"Item #{slot.itemId}";

            if (priceText != null)
                priceText.text = $"{slot.price}g";

            if (stockText != null)
                stockText.text = slot.IsInfinite ? "∞" : slot.stock.ToString();

            if (iconImage != null && item?.info.Icon != null)
                iconImage.sprite = item.info.Icon;
        }

        /// <inheritdoc/>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (slot == null || !slot.IsAvailable)
            {
                OnPurchaseFailed.Invoke(slot);
                return;
            }

            var currency = _currency ?? PlayerWallet.Instance;
            if (currency == null || !currency.TrySpend(slot.price))
            {
                OnPurchaseFailed.Invoke(slot);
                return;
            }

            if (_buyerInventory == null || !_buyerInventory.TryAddItem(slot.itemId))
            {
                currency.Add(slot.price);
                OnPurchaseFailed.Invoke(slot);
                return;
            }

            slot.TrySell();
            Refresh();
            OnPurchased.Invoke(slot);
        }

        /// <inheritdoc/>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (highlight != null)
                highlight.color = new Color(1f, 1f, 0.75f, highlight.color.a);
        }

        /// <inheritdoc/>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (highlight != null)
                highlight.color = _defaultHighlightColor;
        }
    }
}
