using Game.Character;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Game.Abilities
{
    public class Projectile : MonoBehaviour
    {
        public delegate void ProjectileHit(GameObject target);
        public event ProjectileHit OnHit;
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

            OnHit?.Invoke(collision.gameObject);
            Destroy(gameObject);

        }

        public virtual void OnDestroy()
        {
            OnDestroyed?.Invoke();
        }
    }
}
