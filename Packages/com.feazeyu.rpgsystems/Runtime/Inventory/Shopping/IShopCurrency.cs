namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>Abstraction over a spendable currency balance used by shops.</summary>
    public interface IShopCurrency
    {
        /// <summary>Current balance.</summary>
        int Balance { get; }
        /// <summary>Attempts to spend <paramref name="amount"/>; returns false if insufficient.</summary>
        bool TrySpend(int amount);
        /// <summary>Adds <paramref name="amount"/> to the balance.</summary>
        void Add(int amount);
    }
}
