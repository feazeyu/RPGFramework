using Game.Core.Interfaces;
using System;
using UnityEngine;

namespace Game.Core.Stats
{
    public abstract class StatEffect
    {
        public string Name => _source.Name;
        [SerializeField]
        protected INamed _source;

        public Stat Stat { get => _stat; set => _stat = value; }
        [SerializeField]
        protected Stat _stat;

        protected StatEffect(INamed source, Stat stat)
        {
            _source = source;
            _stat = stat;
        }
    }

    [Serializable]
    public class StatEffectF : StatEffect
    {
        public float Flat { get =>_flat; set => _flat = value; }
        [SerializeField]
        private float _flat;

        public float Multiply { get => _multiply; set => _multiply = value; }
        [SerializeField]
        private float _multiply;

        public Scaling Scaling { get => _scaling; set => _scaling = value; }
        [SerializeField]
        private Scaling _scaling;

        public float FlatScale { get => _flatScale; set => _flatScale = value; }
        [SerializeField]
        private float _flatScale;

        public float MultiplyScale { get => _multiplyScale; set => _multiplyScale = value; }
        [SerializeField]
        private float _multiplyScale;

        public StatEffectF(INamed source, Stat stat, float flat = 0f, float multiply = 0f) : base(source, stat)
        {
            _flat = flat;
            _multiply = multiply;
        }

        public static StatEffectF CreateFlat(float flat, INamed source, Stat stat)
        {
            return new(source, stat, flat, 1f);
        }

        /// <param name="percentage"><c>0f</c> = +0% <br/>
        /// <c>0.2f</c> = +20% <br/>
        /// <c>-0.2f</c> = -20%</param>
        public static StatEffectF CreatePercentage(float percentage, INamed source, Stat stat)
        {
            return new(source, stat, 0f, percentage);
        }
    }
}
