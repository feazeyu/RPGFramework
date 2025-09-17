using System.Collections.Generic;
using UnityEngine;
using Game.Utils;
using UnityEngine.EventSystems;
using UnityEditor;
namespace Game.Inventory
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

                Utils.EventRedirector.AddEventRedirector(UIObject, UIObject);
            }
            else
            {
#if UNITY_EDITOR
                UIObject = (GameObject)PrefabUtility.InstantiatePrefab(inventoryContainerOverride, targetCanvas.transform);
#else
                    inventoryObject = Instantiate(inventoryContainerOverride, targetCanvas.transform);
#endif
                target = UIObject.GetComponent<InventoryListUI>();
            }
            if (target != null)
            {
                target.list = list;
                target.slotPrefab = slotPrefab;
                target.margin = margin;
                target.firstElementPosition = firstElementPosition;
            }
        }

        /// <summary>
        /// Destroys the current UI object and redraws the inventory contents.
        /// </summary>
        public void DrawContents()
        {
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
