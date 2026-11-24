using RootAccess.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RootAccess.AI
{
    /// <summary>
    /// Sweeping security camera. Seeing the player fills a detection meter; when full it raises the alarm and keeps
    /// adding trace while the player stays visible. Hacking it (Terminal_Camera) disables or loops it for a while.
    /// </summary>
    public sealed class SecurityCamera : MonoBehaviour, IHackable
    {
        [SerializeField] private VisionCone vision;
        [SerializeField] private Transform pivot;
        [SerializeField] private float centerAngle = -60f;
        [SerializeField] private float sweepAngle = 35f;
        [SerializeField] private float sweepSpeed = 0.35f;
        [SerializeField] private float detectionSeconds = 1.1f;
        [SerializeField] private float tracePerSecond = 25f;
        [SerializeField] private float alarmTrace = 30f;
        [SerializeField] private float hackedDuration = 20f;
        [SerializeField] private Light2D statusLight;
        [SerializeField] private Animator animator;
        [SerializeField] private Color idleLightColor = new(0.3f, 1f, 0.5f);
        [SerializeField] private Color alertLightColor = new(1f, 0.15f, 0.2f);

        private float _detection;
        private bool _alarmRaised;
        private float _disabledUntil;
        private float _sweepTime;

        public string HackLabel => Strings.HackTargets.Camera;
        public bool IsHacked => Time.time < _disabledUntil;
        public float Detection => _detection;

        private void Awake()
        {
            if (vision == null) Debug.LogError($"[SecurityCamera] '{name}' has no VisionCone.", this);
            if (pivot == null) pivot = transform;
        }

        private void Update()
        {
            bool hacked = IsHacked;
            if (vision != null) vision.SetActive(!hacked);
            if (animator != null) animator.SetBool(AnimatorParams.Active, !hacked);
            if (hacked)
            {
                _detection = 0f;
                UpdateLight(0f, true);
                return;
            }

            _sweepTime += Time.deltaTime * sweepSpeed;
            float angle = centerAngle + Mathf.Sin(_sweepTime * Mathf.PI * 2f) * sweepAngle;
            if (_detection <= 0f) pivot.localRotation = Quaternion.Euler(0f, 0f, angle);

            ServiceLocator.TryGet(out IPlayerTarget player);
            bool sees = vision != null && vision.CanSee(player, out _);
            if (sees)
            {
                _detection = Mathf.Min(1f, _detection + Time.deltaTime / detectionSeconds);
                Vector2 toPlayer = player.Position - (Vector2)pivot.position;
                float trackAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
                pivot.rotation = Quaternion.RotateTowards(pivot.rotation, Quaternion.Euler(0f, 0f, trackAngle), 90f * Time.deltaTime);
                if (_detection >= 1f)
                {
                    if (!_alarmRaised)
                    {
                        _alarmRaised = true;
                        EventBus<AlarmRaised>.Publish(new AlarmRaised(player.Position));
                        EventBus<TraceNoise>.Publish(new TraceNoise(alarmTrace, TraceSource.Camera, player.Position));
                        EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Alarm, transform.position));
                    }

                    EventBus<TraceNoise>.Publish(new TraceNoise(tracePerSecond * Time.deltaTime, TraceSource.Camera, player.Position));
                }
            }
            else
            {
                _detection = Mathf.Max(0f, _detection - Time.deltaTime / (detectionSeconds * 2f));
                if (_detection <= 0f) _alarmRaised = false;
            }

            if (vision != null) vision.SetAlert(_detection);
            UpdateLight(_detection, false);
        }

        public void ApplyHack(HackAction action)
        {
            bool disable = action is HackAction.Disable or HackAction.Toggle or HackAction.Close;
            _disabledUntil = disable ? Time.time + hackedDuration : 0f;
            _detection = 0f;
            _alarmRaised = false;
        }

        private void UpdateLight(float detection, bool off)
        {
            if (statusLight == null) return;
            statusLight.enabled = !off;
            statusLight.color = Color.Lerp(idleLightColor, alertLightColor, detection);
        }
    }
}
