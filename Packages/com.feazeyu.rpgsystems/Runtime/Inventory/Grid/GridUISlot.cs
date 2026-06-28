using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>
    /// UI slot for a grid cell. Resolves its item through <see cref="anchorSlotPosition"/>
    /// when the cell is a non-anchor part of a multi-cell item.
    /// </summary>
    internal class GridUISlot : PositionalUISlot
    {
        /// <summary>Anchor cell for multi-cell items; (-1,-1) when this is the anchor itself.</summary>
        public Vector2Int anchorSlotPosition = new Vector2Int(-1, -1);
        /// <inheritdoc/>
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
