using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>
    /// Rusty grate that gives way under the player (Level 6): it shakes for <see cref="crumbleDelay"/> after someone lands
    /// on it, then drops out (collider off, sprite falling and fading) and comes back after <see cref="respawnSeconds"/>,
    /// as soon as nobody is standing where it was.
    /// </summary>
    public sealed class CrumblingPlatform : MonoBehaviour
    {
        private enum Phase { Solid, Shaking, Fallen }

        [SerializeField] private Collider2D solid;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField, Min(0.05f)] private float crumbleDelay = 0.55f;
        [SerializeField, Min(0.5f)] private float respawnSeconds = 3f;
        [SerializeField, Min(0f)] private float shakeAmplitude = 0.05f;

        private Phase _phase = Phase.Solid;
        private float _phaseUntil;
        private Vector3 _visualHome;
        private Color _visualColor = Color.white;
        private Bounds _solidBounds;

        public bool IsSolid => _phase != Phase.Fallen;
        public bool IsShaking => _phase == Phase.Shaking;

        private void Awake()
        {
            if (solid == null) solid = GetComponent<Collider2D>();
            if (solid == null) Debug.LogError($"[CrumblingPlatform] '{name}' has no collider.", this);
            if (visual == null) return;
            _visualHome = visual.transform.localPosition;
            _visualColor = visual.color;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_phase != Phase.Solid || collision.collider.GetComponentInParent<IPlayerTarget>() == null) return;
            // Only weight from above: bumping the grate from below or the side leaves it alone.
            if (collision.collider.bounds.min.y < solid.bounds.max.y - 0.15f) return;
            Trigger();
        }

        /// <summary>Starts the shake-then-fall sequence (also used by tests).</summary>
        internal void Trigger()
        {
            if (_phase != Phase.Solid) return;
            _phase = Phase.Shaking;
            _phaseUntil = Time.time + crumbleDelay;
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Land, transform.position));
        }

        private void Update()
        {
            switch (_phase)
            {
                case Phase.Shaking:
                    if (visual != null)
                        visual.transform.localPosition = _visualHome +
                                                         new Vector3(Random.Range(-shakeAmplitude, shakeAmplitude), Random.Range(-shakeAmplitude, shakeAmplitude), 0f);
                    if (Time.time >= _phaseUntil) Fall();
                    break;
                case Phase.Fallen:
                    if (visual != null)
                    {
                        float t = 1f - Mathf.Clamp01((_phaseUntil - Time.time) / respawnSeconds);
                        visual.transform.localPosition = _visualHome + Vector3.down * Mathf.Min(3f, t * 12f);
                        visual.color = new Color(_visualColor.r, _visualColor.g, _visualColor.b, _visualColor.a * Mathf.Clamp01(1f - t * 4f));
                    }

                    if (Time.time >= _phaseUntil && !Occupied()) Restore();
                    break;
            }
        }

        private void Fall()
        {
            _phase = Phase.Fallen;
            _phaseUntil = Time.time + respawnSeconds;
            // A disabled collider reports empty bounds: remember where the grate was for the respawn check.
            _solidBounds = solid.bounds;
            solid.enabled = false;
            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.LandingDust, transform.position));
        }

        private void Restore()
        {
            _phase = Phase.Solid;
            solid.enabled = true;
            if (visual == null) return;
            visual.transform.localPosition = _visualHome;
            visual.color = _visualColor;
        }

        /// <summary>The player is where the grate would reappear: wait, or it would be pushed out of the collider.</summary>
        private bool Occupied()
        {
            return Physics2D.OverlapBox(_solidBounds.center, _solidBounds.size + new Vector3(0f, 0.2f, 0f), 0f, Layers.PlayerMask | Layers.ToMask(Layers.Hidden)) != null;
        }
    }
}
