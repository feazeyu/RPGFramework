using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>
    /// Drag handler for a non-anchor (satellite) cell of a multi-cell item.
    /// Resolves the real owning slot through <see cref="targetPosition"/> so the
    /// drag originates from, and drops resolve to, the item's anchor cell. Use
    /// this instead of <see cref="InventoryItemUIHandler"/> on satellite cells of
    /// shaped items; <see cref="InventoryHelper.CreateUIDragHandler"/> attaches it
    /// when <c>redirector: true</c>.
    /// </summary>
    public class InventoryItemUIRedirectingHandler : InventoryItemUIHandler
    {
        /// <summary>Target position.</summary>
        public Vector2Int targetPosition;
        /// <inheritdoc/>
        protected override GameObject GetOriginalParent()
        {
            return transform.parent.parent.Find($"Cell_{targetPosition.x}_{targetPosition.y}").gameObject;
        }
    }
}
