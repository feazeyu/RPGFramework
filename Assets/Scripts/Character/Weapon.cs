using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Character
{
    public abstract class Weapon : MonoBehaviour
    {
        public abstract void Attack();
    }
}
