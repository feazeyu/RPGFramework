using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Items
{
    public class Item : MonoBehaviour
    {
        public ItemInfo info;
        [Tooltip("Set a different prefab to display on hover")]
        public GameObject descriptionOverride;
        private GameObject descriptionObject;
        public void Start()
        {
            CalculatePivot();
        }

        public void CalculatePivot()
        {
            var centerF = info.Shape.GetCenter();
            Vector2 dimensions = centerF * 2 + new Vector2(1, 1);
            var center = GetAnchorSlot() + new Vector2(0.5f, 0.5f);
            Vector2 newPivot = new(center.x / dimensions.x, 1-(center.y / dimensions.y));
            gameObject.GetComponent<RectTransform>().pivot = newPivot;
        }

        public Vector2Int GetAnchorSlot()
        {
            var centerF = info.Shape.GetCenter();
            Vector2 centerProspect = centerF;
            float minDistance = float.MaxValue;
            foreach (var position in info.Shape.Positions)
            {
                if (Vector2.Distance(centerF, position) < minDistance)
                {
                    centerProspect = position;
                    minDistance = Vector2.Distance(centerF, position);
                }
            }
            return new Vector2Int((int)centerProspect.x, (int)centerProspect.y);
        }

        public GameObject DisplayTooltip(Transform parent)
        {
            if (descriptionOverride == null)
            {
                descriptionOverride = Resources.Load<GameObject>("ItemAdjacent/DefaultItemDescription");
            }
            if (descriptionObject == null)
            {
                descriptionObject = Instantiate(descriptionOverride, parent);
                var description = descriptionObject.GetComponent<ItemDescription>();
                if (description == null)
                {
                    Debug.LogError("ItemDescription component not found on the description object.");
                    return null;
                }
                description.iconImage.sprite = CreateSpriteFromBoolArray(info.Shape.GetBoolGrid(5));
                description.nameText.text = info.Name;
                description.rarityText.text = info.Tier.ToString();
                description.descriptionText.text = info.Description;
            }
            descriptionObject.SetActive(true);
            return descriptionObject;
        }

        private Sprite CreateSpriteFromBoolArray(bool[,] boolArray)
        {
            int gridSize = 5;
            Color trueColor = Color.white; 
            Color falseColor = Color.black; 
            // Create a new texture
            Texture2D texture = new Texture2D(gridSize, gridSize);

            // Set filter mode to prevent blurring
            texture.filterMode = FilterMode.Point;

            // Set each pixel based on the bool array
            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    texture.SetPixel(x, y, boolArray[x, y] ? trueColor : falseColor);
                }
            }

            // Apply all SetPixel calls
            texture.Apply();

            // Create a sprite from the texture
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, gridSize, gridSize),
                new Vector2(0.5f, 0.5f), // pivot point
                gridSize // pixels per unit
            );

            return sprite;
        }

        public void HideTooltip()
        {
            descriptionObject.SetActive(false);
        }

    }
}
