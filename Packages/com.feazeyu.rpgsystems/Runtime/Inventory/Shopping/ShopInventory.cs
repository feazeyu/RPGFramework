using System.Collections.Generic;
using UnityEngine;

namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>Authored asset holding a shop's listings; clone it for runtime use.</summary>
    [CreateAssetMenu(fileName = "NewShopInventory", menuName = "RPGFramework/Shop/ShopInventory")]
    public class ShopInventory : ScriptableObject
    {
        /// <summary>The shop's listings.</summary>
        public List<ShopSlot> listings = new();

        /// <summary>
        /// Builds a throwaway runtime copy of this inventory. Buying and selling mutate
        /// <see cref="ShopSlot.stock"/>, so shops must run against a clone — otherwise those
        /// mutations are written back into the authored asset and persist across play sessions
        /// in the editor. Mirrors <c>Blackboard.CloneForRuntime()</c>: the authored asset is
        /// never mutated at runtime. <see cref="Object.Instantiate(Object)"/> deep-copies the
        /// serialized <see cref="listings"/> list (ShopSlot is a by-value [Serializable] type).
        /// </summary>
        public ShopInventory CloneForRuntime()
        {
            var copy = Instantiate(this);
            copy.name = $"{name} (Runtime)";
            copy.hideFlags = HideFlags.DontSave;
            return copy;
        }
    }
}
