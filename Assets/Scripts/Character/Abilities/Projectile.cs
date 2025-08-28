using Game.Character;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Game.Character
{
    public abstract class Projectile : WeaponCollisionHandler
    {
        public delegate void ProjectileExpired();
        public event ProjectileExpired OnExpired;
        public delegate void ProjectileDestroyed();
        public event ProjectileDestroyed OnDestroyed;
        public SpellInfo SpellInfo;
        private float travelledDistance = 0f;
        private void Update()
        {
            float distanceThisFrame = SpellInfo.speed * Time.deltaTime;
            travelledDistance += distanceThisFrame;
            transform.Translate(Vector3.right * distanceThisFrame);

            if (travelledDistance >= SpellInfo.range)
            {
                OnExpired?.Invoke();
                Destroy(gameObject);
            }
        }
        public virtual void OnCollisionEnter2D(Collision2D collision)
        {
            HandleCollision(collision.gameObject);
            Destroy(gameObject);
        }

        public virtual void OnDestroy()
        {
            OnDestroyed?.Invoke();
        }
    }
}
