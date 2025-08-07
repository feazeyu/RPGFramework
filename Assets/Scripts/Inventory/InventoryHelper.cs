using Codice.Client.Common;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

#nullable enable
namespace Game.Inventory
{
    public static class InventoryHelper
    {
        private static string[]? slotNames;

        public static string[] GetSlotTypeNames()
        {
            return slotNames ??= Assembly.GetAssembly(typeof(InventorySlot))
                .GetTypes()
                .Where(t => ((t.IsSubclassOf(typeof(InventorySlot)) || t.IsAssignableFrom(typeof(InventorySlot))) && !t.IsAbstract) && !t.GetInterfaces().Contains(typeof(IHideInSelections)))
                .Select(t => t.Name)
                .ToArray();
        }
        public static void GenerateDragLayer(Canvas target)
        {
            if (target == null)
            {
                Debug.LogError("InventoryUIGenerator: Target Canvas is not set.");
                return;
            }
            Transform existing = target.transform.Find("DragLayer");
            if (existing != null)
            {
                existing.transform.SetAsLastSibling();
                return;
            }

            GameObject dragLayer = new("DragLayer", typeof(RectTransform), typeof(CanvasRenderer));
            RectTransform rectTransform = dragLayer.GetComponent<RectTransform>();
            dragLayer.transform.SetParent(target.transform, false);
            dragLayer.layer = LayerMask.NameToLayer("UI");
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            dragLayer.transform.SetAsLastSibling();

            Undo.RegisterCreatedObjectUndo(dragLayer, "Create DragLayer");

            Debug.Log("DragLayer created. It is used for handling item drag and drop interactions, do not delete if u don't know what you're doing.");
        }

        public static GameObject CreateUIDragHandler(GameObject parent, bool redirector = false)
        {
            GameObject handler = new GameObject("UIDragHandler");
            handler.transform.parent = parent.transform;
            if (redirector)
            {
                handler.AddComponent<InventoryItemUIRedirectingHandler>();
            }
            else
            {
                handler.AddComponent<InventoryItemUIHandler>();
            }
            var rect = handler.AddComponent<RectTransform>();
            var img = handler.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0); // Transparent background
            CanvasGroup group = handler.AddComponent<CanvasGroup>();
            group.ignoreParentGroups = true;
            handler.layer = LayerMask.NameToLayer("UI");
            Undo.RegisterCreatedObjectUndo(handler, "Create UIDragHandler");
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = parent.GetComponent<RectTransform>().sizeDelta;
            handler.transform.SetAsLastSibling();
            return handler;
        }
    }
    public interface IUIItemContainer : IItemContainer
    {
        void RedrawContents();

    }
    public interface IItemContainer
    {
        int RemoveItem();
        bool PutItem(GameObject item);

        void ReturnItem(GameObject item)
        {
            PutItem(item);
        }
    }
    public interface ISingleItemContainer : IItemContainer
    {
        GameObject? Item { get; }
    }
    public interface IPositionalItemContainer : IItemContainer
    {
        int IItemContainer.RemoveItem()
        {
            return RemoveItem(Vector2Int.zero);
        }
        int RemoveItem(Vector2Int position);

        bool IItemContainer.PutItem(GameObject item)
        {
            return PutItem(new Vector2Int(-1, -1), item);
        }
        bool PutItem(Vector2Int position, GameObject item);

        void IItemContainer.ReturnItem(GameObject item)
        {
            PutItem(item);
        }
        void ReturnItem(Vector2Int position, GameObject item)
        {
            PutItem(position, item);
        }
        GameObject? GetItem(Vector2Int position);
    }

    public interface IUIPositionalItemContainer : IPositionalItemContainer
    {
        void RedrawContents();
    }
}
