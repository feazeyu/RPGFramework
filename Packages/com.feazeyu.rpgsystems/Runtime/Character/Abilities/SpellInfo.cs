using Feazeyu.RPGSystems.Core.Utilities;
using UnityEngine;
namespace Feazeyu.RPGSystems.Character
{
    [CreateAssetMenu(fileName ="NewSpellInfo", menuName = "RPGFramework/Abilities/SpellInfo")]
    public class SpellInfo : ScriptableObject
    {
        [Header("Projectile Settings")]
        public GameObject prefab;
        public float speed = 10f;
        public float range = 20f;
        public float damage = 5f;
        //Cooldown in milliseconds
        public float cooldown = 100;
        public SerializableDictionary<ResourceTypes, float> resourceCosts;
    }
}
