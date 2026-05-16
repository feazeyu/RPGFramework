using System;
using UnityEngine;

namespace Feazeyu.RPGSystems.Inventory
{
    public class PlayerWallet : MonoBehaviour, IShopCurrency
    {
        public static PlayerWallet Instance { get; private set; }

        [SerializeField] private int _balance = 100;

        public event Action<int> OnBalanceChanged;

        public int Balance => _balance;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
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
