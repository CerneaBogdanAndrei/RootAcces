using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Player
{
    /// <summary>
    /// Stealth state: inside a shadow zone and crouching (or standing still) the player is hidden — moved to the
    /// Hidden physics layer so vision raycasts and enemy bodies ignore it, and tinted dark.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerVisibility : MonoBehaviour, IHideable
    {
        [SerializeField] private SpriteRenderer tintRenderer;
        [SerializeField] private Color hiddenTint = new(0.35f, 0.4f, 0.55f, 0.85f);
        [SerializeField] private float stillSpeedThreshold = 0.6f;

        private PlayerController _controller;
        private int _hideZones;
        private Color _visibleTint = Color.white;

        public bool IsHidden { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            if (tintRenderer != null) _visibleTint = tintRenderer.color;
        }

        public void SetInHideZone(bool inside) => _hideZones = Mathf.Max(0, _hideZones + (inside ? 1 : -1));

        private void Update()
        {
            bool still = Mathf.Abs(_controller.Velocity.x) < stillSpeedThreshold;
            bool hidden = _hideZones > 0 && _controller.IsAlive &&
                          (_controller.IsCrouching || (still && _controller.IsGrounded));
            if (hidden == IsHidden) return;

            IsHidden = hidden;
            gameObject.layer = hidden ? Layers.Hidden : Layers.Player;
            if (tintRenderer != null) tintRenderer.color = hidden ? hiddenTint : _visibleTint;
            EventBus<PlayerHiddenChanged>.Publish(new PlayerHiddenChanged(hidden));
        }
    }
}
