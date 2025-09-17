using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Game.Inventory
{
    /// <summary>
    /// Represents an inventory list that manages inventory slots and interacts with the UI generator.
    /// </summary>
    [Serializable]
    public class InventoryList : MonoBehaviour
    {
        /// <summary>
        /// Indicates whether slot capacity is enabled.
        /// </summary>
        public bool EnableSlotCapacity = false;

        /// <summary>
        /// The maximum number of slots in the inventory.
        /// </summary>
        public int capacity = 20;

        /// <summary>
        /// The scroll sensitivity for the inventory UI.
        /// </summary>
        public int scrollSensitivity = 10;

        /// <summary>
        /// The list of stackable inventory slots.
        /// </summary>
        public List<StackableInventorySlot> contents;

        /// <summary>
        /// Suppresses automatic addition of the UI generator component.
        /// </summary>
        private bool suppressAutoAddUI = false;

        /// <summary>
        /// The UI generator responsible for creating and managing the inventory UI.
        /// </summary>
        [SerializeField]
        private InventoryListGenerator _uiGenerator;

        /// <summary>
        /// Gets the UI generator for this inventory list, adding it if necessary.
        /// </summary>
        public InventoryListGenerator uiGenerator
        {
            get
            {
                if (_uiGenerator == null)
                {
                    _uiGenerator = GetComponent<InventoryListGenerator>();
                }
                return _uiGenerator;
            }
        }

        /// <summary>
        /// Unity callback invoked when the script is loaded or a value changes in the Inspector.
        /// Ensures the UI generator is added only once in the editor.
        /// </summary>
        private void OnValidate()
        {
#if UNITY_EDITOR
            // Ensure the generator UI is only added once
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

        /// <summary>
        /// Toggles the active state of the inventory UI.
        /// </summary>
        public void ToggleInventory()
        {
            if (uiGenerator)
                uiGenerator.ToggleInventoryActiveState();
        }

        /// <summary>
        /// Opens the inventory UI.
        /// </summary>
        public void OpenInventory()
        {
            if (uiGenerator)
                uiGenerator.SetInventoryActiveState(true);
        }

        /// <summary>
        /// Closes the inventory UI.
        /// </summary>
        public void CloseInventory()
        {
            if (uiGenerator)
                uiGenerator.SetInventoryActiveState(false);
        }
    }
}
