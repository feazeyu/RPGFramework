using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Feazeyu.RPGSystems.Items
{
    /// <summary>
    /// View component for an item tooltip: holds the UI references populated by
    /// <see cref="Item.DisplayTooltip"/>.
    /// </summary>
    public class ItemDescription : MonoBehaviour
    {
        /// <summary>Image showing the item's shape/icon.</summary>
        public Image iconImage;
        /// <summary>Text element for the item's name.</summary>
        public TextMeshProUGUI nameText;
        /// <summary>Text element for the item's tier/rarity.</summary>
        public TextMeshProUGUI rarityText;
        /// <summary>Text element for the item's description.</summary>
        public TextMeshProUGUI descriptionText;
    }
}
