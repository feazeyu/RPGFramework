using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Character
{
    public abstract class WeaponCollisionHandler : MonoBehaviour
    {
        public delegate void WeaponHit(GameObject target);
        public event WeaponHit OnHit;
        HashSet<GameObject> alreadyHit = new HashSet<GameObject>();


        public virtual void HandleCollision(GameObject target) {
            if (!alreadyHit.Add(target)) return;
            HandleOnHits(target);
        }
        private void HandleOnHits(GameObject target) { 
            OnHit?.Invoke(target);
        }
        public void ClearAlreadyHit() {
            alreadyHit.Clear();
        }

    }
}
