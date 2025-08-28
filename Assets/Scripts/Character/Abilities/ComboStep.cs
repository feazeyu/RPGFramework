using Game.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Character
{
    [Serializable, CreateAssetMenu(fileName = "NewComboStep", menuName = "RPGFramework/Abilities/ComboStep")]
    public class ComboStep : ScriptableObject
    {
        public Motion motion;
        public float comboCancelTime = 0.5f;
    }
}
