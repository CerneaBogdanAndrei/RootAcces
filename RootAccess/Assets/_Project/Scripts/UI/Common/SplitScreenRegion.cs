using RootAccess.Core;
using UnityEngine;

namespace RootAccess.UI
{
    /// <summary>
    /// Local split-screen co-op: keeps one of the Infiltrator's overlays (HUD, dialogue box, hacking minigame) inside the
    /// left half of the screen, because the Operator's console owns the right half. A box wider than the half is scaled
    /// down to fit instead of being cut by the console. Full co-op off restores the original layout.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SplitScreenRegion : MonoBehaviour
    {
        [SerializeField, Tooltip("Moved into the left half; this object when empty.")]
        private RectTransform region;
        [SerializeField, Tooltip("Scaled down when it is wider than the half (a fixed-size box inside the region, or the region itself).")]
        private RectTransform fitContent;
        [SerializeField, Min(0f)] private float margin = 24f;

        private Vector2 _anchorMin;
        private Vector2 _anchorMax;
        private Vector2 _position;
        private Vector3 _contentScale = Vector3.one;

        public bool IsSplit { get; private set; }

        public void Configure(RectTransform target, RectTransform content)
        {
            region = target;
            fitContent = content;
        }

        private void Awake()
        {
            if (region == null) region = (RectTransform)transform;
            _anchorMin = region.anchorMin;
            _anchorMax = region.anchorMax;
            _position = region.anchoredPosition;
            if (fitContent != null) _contentScale = fitContent.localScale;
            // Subscribed for the whole lifetime: panels stay alive while hidden and must follow a switch made meanwhile.
            EventBus<CoopViewChanged>.Subscribe(OnCoopView);
        }

        private void Start()
        {
            if (ServiceLocator.TryGet(out IOperatorConsole console) && console.Mode == CoopMode.LocalSplit) Apply(true);
        }

        private void OnDestroy() => EventBus<CoopViewChanged>.Unsubscribe(OnCoopView);

        /// <summary>The half's width follows the resolution (and is unknown on the very first frame), so the fit is kept current.</summary>
        private void LateUpdate()
        {
            if (!IsSplit || fitContent == null) return;
            Vector3 scale = _contentScale * FitScale();
            if (fitContent.localScale != scale) fitContent.localScale = scale;
        }

        private void OnCoopView(CoopViewChanged evt) => Apply(evt.Split);

        /// <summary>Left half (scaled to fit) when <paramref name="split"/>, the original layout otherwise.</summary>
        public void Apply(bool split)
        {
            IsSplit = split;
            region.anchorMin = split ? new Vector2(_anchorMin.x * 0.5f, _anchorMin.y) : _anchorMin;
            region.anchorMax = split ? new Vector2(_anchorMax.x * 0.5f, _anchorMax.y) : _anchorMax;
            region.anchoredPosition = split ? new Vector2(_position.x * 0.5f, _position.y) : _position;
            if (fitContent != null) fitContent.localScale = _contentScale * (split ? FitScale() : 1f);
        }

        private float FitScale()
        {
            if (region.parent is not RectTransform parent) return 1f;
            float available = parent.rect.width * 0.5f - 2f * margin;
            float width = fitContent.rect.width;
            if (available <= 0f || width <= 0f) return 1f;
            return width > available ? available / width : 1f;
        }
    }
}
