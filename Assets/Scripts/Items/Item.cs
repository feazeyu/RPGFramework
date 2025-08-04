using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Items
{
    public class Item : MonoBehaviour
    {
        public ItemInfo info;

        public void Start()
        {
            CalculatePivot();
        }

        public void CalculatePivot() {
            var centerF = info.Shape.GetCenter();
            Vector2 dimensions = centerF * 2 + new Vector2(1, 1);
            var center = GetAnchorSlot() + new Vector2(0.5f, 0.5f);
            Vector2 newPivot = new(center.x / dimensions.x, 1-(center.y / dimensions.y));
            gameObject.GetComponent<RectTransform>().pivot = newPivot;
        }

        public Vector2Int GetAnchorSlot() {
            var centerF = info.Shape.GetCenter();
            Vector2 centerProspect = centerF;
            float minDistance = float.MaxValue;
            foreach (var position in info.Shape.Positions) { 
                if (Vector2.Distance(centerF, position) < minDistance) {
                    centerProspect = position;
                    minDistance = Vector2.Distance(centerF, position);
                }
            }
            return new Vector2Int((int)centerProspect.x, (int)centerProspect.y);
        }
    }
}
