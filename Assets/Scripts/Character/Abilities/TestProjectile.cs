using System.Runtime.CompilerServices;
using UnityEngine;
 using Game.Character;

namespace Game.Character
{
    internal class TestProjectile : Projectile
    {
        public void Awake()
        {
            OnDestroyed += () => Debug.Log("Projectile Gon");
            OnHit += (target) => Damage(target);
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            OnDestroyed -= () => Debug.Log("Projectile Gon");
            OnHit -= (target) => Debug.Log($"Hit {target.name}");
        }
        public void Damage(GameObject target) { 
            Entity targetEntity = target.GetComponent<Entity>();
            if (targetEntity != null) {
                targetEntity.resources[ResourceTypes.Health].Points -= SpellInfo.damage;
            }
        }
    }
}
