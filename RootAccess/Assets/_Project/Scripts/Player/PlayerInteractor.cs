using System.Collections.Generic;
using RootAccess.Core;
using RootAccess.Input;
using UnityEngine;

namespace RootAccess.Player
{
    /// <summary>Finds the closest usable <see cref="IInteractable"/>, shows its prompt and fires it on Interact.</summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private InputReader input;
        [SerializeField, Min(0.1f)] private float radius = 1.2f;
        [SerializeField, Min(0.02f)] private float scanInterval = 0.1f;

        private readonly List<Collider2D> _results = new(16);
        private PlayerController _controller;
        private ContactFilter2D _filter;
        private IInteractable _current;
        private string _shownPrompt;
        private float _nextScan;

        public IInteractable Current => _current;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _filter = new ContactFilter2D { useLayerMask = true, layerMask = Layers.InteractableMask, useTriggers = true };
        }

        private void OnEnable()
        {
            if (input != null) input.InteractEvent += OnInteract;
        }

        private void OnDisable()
        {
            if (input != null) input.InteractEvent -= OnInteract;
            SetCurrent(null);
        }

        private void Update()
        {
            if (Time.time < _nextScan) return;
            _nextScan = Time.time + scanInterval;
            SetCurrent(_controller.IsAlive && _controller.StateId != PlayerStateId.Hack ? FindBest() : null);
        }

        private IInteractable FindBest()
        {
            Vector2 origin = transform.position;
            int count = Physics2D.OverlapCircle(origin, radius, _filter, _results);
            IInteractable best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                IInteractable candidate = _results[i].GetComponentInParent<IInteractable>();
                if (candidate == null) continue;
                float distance = ((Vector2)candidate.InteractionPosition - origin).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = candidate;
            }

            return best;
        }

        private void SetCurrent(IInteractable next)
        {
            string prompt = next?.Prompt;
            if (ReferenceEquals(next, _current) && prompt == _shownPrompt) return;
            _current = next;
            _shownPrompt = prompt;
            EventBus<InteractionPromptChanged>.Publish(new InteractionPromptChanged(prompt, next != null));
        }

        private void OnInteract()
        {
            if (_current == null || !_current.CanInteract(_controller)) return;
            _current.Interact(_controller);
            _nextScan = 0f;
        }
    }
}
