using Codice.Client.Common;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

#nullable enable
namespace Game.Inventory
{
    /// <summary>
    /// Provides helper methods for inventory UI and slot management.
    /// </summary>
    public static class InventoryHelper
    {
        private static string[]? slotNames;

        /// <summary>
        /// Gets the names of all non-abstract <see cref="InventorySlot"/> types that are not hidden in selections.
        /// </summary>
        /// <returns>An array of slot type names.</returns>
        public static string[] GetSlotTypeNames()
        {
            return slotNames ??= Assembly.GetAssembly(typeof(InventorySlot))
                .GetTypes()
                .Where(t => ((t.IsSubclassOf(typeof(InventorySlot)) || t.IsAssignableFrom(typeof(InventorySlot))) && !t.IsAbstract) && !t.GetInterfaces().Contains(typeof(IHideInSelections)))
                .Select(t => t.Name)
                .ToArray();
        }

        /// <summary>
        /// Generates a drag layer on the specified canvas for handling item drag and drop interactions.
        /// </summary>
        /// <param name="target">The target canvas to add the drag layer to.</param>
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

        /// <summary>
        /// Creates a UI drag handler as a child of the specified parent GameObject.
        /// </summary>
        /// <param name="parent">The parent GameObject to attach the handler to.</param>
        /// <param name="redirector">If true, adds a redirecting handler; otherwise, adds a standard handler.</param>
        /// <returns>The created drag handler GameObject.</returns>
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
    /// <summary>
    /// Represents a UI item container that can redraw its contents.
    /// </summary>
    public interface IUIItemContainer : IItemContainer
    {
        /// <summary>
        /// Redraws the contents of the container.
        /// </summary>
        void RedrawContents();
    }
    /// <summary>
    /// Represents a generic item container.
    /// </summary>
    public interface IItemContainer
    {
        /// <summary>
        /// Removes an item from the container.
        /// </summary>
        /// <returns>The number of items removed.</returns>
        int RemoveItem();

        /// <summary>
        /// Puts an item into the container.
        /// </summary>
        /// <param name="item">The item GameObject to put.</param>
        /// <returns>True if the item was successfully put; otherwise, false.</returns>
        bool PutItem(GameObject item);

        /// <summary>
        /// Returns an item to the container.
        /// </summary>
        /// <param name="item">The item GameObject to return.</param>
        void ReturnItem(GameObject item)
        {
            PutItem(item);
        }
    }
    /// <summary>
    /// Represents a container that holds a single item.
    /// </summary>
    public interface ISingleItemContainer : IItemContainer
    {
        /// <summary>
        /// Gets the item in the container, or null if empty.
        /// </summary>
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

    /// <summary>
    /// Represents a UI positional item container that can redraw its contents.
    /// </summary>
    public interface IUIPositionalItemContainer : IPositionalItemContainer
    {
        /// <summary>
        /// Redraws the contents of the container.
        /// </summary>
        void RedrawContents();
    }
}
