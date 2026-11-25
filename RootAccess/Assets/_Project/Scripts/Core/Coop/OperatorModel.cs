using UnityEngine;

namespace RootAccess.Core
{
    /// <summary>
    /// The Operator's bandwidth: actions cost energy that regenerates over time, so the second player supports the
    /// infiltrator instead of opening every door at once. Plain C#, ticked by the infiltrator's side (authoritative).
    /// </summary>
    public sealed class OperatorModel
    {
        public const float DefaultMaxEnergy = 100f;
        public const float DefaultRegenPerSecond = 6f;

        public OperatorModel(float maxEnergy = DefaultMaxEnergy, float regenPerSecond = DefaultRegenPerSecond)
        {
            MaxEnergy = Mathf.Max(1f, maxEnergy);
            RegenPerSecond = Mathf.Max(0f, regenPerSecond);
            Energy = MaxEnergy;
        }

        public float MaxEnergy { get; }
        public float RegenPerSecond { get; }
        public float Energy { get; private set; }

        /// <summary>Opening a door is the strongest help, a distraction the cheapest.</summary>
        public static float CostOf(OperatorTargetKind kind) => kind switch
        {
            OperatorTargetKind.Door => 35f,
            OperatorTargetKind.Camera => 25f,
            _ => 20f
        };

        public bool CanAfford(OperatorTargetKind kind) => Energy >= CostOf(kind);

        public bool TrySpend(OperatorTargetKind kind)
        {
            if (!CanAfford(kind)) return false;
            Energy -= CostOf(kind);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime > 0f) Energy = Mathf.Min(MaxEnergy, Energy + RegenPerSecond * deltaTime);
        }

        public void Refill() => Energy = MaxEnergy;
    }
}
