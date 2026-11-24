using System;
using UnityEngine;

namespace RootAccess.Systems
{
    public enum TraceLevel
    {
        Clear,
        Suspicious,
        Alert,
        Lockdown
    }

    [Serializable]
    public struct TraceSettings
    {
        [Range(0f, 100f)] public float suspiciousThreshold;
        [Range(0f, 100f)] public float alertThreshold;
        [Min(0f)] public float decayPerSecond;
        [Min(0f)] public float decayDelaySeconds;
        [Range(0f, 100f)] public float lockdownFloor;

        public static TraceSettings Default => new()
        {
            suspiciousThreshold = 25f,
            alertThreshold = 60f,
            decayPerSecond = 6f,
            decayDelaySeconds = 3f,
            lockdownFloor = 50f
        };
    }

    /// <summary>
    /// Alert level 0–100. Rises from detections and failed hacks, decays after a quiet period and triggers a
    /// lockdown at 100 (decay stops at <see cref="TraceSettings.lockdownFloor"/> while locked down).
    /// </summary>
    public sealed class TraceMeter
    {
        public const float Max = 100f;

        private readonly TraceSettings _settings;
        private float _timeSinceLastIncrease;

        public TraceMeter(TraceSettings settings)
        {
            _settings = settings;
            _timeSinceLastIncrease = float.MaxValue;
        }

        public event Action<float> ValueChanged;
        public event Action<TraceLevel, TraceLevel> LevelChanged;
        public event Action LockdownTriggered;

        public float Value { get; private set; }
        public float Normalized => Value / Max;
        public TraceLevel Level { get; private set; } = TraceLevel.Clear;
        public bool IsLockdown => Level == TraceLevel.Lockdown;

        public void Add(float amount)
        {
            if (amount <= 0f) return;
            _timeSinceLastIncrease = 0f;
            SetValue(Value + amount);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || Value <= 0f) return;
            _timeSinceLastIncrease += deltaTime;
            if (_timeSinceLastIncrease < _settings.decayDelaySeconds) return;

            float floor = IsLockdown ? _settings.lockdownFloor : 0f;
            if (Value <= floor) return;
            SetValue(Mathf.Max(floor, Value - _settings.decayPerSecond * deltaTime));
        }

        public void ResetMeter()
        {
            _timeSinceLastIncrease = float.MaxValue;
            SetValue(0f, forceLevel: true);
        }

        public TraceLevel Evaluate(float value)
        {
            if (value >= Max) return TraceLevel.Lockdown;
            if (value >= _settings.alertThreshold) return TraceLevel.Alert;
            if (value >= _settings.suspiciousThreshold) return TraceLevel.Suspicious;
            return TraceLevel.Clear;
        }

        private void SetValue(float value, bool forceLevel = false)
        {
            float clamped = Mathf.Clamp(value, 0f, Max);
            if (!Mathf.Approximately(clamped, Value))
            {
                Value = clamped;
                ValueChanged?.Invoke(Value);
            }

            TraceLevel next = Evaluate(Value);
            if (IsLockdown && !forceLevel && Value >= _settings.lockdownFloor) next = TraceLevel.Lockdown;
            if (next == Level) return;

            TraceLevel previous = Level;
            Level = next;
            LevelChanged?.Invoke(previous, next);
            if (next == TraceLevel.Lockdown) LockdownTriggered?.Invoke();
        }
    }
}
