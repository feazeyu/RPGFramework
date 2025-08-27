using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Abilities
{
    [Serializable]
    public class Combo : ScriptableObject
    {
        public List<ComboStep> steps;
    }

}
