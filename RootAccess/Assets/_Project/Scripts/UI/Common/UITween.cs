using UnityEngine;

namespace RootAccess.UI
{
    /// <summary>
    /// Unscaled-time fade + scale transition for a panel (works while the game is paused). Driven per-frame, no
    /// coroutines or allocations.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UITween : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float duration = 0.18f;
        [SerializeField] private float hiddenScale = 0.94f;
        [SerializeField] private RectTransform scaleTarget;

        private CanvasGroup _group;
        private float _from;
        private float _to;
        private float _elapsed;
        private bool _running;

        public bool IsShown => _to > 0.5f;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (scaleTarget == null) scaleTarget = transform as RectTransform;
        }

        public void Play(bool show, bool instant = false)
        {
            if (_group == null) Awake();
            _from = _group.alpha;
            _to = show ? 1f : 0f;
            _elapsed = 0f;
            _group.interactable = show;
            _group.blocksRaycasts = show;
            _running = !instant;
            if (instant) Apply(_to);
        }

        private void Update()
        {
            if (!_running) return;
            _elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_elapsed / duration);
            float eased = 1f - (1f - t) * (1f - t);
            Apply(Mathf.Lerp(_from, _to, eased));
            if (t >= 1f) _running = false;
        }

        private void Apply(float visibility)
        {
            _group.alpha = visibility;
            if (scaleTarget != null) scaleTarget.localScale = Vector3.one * Mathf.Lerp(hiddenScale, 1f, visibility);
        }
    }
}
