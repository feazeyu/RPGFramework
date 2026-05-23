using System;
using UnityEngine;

namespace Feazeyu.RPGSystems.Inventory
{
    public class PlayerWallet : MonoBehaviour, IShopCurrency
    {
        private static PlayerWallet _instance;

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

        public event Action<int> OnBalanceChanged;

        public int Balance => _balance;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(this); return; }
            _instance = this;
        }

        public bool TrySpend(int amount)
        {
            if (_balance < amount) return false;
            _balance -= amount;
            OnBalanceChanged?.Invoke(_balance);
            return true;
        }

        public void Add(int amount)
        {
            _balance += amount;
            OnBalanceChanged?.Invoke(_balance);
        }
    }
}
