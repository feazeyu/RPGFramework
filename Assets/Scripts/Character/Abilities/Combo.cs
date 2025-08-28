using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Character
{
    [Serializable, CreateAssetMenu(fileName = "NewCombo", menuName = "RPGFramework/Abilities/Combo")]
    public class Combo : ScriptableObject
    {
        public List<ComboStep> steps;
    }

}
