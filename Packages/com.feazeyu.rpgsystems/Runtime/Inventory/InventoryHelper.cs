using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

#nullable enable
namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>
    /// Provides helper methods for inventory UI and slot management.
    /// </summary>
    public static class InventoryHelper
    {
        private static string[]? slotNames;
        private static Type[]? slotTypes;

        /// <summary>
        /// Gets all non-abstract <see cref="InventorySlot"/> types (including <see cref="InventorySlot"/>
        /// itself) from every loaded assembly, excluding any marked <see cref="IHideInSelections"/>.
        /// This allows slot types defined in external assemblies (e.g. a game's own asmdef) to be discovered.
        /// </summary>
        /// <returns>The discovered slot types.</returns>
        public static Type[] GetSlotTypes()
        {
            return slotTypes ??= AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .Where(t => typeof(InventorySlot).IsAssignableFrom(t)
                            && !t.IsAbstract
                            && !typeof(IHideInSelections).IsAssignableFrom(t))
                .ToArray();
        }

        /// <summary>
        /// Gets the names of all non-abstract <see cref="InventorySlot"/> types that are not hidden in selections.
        /// </summary>
        /// <returns>An array of slot type names.</returns>
        public static string[] GetSlotTypeNames()
        {
            return slotNames ??= GetSlotTypes().Select(t => t.Name).ToArray();
        }

        /// <summary>
        /// Resolves a slot type by its simple type name across all loaded assemblies.
        /// Use this instead of <see cref="Type.GetType(string)"/> so slot types in any
        /// namespace or assembly resolve correctly. If two discovered slot types share a
        /// simple name, the first one found wins.
        /// </summary>
        /// <param name="name">The simple (non-namespaced) type name.</param>
        /// <returns>The matching slot <see cref="Type"/>, or null if none is found.</returns>
        public static Type? ResolveSlotType(string name)
        {
            return Array.Find(GetSlotTypes(), t => t.Name == name);
        }

        /// <summary>
        /// Returns the loadable types from an assembly, tolerating assemblies that fail to
        /// fully load their type list (returns the types that did load).
        /// </summary>
        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.OfType<Type>();
            }
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

#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(dragLayer, "Create DragLayer");
#endif

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
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(handler, "Create UIDragHandler");
#endif
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
        /// Removes <paramref name="count"/> items with <paramref name="itemId"/> from the container.
        /// Returns false if not enough items are present (no-op in that case).
        /// </summary>
        bool RemoveItem(int itemId, int count = 1) => false;

        /// <summary>
        /// Returns the total number of items with <paramref name="itemId"/> in the container.
        /// </summary>
        int CountItem(int itemId) => 0;

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

        /// <summary>
        /// Looks up the item prefab by ID and tries to add <paramref name="count"/> copies.
        /// Returns false if the item is not registered or there is no space.
        /// Implementors that support this should override the default.
        /// </summary>
        bool TryAddItem(int itemId, int count = 1) => false;
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

        /// <summary>
        /// Removes the item from this single-item slot.
        /// </summary>
        /// <returns>The item ID that was removed, or -1 if empty.</returns>
        int RemoveItem();
    }
    public interface IPositionalItemContainer : IItemContainer
    {
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
