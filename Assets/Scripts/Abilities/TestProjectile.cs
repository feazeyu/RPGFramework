using System.Runtime.CompilerServices;
using UnityEngine;


namespace Game.Abilities
{
    internal class TestProjectile : Projectile
    {
        public void Awake()
        {
            OnDestroyed += () => Debug.Log("Projectile Gon");
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            OnDestroyed -= () => Debug.Log("Projectile Gon");
        }
    }
}
