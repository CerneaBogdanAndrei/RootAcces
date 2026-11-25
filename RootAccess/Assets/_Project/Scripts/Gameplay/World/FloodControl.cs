using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>
    /// Sluice controls for a <see cref="WaterBody2D"/>: hacking the linked terminal opens the gate and the water rises
    /// (bottom fixed) to <see cref="floodedDepth"/>, so the player can swim up to a ledge that was out of reach.
    /// </summary>
    public sealed class FloodControl : MonoBehaviour, IHackable
    {
        [SerializeField] private WaterBody2D water;
        [SerializeField, Min(0.2f)] private float floodedDepth = 8f;
        [SerializeField, Min(0.1f)] private float riseSpeed = 2f;

        private bool _flooding;

        public string HackLabel => Strings.HackTargets.Sluice;
        public bool IsHacked { get; private set; }
        public bool IsFlooded => water != null && water.Depth >= floodedDepth - 0.01f;

        private void Awake()
        {
            if (water == null) Debug.LogError($"[FloodControl] '{name}' has no WaterBody2D.", this);
        }

        public void ApplyHack(HackAction action)
        {
            if (IsHacked || water == null) return;
            IsHacked = true;
            _flooding = true;
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Splash, water.transform.position));
            EventBus<ScreenShakeRequested>.Publish(new ScreenShakeRequested(0.4f));
            EventBus<ToastRequested>.Publish(new ToastRequested(Strings.Hud.SluiceOpened));
        }

        private void FixedUpdate()
        {
            if (!_flooding) return;
            water.SetDepth(Mathf.MoveTowards(water.Depth, floodedDepth, riseSpeed * Time.fixedDeltaTime));
            if (IsFlooded) _flooding = false;
        }
    }
}
