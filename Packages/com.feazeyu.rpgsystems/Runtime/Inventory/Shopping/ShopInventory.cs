using System.Collections.Generic;
using UnityEngine;

namespace Feazeyu.RPGSystems.Inventory
{
    [CreateAssetMenu(fileName = "NewShopInventory", menuName = "RPGFramework/Shop/ShopInventory")]
    public class ShopInventory : ScriptableObject
    {
        public List<ShopSlot> listings = new();
    }
}
