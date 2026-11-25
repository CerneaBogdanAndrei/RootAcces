using RootAccess.Core;
using RootAccess.Input;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Player
{
    /// <summary>
    /// Throws an EMP grenade (one from the inventory) toward the mouse cursor or right stick, or forward when neither aims.
    /// The grenade prefab is pooled and only needs a Rigidbody2D here; its blast logic lives in the Gameplay assembly.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerGrenadeThrower : MonoBehaviour
    {
        private const float StickDeadZone = 0.35f;
        private const float UpwardBias = 0.35f;

        [SerializeField] private InputReader input;
        [SerializeField] private GameObject grenadePrefab;
        [SerializeField] private ItemDefinition grenadeItem;
        [SerializeField, Min(1f)] private float throwSpeed = 12f;
        [SerializeField, Min(0f)] private float cooldown = 0.6f;
        [SerializeField] private Vector2 spawnOffset = new(0.35f, 1.1f);

        private PlayerController _controller;
        private float _readyAt;

        private void Awake() => _controller = GetComponent<PlayerController>();

        private void OnEnable()
        {
            if (input != null) input.ThrowEvent += OnThrow;
        }

        private void OnDisable()
        {
            if (input != null) input.ThrowEvent -= OnThrow;
        }

        private void OnThrow()
        {
            if (!ServiceLocator.TryGet(out GameManager gameManager) || gameManager.State != GameState.Playing) return;
            Throw(AimDirection());
        }

        /// <summary>Throws one grenade in <paramref name="direction"/>. Returns false (with a toast) when it cannot.</summary>
        public bool Throw(Vector2 direction)
        {
            if (grenadePrefab == null || grenadeItem == null || Time.time < _readyAt) return false;
            if (!_controller.IsAlive || _controller.StateId is PlayerStateId.Hack or PlayerStateId.Teleport) return false;
            if (!ServiceLocator.TryGet(out SessionService session) || !session.Session.Inventory.Remove(grenadeItem))
            {
                EventBus<ToastRequested>.Publish(new ToastRequested(Strings.Hud.NoEmp));
                return false;
            }

            _readyAt = Time.time + cooldown;
            Vector2 origin = _controller.Position + new Vector2(spawnOffset.x * _controller.Facing, spawnOffset.y);
            GameObject grenade = ServiceLocator.TryGet(out PoolService pool)
                ? pool.Spawn(grenadePrefab, origin, Quaternion.identity)
                : Instantiate(grenadePrefab, origin, Quaternion.identity);
            if (grenade.TryGetComponent(out Collider2D grenadeCollider)) Physics2D.IgnoreCollision(grenadeCollider, _controller.Capsule);
            if (grenade.TryGetComponent(out Rigidbody2D body))
                body.linearVelocity = direction.normalized * throwSpeed + _controller.Velocity * 0.3f;
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Dash, origin));
            return true;
        }

        private Vector2 AimDirection()
        {
            Vector2 direction;
            if (input.AimStick.sqrMagnitude > StickDeadZone * StickDeadZone)
            {
                direction = input.AimStick;
            }
            else if (Camera.main != null && input.PointerPosition != Vector2.zero)
            {
                Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(input.PointerPosition.x, input.PointerPosition.y, -Camera.main.transform.position.z));
                direction = (Vector2)world - (_controller.Position + Vector2.up * spawnOffset.y);
            }
            else
            {
                direction = new Vector2(_controller.Facing, 0f);
            }

            direction = direction.sqrMagnitude < 0.001f ? new Vector2(_controller.Facing, 0f) : direction.normalized;
            direction.y += UpwardBias;
            return direction.normalized;
        }
    }
}
