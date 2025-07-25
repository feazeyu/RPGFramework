using Codice.Client.Common;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

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
                .Where(t => ((t.IsSubclassOf(typeof(InventorySlot)) || t.IsAssignableFrom(typeof(InventorySlot))) && !t.IsAbstract) && !t.GetInterfaces().Contains(typeof(HideInSelections)))
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

        public static GameObject CreateUIDragHandler(GameObject parent) {
            GameObject handler = new GameObject("UIDragHandler");
            handler.transform.parent = parent.transform;
            handler.AddComponent<InventoryItemUIHandler>();
            var rect = handler.AddComponent<RectTransform>();
            var img = handler.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0); // Transparent background
            handler.AddComponent<CanvasGroup>();
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
    public interface IItemContainer {
        int RemoveItem(GameObject? item);
        bool PutItem(GameObject item);

        void ReturnItem(GameObject item)
        {
            PutItem(item);
        }
    }

}
