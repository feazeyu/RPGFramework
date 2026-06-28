using UnityEngine;
using TMPro;
namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>Holds the TMP text fields used to render an inventory slot's item name and stack count.</summary>
    public class TextCountItemRenderer : MonoBehaviour
    {
        /// <summary>Text element showing the stack count.</summary>
        public TMPro.TextMeshProUGUI CountText;
        /// <summary>Text element showing the item name.</summary>
        public TMPro.TextMeshProUGUI ItemText;
    }
}
