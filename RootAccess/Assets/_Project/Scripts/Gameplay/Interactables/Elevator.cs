using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>
    /// Kinematic platform travelling between two points. Locked elevators must be hacked first. With
    /// <see cref="autoReturnSeconds"/> set, an elevator left empty at B rides back to A, so a player who drops back
    /// down the shaft can always call it again.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Elevator : MonoBehaviour, IInteractable, IHackable
    {
        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;
        [SerializeField] private float speed = 2.5f;
        [SerializeField] private bool locked = true;
        [SerializeField] private Animator animator;
        [SerializeField, Min(0f), Tooltip("0 = stay at B. Otherwise ride back to A after this many seconds without a rider.")]
        private float autoReturnSeconds;

        private Rigidbody2D _body;
        private Transform _target;
        private bool _moving;
        private int _riders;
        private float _emptySince = -1f;

        public string HackLabel => Strings.HackTargets.Elevator;
        public bool IsHacked => !locked;
        public bool IsMoving => _moving;
        public bool IsAtB => !_moving && _target == pointB;
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
            MoveTo(_target == pointA ? pointB : pointA);
        }

        public void ApplyHack(HackAction action) => locked = false;

        private void MoveTo(Transform target)
        {
            _target = target;
            _moving = true;
            _emptySince = -1f;
            if (animator != null) animator.SetBool(AnimatorParams.Active, true);
        }

        private void Update()
        {
            if (autoReturnSeconds <= 0f || !IsAtB || _riders > 0)
            {
                _emptySince = -1f;
                return;
            }

            if (_emptySince < 0f) _emptySince = Time.time;
            else if (Time.time - _emptySince >= autoReturnSeconds) MoveTo(pointA);
        }

        private void FixedUpdate()
        {
            if (!_moving || _target == null) return;
            Vector2 next = Vector2.MoveTowards(_body.position, _target.position, speed * Time.fixedDeltaTime);
            _body.MovePosition(next);
            if ((next - (Vector2)_target.position).sqrMagnitude > 0.0001f) return;
            _moving = false;
            if (animator != null) animator.SetBool(AnimatorParams.Active, false);
        }

        // The interact zone (child trigger) reports here because this object owns the Rigidbody2D.
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<IPlayerTarget>() != null) _riders++;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<IPlayerTarget>() != null) _riders = Mathf.Max(0, _riders - 1);
        }
    }
}
