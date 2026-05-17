using System.Collections.Generic;
using UnityEngine;
using Feazeyu.RPGSystems.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>
    /// Generates and manages the UI for an inventory list, including creation, drawing, and visibility toggling.
    /// </summary>
    public class InventoryListGenerator : MonoBehaviour
    {
        /// <summary>
        /// The inventory list to display.
        /// </summary>
        public InventoryList list;

        /// <summary>
        /// The UI component representing the inventory list.
        /// </summary>
        private InventoryListUI target;

        /// <summary>
        /// Background behind the item name.
        /// </summary>
        [Tooltip("Background behind the item name.")]
        public GameObject slotPrefab;

        /// <summary>
        /// The canvas on which to display the inventory UI.
        /// </summary>
        public Canvas targetCanvas;

        /// <summary>
        /// The root UI object for the inventory.
        /// </summary>
        [SerializeField, HideInInspector]
        private GameObject UIObject;

        /// <summary>
        /// If unset, will generate an empty object.
        /// </summary>
        [Tooltip("If unset, will generate an empty object.")]
        public GameObject inventoryContainerOverride;

        /// <summary>
        /// Sets the first inventory element's position relative to the resulting inventory object.
        /// </summary>
        [Tooltip("Set first inventory element's position relative to the resulting inventory object")]
        public Vector2 firstElementPosition = new Vector2(0, 0);

        /// <summary>
        /// The margin between inventory slots.
        /// </summary>
        public Vector2 margin = new Vector2(0, 0);

        /// <summary>
        /// What name should the inventory object have? Useful for debugging and organization purposes.
        /// </summary>
        public string inventoryName = "Inventory";

        /// <summary>
        /// Anchor point on the canvas to attach the generated UI to.
        /// </summary>
        [HideInInspector]
        public TextAnchor anchorPosition = TextAnchor.UpperLeft;

        /// <summary>
        /// Generates the inventory UI object, either from a prefab or as a new GameObject.
        /// </summary>
        private void GenerateInventoryObject()
        {
            if (inventoryContainerOverride == null)
            {
                UIObject = new GameObject("ListInventoryUI");
                UIObject.transform.parent = targetCanvas.transform;
                target = UIObject.AddComponent<InventoryListUI>();
                target.gameObject.AddComponent<RectTransform>();

                EventRedirector.AddEventRedirector(UIObject, UIObject);
            }
            else
            {
#if UNITY_EDITOR
                UIObject = (GameObject)PrefabUtility.InstantiatePrefab(inventoryContainerOverride, targetCanvas.transform);
                UIObject.name = inventoryName;
#else
                    inventoryObject = Instantiate(inventoryContainerOverride, targetCanvas.transform);
                    inventoryObject.name = inventoryName;
#endif
                target = UIObject.GetComponent<InventoryListUI>();
            }
            var rect = UIObject.GetComponent<RectTransform>();
            if (rect != null)
            {
                var anchorVec = AnchorVector(anchorPosition);
                rect.anchorMin = anchorVec;
                rect.anchorMax = anchorVec;
                rect.pivot = anchorVec;
            }

            if (target != null)
            {
                target.list = list;
                target.slotPrefab = slotPrefab;
                target.margin = margin;
                target.firstElementPosition = firstElementPosition;
            }
        }

        private static Vector2 AnchorVector(TextAnchor anchor)
        {
            int val = (int)anchor;
            return new Vector2((val % 3) * 0.5f, 1f - (val / 3) * 0.5f);
        }

        /// <summary>
        /// Destroys the current UI object and redraws the inventory contents.
        /// </summary>
        public void DrawContents()
        {
            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
                if (targetCanvas == null)
                {
                    Debug.LogError("InventoryListGenerator: Target Canvas is not set and no Canvas was found in the scene.");
                    return;
                }
                Debug.LogWarning($"InventoryListGenerator on '{name}': Target Canvas is not set. Using '{targetCanvas.name}' found in the scene.", this);
            }
            DestroyImmediate(UIObject);
            GenerateInventoryObject();

            if (target == null)
            {
                target = UIObject.GetComponent<InventoryListUI>();
            }
            target.CreateOriginPoint();
            target.GenerateUI();
        }

        /// <summary>
        /// Sets the active state of the inventory UI.
        /// </summary>
        /// <param name="active">If true, activates the UI; otherwise, deactivates it.</param>
        public void SetInventoryActiveState(bool active)
        {
            if (UIObject)
                UIObject.SetActive(active);
        }

        /// <summary>
        /// Toggles the active state of the inventory UI.
        /// </summary>
        public void ToggleInventoryActiveState()
        {
            if (UIObject)
                SetInventoryActiveState(!UIObject.activeSelf);
        }
    }
}
