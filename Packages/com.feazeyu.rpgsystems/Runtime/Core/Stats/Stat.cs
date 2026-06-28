namespace Feazeyu.RPGSystems.Core.Stats
{
    /// <summary>Identifies a character attribute that a <see cref="StatEffect"/> can target.</summary>
    public enum Stat
    {
        /// <summary>Maximum health pool.</summary>
        MaxHitPoints,
        /// <summary>Physical damage reduction.</summary>
        Armor,
        /// <summary>Magical damage reduction.</summary>
        MagicResistance,
        /// <summary>Outgoing damage.</summary>
        Damage,
        /// <summary>Attack/ability reach.</summary>
        Range,
        /// <summary>Attacks per unit time.</summary>
        AttackSpeed,
        /// <summary>Locomotion speed.</summary>
        MovementSpeed
    }
}
