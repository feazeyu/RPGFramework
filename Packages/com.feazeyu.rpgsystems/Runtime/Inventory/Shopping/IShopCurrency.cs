namespace Feazeyu.RPGSystems.Inventory
{
    public interface IShopCurrency
    {
        int Balance { get; }
        bool TrySpend(int amount);
        void Add(int amount);
    }
}
