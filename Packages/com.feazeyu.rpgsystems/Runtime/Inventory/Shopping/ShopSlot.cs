using System;
using UnityEngine;

namespace Feazeyu.RPGSystems.Inventory
{
    [Serializable]
    public class ShopSlot
    {
        public int itemId;
        public int price;
        [Tooltip("-1 = infinite stock")]
        public int stock = -1;

        public bool IsInfinite => stock < 0;
        public bool IsAvailable => IsInfinite || stock > 0;

        public bool TrySell()
        {
            if (!IsAvailable) return false;
            if (!IsInfinite) stock--;
            return true;
        }

        public void UndoSell()
        {
            if (!IsInfinite) stock++;
        }
    }
}
