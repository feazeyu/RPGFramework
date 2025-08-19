using Game.Core.Utilities;
using UnityEngine;
namespace Game.Character
{
    public class Spell : ScriptableObject
    {
        [Header("Projectile Settings")]
        public GameObject prefab;
        public Transform spawnPoint;
        public float speed = 10f;
        public float range = 20f;
        public float damage = 5f;
        public float cooldown = 1f;
        public SerializableDictionary<ResourceTypes, float> resourceCosts;

    }
}
