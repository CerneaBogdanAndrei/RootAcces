using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RootAccess.Gameplay
{
    /// <summary>
    /// Transmitter at the top of the Halden antenna (Level 6). Hacking its terminal arms it; the broadcast
    /// (<see cref="UplinkTransfer"/>) starts as soon as the player is at the transmitter and only advances while they stay
    /// inside <see cref="zoneSize"/>. Starting it raises the alarm; the drones held in <see cref="waves"/> switch on as it
    /// climbs. At 100% the <see cref="targets"/> open (the way to the exit), the drones drop and the objective is reported.
    /// <para>Arming (not starting) on hack matters on a replay: a terminal hacked in an earlier run re-applies its targets
    /// when the level loads, and the alarm must not go off while the player is still at the other end of the level.</para>
    /// </summary>
    public sealed class UplinkStation : MonoBehaviour, IHackable
    {
        private const float StreamInterval = 0.45f;

        [SerializeField, Min(1f)] private float seconds = 24f;
        [SerializeField] private string completedObjectiveId = "uplink";
        [SerializeField, Tooltip("Size of the area (centred above the pivot) the player must stay in.")]
        private Vector2 zoneSize = new(11f, 7f);
        [SerializeField, Tooltip("Inactive drones (or groups) switched on one by one as the broadcast advances.")]
        private GameObject[] waves = new GameObject[0];
        [SerializeField, Tooltip("Hackables opened when the broadcast completes (the door to the exit).")]
        private MonoBehaviour[] targets = new MonoBehaviour[0];
        [SerializeField] private Light2D beacon;
        [SerializeField] private Transform emitter;

        private UplinkTransfer _transfer;
        private bool _armed;
        private int _released;
        private int _lastPercent = -1;
        private bool _lastPresent;
        private float _nextStream;

        public string HackLabel => Strings.HackTargets.Uplink;
        public bool IsHacked => _armed;
        public bool IsArmed => _armed;
        public UplinkTransfer Transfer => _transfer ??= new UplinkTransfer(seconds, waves.Length);
        public Bounds Zone => new(transform.position + Vector3.up * (zoneSize.y * 0.5f), zoneSize);
        public int ReleasedWaves => _released;

        private void Awake()
        {
            foreach (GameObject wave in waves)
                if (wave != null) wave.SetActive(false);
        }

        private void OnDisable()
        {
            if (_transfer is { Running: true }) EventBus<UplinkProgressChanged>.Publish(new UplinkProgressChanged(_transfer.Progress, false, false));
        }

        public void ApplyHack(HackAction action) => _armed = true;

        /// <summary>True while the living player stands inside the transmitter's zone.</summary>
        public bool PlayerPresent()
        {
            if (!ServiceLocator.TryGet(out IPlayerTarget player) || !player.IsAlive) return false;
            Bounds zone = Zone;
            Vector2 p = player.Position;
            return p.x >= zone.min.x && p.x <= zone.max.x && p.y >= zone.min.y && p.y <= zone.max.y;
        }

        private void Update() => Step(Time.deltaTime);

        /// <summary>One frame of the broadcast (tests call it with a large step to skip the wait).</summary>
        internal void Step(float deltaTime)
        {
            if (!_armed || Transfer.Completed) return;
            bool present = PlayerPresent();
            if (!_transfer.Started)
            {
                if (!present) return;
                Begin();
            }

            if (_transfer.Tick(deltaTime, present))
            {
                Complete();
                return;
            }

            ReleaseWaves();
            if (present && emitter != null && Time.time >= _nextStream)
            {
                _nextStream = Time.time + StreamInterval;
                EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.DataStream, emitter.position));
            }

            if (beacon != null) beacon.intensity = present ? 1.2f + Mathf.PingPong(Time.time * 4f, 1.2f) : 0.4f;
            if (present == _lastPresent && _transfer.Percent == _lastPercent) return;
            _lastPresent = present;
            _lastPercent = _transfer.Percent;
            EventBus<UplinkProgressChanged>.Publish(new UplinkProgressChanged(_transfer.Progress, true, present));
        }

        private void Begin()
        {
            _transfer.Start();
            EventBus<AlarmRaised>.Publish(new AlarmRaised(transform.position));
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Alarm, transform.position));
            EventBus<ScreenShakeRequested>.Publish(new ScreenShakeRequested(0.5f));
            EventBus<ToastRequested>.Publish(new ToastRequested(Strings.Hud.UplinkStarted));
        }

        private void ReleaseWaves()
        {
            int due = Mathf.Min(_transfer.WavesDue, waves.Length);
            while (_released < due)
            {
                GameObject wave = waves[_released++];
                if (wave == null) continue;
                wave.SetActive(true);
                EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.TeleportIn, wave.transform.position));
            }
        }

        private void Complete()
        {
            foreach (MonoBehaviour target in targets)
                if (target is IHackable hackable)
                    hackable.ApplyHack(HackAction.Open);

            // The broadcast hijacks Halden's own network: every drone it called up drops out of the sky.
            foreach (GameObject wave in waves)
            {
                if (wave == null || !wave.activeInHierarchy) continue;
                foreach (IEmpTarget drone in wave.GetComponentsInChildren<IEmpTarget>()) drone.ApplyEmp(30f);
            }

            if (beacon != null) beacon.intensity = 2.4f;
            EventBus<UplinkProgressChanged>.Publish(new UplinkProgressChanged(1f, false, true));
            EventBus<ObjectiveReported>.Publish(new ObjectiveReported(ObjectiveType.Reach, completedObjectiveId));
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.HackSuccess, transform.position));
            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.EmpExplosion, emitter != null ? emitter.position : transform.position));
            EventBus<ScreenShakeRequested>.Publish(new ScreenShakeRequested(0.8f));
            EventBus<ToastRequested>.Publish(new ToastRequested(Strings.Hud.UplinkDone));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 1f, 1f, 0.5f);
            Bounds zone = Zone;
            Gizmos.DrawWireCube(zone.center, zone.size);
        }
    }
}
