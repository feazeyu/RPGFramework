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

        [Header("UI — auto-wired from named children if left null")]
        public TMP_Text nameText;
        public TMP_Text priceText;
        public TMP_Text stockText;
        public Image iconImage;
        public Graphic highlight;

        public UnityEvent<ShopSlot> OnPurchased;
        public UnityEvent<ShopSlot> OnPurchaseFailed;

        private IShopCurrency _currency;
        private Color _defaultHighlightColor;

        public void Setup(ShopSlot shopSlot, IShopCurrency currency)
        {
            slot = shopSlot;
            _currency = currency;
            if (highlight != null)
                _defaultHighlightColor = highlight.color;
            Refresh();
        }

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

            if (PlayerInventoryService.Instance == null || !PlayerInventoryService.Instance.TryAddItem(slot.itemId))
            {
                currency.Add(slot.price); // refund — inventory was full
                OnPurchaseFailed.Invoke(slot);
                return;
            }

            slot.TrySell();
            Refresh();
            OnPurchased.Invoke(slot);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (highlight != null)
                highlight.color = new Color(1f, 1f, 0.75f, highlight.color.a);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (highlight != null)
                highlight.color = _defaultHighlightColor;
        }
    }
}
