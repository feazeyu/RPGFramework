using System;
using UnityEngine;

namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>Singleton <see cref="IShopCurrency"/> holding the player's currency balance.</summary>
    public class PlayerWallet : MonoBehaviour, IShopCurrency
    {
        private static PlayerWallet _instance;

        /// <summary>Lazily-resolved singleton instance; auto-created if none exists.</summary>
        public static PlayerWallet Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = FindFirstObjectByType<PlayerWallet>();
                if (_instance == null)
                    _instance = new GameObject("PlayerWallet").AddComponent<PlayerWallet>();
                return _instance;
            }
        }

        [SerializeField] private int _balance = 100;

        /// <summary>Raised with the new balance whenever it changes.</summary>
        public event Action<int> OnBalanceChanged;

        /// <inheritdoc/>
        public int Balance => _balance;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(this); return; }
            _instance = this;
        }

        /// <inheritdoc/>
        public bool TrySpend(int amount)
        {
            if (_balance < amount) return false;
            _balance -= amount;
            OnBalanceChanged?.Invoke(_balance);
            return true;
        }

        /// <inheritdoc/>
        public void Add(int amount)
        {
            _balance += amount;
            OnBalanceChanged?.Invoke(_balance);
        }
    }
}
