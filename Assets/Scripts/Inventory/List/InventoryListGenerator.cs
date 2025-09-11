using System.Collections.Generic;
using UnityEngine;
using Game.Utils;
using UnityEngine.EventSystems;
using UnityEditor;
namespace Game.Inventory
{
    public class InventoryListGenerator : MonoBehaviour
    {
        public InventoryList list;
        private InventoryListUI target;
        [Tooltip("Background behind the item name.")]
        public GameObject slotPrefab;
        public Canvas targetCanvas;
        [SerializeField, HideInInspector]
        private GameObject UIObject;
        [Tooltip("If unset, will generate an empty object.")]
        public GameObject inventoryContainerOverride;
        [Tooltip("Set first inventory element's position relative to the resulting inventory object")]
        public Vector2 firstElementPosition = new Vector2(0, 0);
        public Vector2 margin = new Vector2(0, 0);
        private void GenerateInventoryObject()
        {
            if (inventoryContainerOverride == null)
            {
                UIObject = new GameObject("ListInventoryUI");
                UIObject.transform.parent = targetCanvas.transform;
                target = UIObject.AddComponent<InventoryListUI>();
                target.gameObject.AddComponent<RectTransform>();
                target.firstElementPosition = firstElementPosition;
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
            if (target!=null)
            {
                target.list = list;
                target.slotPrefab = slotPrefab;
                target.margin = margin;
            }
        }
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
        public void SetInventoryActiveState(bool active)
        {
            if (UIObject)
                UIObject.SetActive(active);
        }
        public void ToggleInventoryActiveState()
        {
            if (UIObject)
                SetInventoryActiveState(!UIObject.activeSelf);
        }

    }
}
