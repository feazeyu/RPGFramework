namespace Feazeyu.RPGSystems.Core.Interfaces
{
    /// <summary>Contract for objects that expose a human-readable name.</summary>
    public interface INamed
    {
        /// <summary>The object's display name.</summary>
        public string Name { get; }
    }
}
