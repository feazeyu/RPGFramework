using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Inventory
{
    internal class GridUISlot : PositionalUISlot
    {
        public Vector2Int anchorSlotPosition = new Vector2Int(-1, -1);
        public override GameObject Item
        {
            get
            {
                if (target == null)
                {
                    Debug.LogError("GridUISlot: Target is not set.");
                    return null;
                }
                if(anchorSlotPosition == new Vector2Int(-1,-1))
                {
                    return target.GetItem(position);
                }
                return target.GetItem(anchorSlotPosition);
            }
        }
    }
}
