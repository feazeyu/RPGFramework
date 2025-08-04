using System.Collections.Generic;
using UnityEngine;
using Game.Utils;
using UnityEngine.EventSystems;
using UnityEditor;
namespace Game.Inventory
{
    public class InventoryListGenerator : MonoBehaviour
    {
        public int capacity = 20;
        [SerializeField]
        private List<StackableInventorySlot> contents;
        [Tooltip("Background behind the item name.")]
        public GameObject slotPrefab;
        public Canvas targetCanvas;
        [SerializeField, HideInInspector]
        private GameObject inventoryObject;
        [Tooltip("If unset, will generate an empty object.")]
        public GameObject inventoryContainerOverride;
        [Tooltip("Set first inventory element's position relative to the resulting inventory object")]
        public Vector2 firstElementPosition = new Vector2(0, 0);
        public Vector2 margin = new Vector2(0, 0);
        private InventoryList target;
        private void GenerateInventoryObject()
        {
            if (inventoryContainerOverride == null)
            {
                inventoryObject = new GameObject("ListInventory");
                inventoryObject.transform.parent = targetCanvas.transform;
                target = inventoryObject.AddComponent<InventoryList>();
                target.gameObject.AddComponent<RectTransform>();
                target.margin = margin;
                target.firstElementPosition = firstElementPosition;
                Utils.EventRedirector.AddEventRedirector(inventoryObject, inventoryObject);
            }
            else
            {
#if UNITY_EDITOR
                inventoryObject = (GameObject)PrefabUtility.InstantiatePrefab(inventoryContainerOverride, targetCanvas.transform);
#else
                inventoryObject = Instantiate(inventoryContainerOverride, targetCanvas.transform);
#endif
                target = inventoryObject.GetComponent<InventoryList>();
            }
            if (target!=null)
            {
                target.capacity = capacity;
                target.contents = contents;
                target.slotPrefab = slotPrefab;
            }
        }
        public void DrawContents()
        {
            DestroyImmediate(inventoryObject);
            GenerateInventoryObject();

            if (target == null)
            {
                target = inventoryObject.GetComponent<InventoryList>();
            }
            target.CreateOriginPoint();
            target.GenerateUI();
        }

    }
}
