using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Player
{
    /// <summary>
    /// Presentation-only: mirrors controller state into Animator parameters and sprite facing.
    /// Lives on the visual child next to the Animator, so Animation Events (footsteps) land on sibling components.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerAnimatorBridge : MonoBehaviour
    {
        [SerializeField] private PlayerController controller;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Animator _animator;
        private PlayerStateId _lastState;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (controller == null) controller = GetComponentInParent<PlayerController>();
        }

        private void OnEnable()
        {
            if (controller != null) controller.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            if (controller != null) controller.StateChanged -= OnStateChanged;
        }

        private void LateUpdate()
        {
            if (controller == null) return;
            Vector2 velocity = controller.Velocity;
            PlayerStateId state = controller.StateId;
            float maxSpeed = controller.Config.MaxRunSpeed;

            _animator.SetFloat(AnimatorParams.Speed, Mathf.Clamp01(Mathf.Abs(velocity.x) / maxSpeed));
            _animator.SetFloat(AnimatorParams.VerticalSpeed, velocity.y);
            _animator.SetBool(AnimatorParams.Grounded, state == PlayerStateId.Grounded);
            _animator.SetBool(AnimatorParams.Crouching, controller.IsCrouching);
            _animator.SetBool(AnimatorParams.Dashing, state == PlayerStateId.Dash);
            _animator.SetBool(AnimatorParams.WallSliding, state == PlayerStateId.WallSlide);
            _animator.SetBool(AnimatorParams.Swimming, state == PlayerStateId.Swim);
            _animator.SetBool(AnimatorParams.Hacking, state == PlayerStateId.Hack);
            _animator.SetBool(AnimatorParams.Dead, state == PlayerStateId.Dead);

            if (spriteRenderer != null) spriteRenderer.flipX = controller.Facing < 0;
        }

        private void OnStateChanged(PlayerStateId state)
        {
            if (state == PlayerStateId.Hurt && _lastState != PlayerStateId.Hurt) _animator.SetTrigger(AnimatorParams.Hurt);
            _lastState = state;
        }
    }
}
