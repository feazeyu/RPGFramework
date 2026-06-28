using Feazeyu.RPGSystems.Core.Interfaces;
using System;
using UnityEngine;

namespace Feazeyu.RPGSystems.Core.Stats
{
    /// <summary>
    /// Base class for a named modifier applied to a single <see cref="Stats.Stat"/>.
    /// </summary>
    public abstract class StatEffect
    {
        /// <summary>Name of the effect, taken from its <see cref="INamed"/> source.</summary>
        public string Name => _source.Name;
        /// <summary>Source.</summary>
        [SerializeField]
        protected INamed _source;

        /// <summary>The stat this effect modifies.</summary>
        public Stat Stat { get => _stat; set => _stat = value; }
        /// <summary>Stat.</summary>
        [SerializeField]
        protected Stat _stat;

        /// <summary>Initializes the effect with its source and target stat.</summary>
        /// <param name="source">The named originator of the effect (item, buff, …).</param>
        /// <param name="stat">The stat this effect modifies.</param>
        protected StatEffect(INamed source, Stat stat)
        {
            _source = source;
            _stat = stat;
        }
    }

    /// <summary>
    /// A floating-point <see cref="StatEffect"/> contributing a flat addend and/or a
    /// multiplicative factor, optionally scaled.
    /// </summary>
    [Serializable]
    public class StatEffectF : StatEffect
    {
        /// <summary>Flat amount added to the stat's base before multiplication.</summary>
        public float Flat { get => _flat; set => _flat = value; }
        [SerializeField]
        private float _flat;

        /// <summary>Multiplicative contribution (0 = no change, 0.2 = +20%).</summary>
        public float Multiply { get => _multiply; set => _multiply = value; }
        [SerializeField]
        private float _multiply;

        /// <summary>Scaling curve applied to this effect.</summary>
        public Scaling Scaling { get => _scaling; set => _scaling = value; }
        [SerializeField]
        private Scaling _scaling;

        /// <summary>Scale factor applied to <see cref="Flat"/>.</summary>
        public float FlatScale { get => _flatScale; set => _flatScale = value; }
        [SerializeField]
        private float _flatScale;

        /// <summary>Scale factor applied to <see cref="Multiply"/>.</summary>
        public float MultiplyScale { get => _multiplyScale; set => _multiplyScale = value; }
        [SerializeField]
        private float _multiplyScale;

        /// <summary>Creates a float effect with explicit flat and multiplicative parts.</summary>
        public StatEffectF(INamed source, Stat stat, float flat = 0f, float multiply = 0f) : base(source, stat)
        {
            _flat = flat;
            _multiply = multiply;
        }

        /// <summary>Creates a purely additive (flat) effect.</summary>
        public static StatEffectF CreateFlat(float flat, INamed source, Stat stat)
        {
            return new(source, stat, flat, 1f);
        }

        /// <summary>Creates a purely multiplicative (percentage) effect.</summary>
        /// <param name="percentage"><c>0f</c> = +0%, <c>0.2f</c> = +20%, <c>-0.2f</c> = -20%.</param>
        public static StatEffectF CreatePercentage(float percentage, INamed source, Stat stat)
        {
            return new(source, stat, 0f, percentage);
        }
    }
}
