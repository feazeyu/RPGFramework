using System;
using UnityEngine;

namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>One shop listing: an item, its price, and remaining stock (negative = infinite).</summary>
    [Serializable]
    public class ShopSlot
    {
        /// <summary>Id of the item sold by this listing.</summary>
        public int itemId;
        /// <summary>Unit price.</summary>
        public int price;
        /// <summary>Remaining stock; -1 means infinite.</summary>
        [Tooltip("-1 = infinite stock")]
        public int stock = -1;

        /// <summary>Whether the listing has infinite stock.</summary>
        public bool IsInfinite => stock < 0;
        /// <summary>Whether at least one unit can be sold.</summary>
        public bool IsAvailable => IsInfinite || stock > 0;

        /// <summary>Consumes one unit of stock; returns false if none available.</summary>
        public bool TrySell()
        {
            if (!IsAvailable) return false;
            if (!IsInfinite) stock--;
            return true;
        }

        /// <summary>Returns one unit to stock, reversing a <see cref="TrySell"/>.</summary>
        public void UndoSell()
        {
            if (!IsInfinite) stock++;
        }
    }
}
