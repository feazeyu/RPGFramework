using Game.Core.Utilities;
using UnityEngine;
namespace Game.Abilities { 
internal class SpellComponent : ScriptableObject
{
    public float speed;
    public float damage;
    public SerializableDictionary<ResourceTypes, float> resourceCosts;
    public float cooldown;
    public float range;
}
}
