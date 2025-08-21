using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Game.Inventory
{
    [Serializable]
    public class InventoryList : MonoBehaviour
    {
        public bool EnableSlotCapacity = false;
        public int capacity = 20;
        public int scrollSensitivity = 10;
        public List<StackableInventorySlot> contents;
        private bool suppressAutoAddUI = false;
        private void OnValidate()
        {
#if UNITY_EDITOR
            //Ensure the generator UI is only added once
            if (!suppressAutoAddUI && GetComponent<InventoryListGenerator>() == null)
            {
                EditorApplication.delayCall += () => {
                    if (gameObject != null && gameObject.GetComponent<InventoryListGenerator>() == null && !Application.isPlaying)
                    {
                        suppressAutoAddUI = true;
                        var gen = Undo.AddComponent<InventoryListGenerator>(gameObject);
                        gen.list = this;
                    }
                };
            }
#endif
        }
    }
}
