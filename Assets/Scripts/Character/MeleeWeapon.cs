using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Character
{
    public class MeleeWeapon : MonoBehaviour
    {
        [SerializeField]
        private bool _isAttacking = false;
        public bool IsAttacking {
            get { return _isAttacking; }
            private set { _isAttacking = value; }
        }
        private Animator animator;
        public void StartAttack() {
            IsAttacking = true;
            UpdateAnimatorState();
        }

        public void StopAttack() {
            IsAttacking = false;
            UpdateAnimatorState();
        }

        private void UpdateAnimatorState() {
            if (animator == null)
            {
                animator = transform.parent.GetComponent<Animator>();
            }
            if (animator != null)
            {
                animator.SetBool("isAttacking", IsAttacking);
            }
        }
    }
}
