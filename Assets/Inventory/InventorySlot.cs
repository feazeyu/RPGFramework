
using System;
using UnityEngine;

namespace Game.Inventory
{
    public interface HideInSelections { };
    [Serializable]
    public class InventorySlot
    {
        [SerializeReference, HideInInspector]
        internal UISlot uiSlot;
        [SerializeField, HideInInspector]
        public bool IsEnabled = true;
        protected virtual Color BaseColor => Color.white;
        protected string defaultUILabel = "I";
        [HideInInspector]
        public string editorUILabel = "";
        [SerializeField]
        private GameObject _item;
        public GameObject Item
        {
            get => _item;
            protected set => _item = value;
        }

        // These methods are here only for use in the editor, to be able to force items into slots they don't belong.
        // And slots like the locked ones, which cannot be edited by the player.
#if UNITY_EDITOR
        public virtual bool EditorOnlyPutItem(GameObject item)
        {
            if (item == null) return false;
            editorUILabel = defaultUILabel;
            Item = item;
            return true;
        }

        public virtual bool EditorOnlyRemoveItem()
        {
            editorUILabel = "";
            Item = null;
            return true;
        }
#endif
        public virtual bool PutItem(GameObject item)
        {
            if (Item != null || !IsEnabled) return false;
            editorUILabel = defaultUILabel;
            Item = item;
            return true;
        }

        public virtual bool RemoveItem(GameObject item = null)
        {
            if (Item == null) return false;
            editorUILabel = "";
            Item = null;
            return true;
        }

        public Color Color => IsEnabled ? BaseColor : BaseColor * 0.5f;

    }

    [Serializable]
    public class LockedInventorySlot : InventorySlot
    {
        protected override Color BaseColor => Color.red;
        public override bool PutItem(GameObject item) => false;
        public override bool RemoveItem(GameObject item) => false;
    }

    [Serializable]
    public class ListInventorySlot : InventorySlot, HideInSelections
    {
        public ListInventorySlot(GameObject item)
        {
            Item = item;
        }
        [SerializeReference, HideInInspector]
        private InventoryList _target;
        public InventoryList Target
        {
            get
            {
                return _target;
            }
            set
            {
                _target = value;
            }
        }
        public override bool PutItem(GameObject item)
        {
            if (Target == null)
            {
                Debug.LogWarning($"Trying to insert to a non existent {Target}");
                return false;
            }
            return Target.PutItem(item);

        }
        public override bool RemoveItem(GameObject nonoitem)
        {
            if (Target == null)
            {
                Debug.LogWarning($"Trying to remove from a non existent {Target}");
                return false;
            }
            return Target.RemoveItem(Item);

        }

    }
}
