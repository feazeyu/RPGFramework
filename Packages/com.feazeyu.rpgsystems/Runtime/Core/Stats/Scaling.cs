namespace Feazeyu.RPGSystems.Core.Stats
{
    /// <summary>How a <see cref="StatEffect"/>'s contribution scales with its source value.</summary>
    public enum Scaling
    {
        /// <summary>No scaling.</summary>
        None,
        /// <summary>Scales proportionally with the source value.</summary>
        Linear,
        /// <summary>Scales as a product of the source value.</summary>
        Multiplicative,
        /// <summary>Scales with diminishing returns as the source value grows.</summary>
        DiminishingReturns
    }
}
