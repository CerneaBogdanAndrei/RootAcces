using RootAccess.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RootAccess.Hacking
{
    /// <summary>Timing bar view for <see cref="PortScanModel"/>; confirm with Submit (Space/Enter/A) or a mouse click.</summary>
    public sealed class PortScanMinigame : HackingMinigame
    {
        private const float BarWidth = 640f;
        private const float BarHeight = 46f;

        [SerializeField] private Color barColor = new(0.06f, 0.16f, 0.22f, 1f);
        [SerializeField] private Color windowColor = new(0.25f, 1f, 0.6f, 0.9f);
        [SerializeField] private Color cursorColor = new(1f, 1f, 1f, 1f);

        private PortScanModel _model;
        private RectTransform _window;
        private RectTransform _cursor;

        public override HackingMinigameType Type => HackingMinigameType.PortScan;
        public override string Hint => Strings.Hacking.PortScanHint;

        /// <summary>Test hook: the running puzzle (null before the first Begin).</summary>
        internal PortScanModel Model => _model;

        protected override void OnBegin(int difficulty, int seed)
        {
            _model = new PortScanModel(2 + difficulty, 2, 0.55f + difficulty * 0.12f, 0.22f - difficulty * 0.025f, seed);
            Build();
            SetStatus(Hint);
        }

        public override void OnSubmit() => Scan();

        public override void OnClick() => Scan();

        private void Update()
        {
            if (!IsRunning || _model == null) return;
            _model.Tick(Time.unscaledDeltaTime);
            Layout();
        }

        private void Scan()
        {
            if (!IsRunning) return;
            switch (_model.Submit())
            {
                case PortScanResult.Hit:
                    EventBus<SfxRequested>.Publish(SfxRequested.Ui(SfxId.HackBeep));
                    SetStatus($"{Hint} {_model.Hits}/{_model.HitsRequired}");
                    break;
                case PortScanResult.Miss:
                    EventBus<SfxRequested>.Publish(SfxRequested.Ui(SfxId.UiBack));
                    SetStatus($"{Hint} ({_model.Misses}/{_model.AllowedMisses})");
                    break;
                case PortScanResult.Complete:
                    Complete(true);
                    break;
                case PortScanResult.Failed:
                    Complete(false);
                    break;
            }
        }

        private void Build()
        {
            ClearContent();
            if (content.TryGetComponent(out GridLayoutGroup grid)) Destroy(grid);
            if (content.TryGetComponent(out HorizontalLayoutGroup row)) Destroy(row);

            RectTransform bar = CreateRect("Bar", content, barColor, new Vector2(BarWidth, BarHeight));
            _window = CreateRect("Window", bar, windowColor, new Vector2(BarWidth * _model.WindowWidth, BarHeight));
            _cursor = CreateRect("Cursor", bar, cursorColor, new Vector2(6f, BarHeight + 18f));
            Layout();
        }

        private void Layout()
        {
            if (_window == null || _cursor == null) return;
            _window.anchoredPosition = new Vector2((_model.WindowStart + _model.WindowWidth * 0.5f - 0.5f) * BarWidth, 0f);
            _cursor.anchoredPosition = new Vector2((_model.Cursor - 0.5f) * BarWidth, 0f);
        }

        private static RectTransform CreateRect(string objectName, Transform parent, Color color, Vector2 size)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }
    }
}
