using Game.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Abilities
{
    [Serializable, CreateAssetMenu(fileName = "NewComboStep", menuName = "RPGFramework/Abilities/ComboStep")]
    public class ComboStep : ScriptableObject
    {
        public Animation animation;
        public float animationSpeed = 1f;
        public float comboCancelTime = 0.5f;
        public float AnimationDuration
        {
            get
            {
                if (animation != null)
                {
                    return animation.clip.length / animationSpeed;
                }
                return 0f;
            }
        }
        public virtual void Execute(GameObject target) {
            animation.Play();
        }

    }
}
