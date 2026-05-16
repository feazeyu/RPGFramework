using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>
    /// Generates a scrollable list-based shop UI from a ShopInventory.
    /// Assign a rowPrefab (with ShopSlotHandler) for custom row appearance,
    /// or leave null for the procedural fallback (Name | Stock | Price columns).
    ///
    /// Initialization: call Setup() or assign shopInventory then call GenerateUI().
    /// Driven by Shopkeep when placed as its shopListUI reference.
    /// </summary>
    public class ShopListUI : InventoryList
    {
        [Header("Data")]
        public ShopInventory shopInventory;

        [Header("Layout")]
        [Tooltip("Canvas to parent the list to. Defaults to nearest parent Canvas.")]
        public Canvas targetCanvas;
        public Vector2 rowSize = new(300, 40);
        public float rowSpacing = 4f;
        public Vector2 uiPosition = new(0, 0);

        [Header("Prefab")]
        [Tooltip("Row prefab with ShopSlotHandler. Auto-wires children named Name/Price/Stock/Icon. Leave null for procedural fallback.")]
        public GameObject rowPrefab;

        [Header("Currency")]
        [Tooltip("MonoBehaviour implementing IShopCurrency. Falls back to PlayerWallet singleton if null.")]
        [SerializeField] private MonoBehaviour _currencyProvider;

        private IShopCurrency Currency => (_currencyProvider as IShopCurrency) ?? PlayerWallet.Instance;

        private GameObject _root;

        private void Awake()
        {
            if (shopInventory != null)
                GenerateUI();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            // Do not call base — suppress auto-add of InventoryListGenerator.
        }
#endif

        public override void OpenInventory() => _root?.SetActive(true);
        public override void CloseInventory() => _root?.SetActive(false);
        public override void ToggleInventory() { if (_root != null) _root.SetActive(!_root.activeSelf); }

        public void Setup(ShopInventory inventory)
        {
            shopInventory = inventory;
            GenerateUI();
        }

        public void GenerateUI()
        {
            if (_root != null) Destroy(_root);
            if (shopInventory == null) return;

            var parent = targetCanvas != null
                ? targetCanvas.transform
                : GetComponentInParent<Canvas>()?.transform ?? transform;

            _root = new GameObject("ShopList_Root");
            var rootRect = _root.AddComponent<RectTransform>();
            rootRect.SetParent(parent, false);
            rootRect.anchorMin = new Vector2(0, 1);
            rootRect.anchorMax = new Vector2(0, 1);
            rootRect.pivot = new Vector2(0, 1);
            rootRect.anchoredPosition = new Vector2(uiPosition.x, -uiPosition.y);

            var layout = _root.AddComponent<VerticalLayoutGroup>();
            layout.spacing = rowSpacing;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            var fitter = _root.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (var slot in shopInventory.listings)
                CreateRow(slot);

            _root.SetActive(false);
        }

        private void CreateRow(ShopSlot slot)
        {
            GameObject row = rowPrefab != null
                ? Instantiate(rowPrefab, _root.transform)
                : BuildDefaultRow(_root.transform);

            var handler = row.GetComponent<ShopSlotHandler>() ?? row.AddComponent<ShopSlotHandler>();

            if (handler.nameText == null)
                handler.nameText = row.transform.Find("Name")?.GetComponent<TMP_Text>();
            if (handler.priceText == null)
                handler.priceText = row.transform.Find("Price")?.GetComponent<TMP_Text>();
            if (handler.stockText == null)
                handler.stockText = row.transform.Find("Stock")?.GetComponent<TMP_Text>();
            if (handler.iconImage == null)
                handler.iconImage = row.transform.Find("Icon")?.GetComponent<Image>();
            if (handler.highlight == null)
                handler.highlight = row.GetComponent<Graphic>();

            handler.Setup(slot, Currency);
        }

        private GameObject BuildDefaultRow(Transform parent)
        {
            var row = new GameObject("ShopRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);

            var bg = row.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.18f, 0.92f);

            var le = row.AddComponent<LayoutElement>();
            le.preferredWidth = rowSize.x;
            le.preferredHeight = rowSize.y;

            var hLayout = row.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.spacing = 8;
            hLayout.padding = new RectOffset(8, 8, 4, 4);
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = true;

            AddTextCell(row.transform, "Name", 130, 14, Color.white, TextAlignmentOptions.MidlineLeft);
            AddTextCell(row.transform, "Stock", 40, 12, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Center);
            AddTextCell(row.transform, "Price", 70, 12, Color.yellow, TextAlignmentOptions.MidlineRight);

            return row;
        }

        private static void AddTextCell(Transform parent, string childName, float width, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            var obj = new GameObject(childName, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var cellLe = obj.AddComponent<LayoutElement>();
            cellLe.preferredWidth = width;
            var text = obj.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
        }

        public void SetActive(bool active) => _root?.SetActive(active);
        public void Toggle() { if (_root != null) _root.SetActive(!_root.activeSelf); }
    }
}
