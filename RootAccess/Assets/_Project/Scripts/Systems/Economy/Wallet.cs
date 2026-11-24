using System;

namespace RootAccess.Systems
{
    /// <summary>Credits balance. Spending is all-or-nothing.</summary>
    public sealed class Wallet
    {
        public Wallet(int startingCredits = 0) => Credits = Math.Max(0, startingCredits);

        public event Action<int> Changed;

        public int Credits { get; private set; }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Credits += amount;
            Changed?.Invoke(Credits);
        }

        public bool CanAfford(int amount) => amount >= 0 && Credits >= amount;

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Credits < amount) return false;
            if (amount == 0) return true;
            Credits -= amount;
            Changed?.Invoke(Credits);
            return true;
        }

        public void Set(int credits)
        {
            Credits = Math.Max(0, credits);
            Changed?.Invoke(Credits);
        }
    }
}
