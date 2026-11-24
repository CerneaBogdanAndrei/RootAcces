using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>Kinematic platform travelling between two points. Locked elevators must be hacked first.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Elevator : MonoBehaviour, IInteractable, IHackable
    {
        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;
        [SerializeField] private float speed = 2.5f;
        [SerializeField] private bool locked = true;
        [SerializeField] private Animator animator;

        private Rigidbody2D _body;
        private Transform _target;
        private bool _moving;

        public string HackLabel => Strings.HackTargets.Elevator;
        public bool IsHacked => !locked;
        public string Prompt => locked ? Strings.Prompts.NodeLocked : Strings.Prompts.UseElevator;
        public Vector3 InteractionPosition => transform.position;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _target = pointA;
        }

        public bool CanInteract(IInteractor interactor) => !locked && !_moving && pointA != null && pointB != null;

        public void Interact(IInteractor interactor)
        {
            if (!CanInteract(interactor)) return;
            _target = _target == pointA ? pointB : pointA;
            _moving = true;
            if (animator != null) animator.SetBool(AnimatorParams.Active, true);
        }

        public void ApplyHack(HackAction action) => locked = false;

        private void FixedUpdate()
        {
            if (!_moving || _target == null) return;
            Vector2 next = Vector2.MoveTowards(_body.position, _target.position, speed * Time.fixedDeltaTime);
            _body.MovePosition(next);
            if ((next - (Vector2)_target.position).sqrMagnitude > 0.0001f) return;
            _moving = false;
            if (animator != null) animator.SetBool(AnimatorParams.Active, false);
        }
    }
}
