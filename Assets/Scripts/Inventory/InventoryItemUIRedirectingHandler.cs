using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Inventory
{
    internal class InventoryItemUIRedirectingHandler : InventoryItemUIHandler
    {
        public Vector2Int targetPosition;
        protected override GameObject GetOriginalParent()
        {
            return transform.parent.parent.Find($"Cell_{targetPosition.x}_{targetPosition.y}").gameObject;
        }
    }
}
