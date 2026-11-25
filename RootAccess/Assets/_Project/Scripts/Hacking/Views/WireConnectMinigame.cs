using RootAccess.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RootAccess.Hacking
{
    /// <summary>Two columns of coloured sockets: pick a left socket, then the matching right socket.</summary>
    public sealed class WireConnectMinigame : HackingMinigame
    {
        private static readonly Color[] Palette =
        {
            new(1f, 0.3f, 0.35f), new(0.3f, 0.85f, 1f), new(1f, 0.85f, 0.25f),
            new(0.45f, 1f, 0.45f), new(0.85f, 0.4f, 1f), new(1f, 0.6f, 0.2f)
        };

        private WireConnectModel _model;
        private Button[] _left;
        private Button[] _right;

        public override HackingMinigameType Type => HackingMinigameType.WireConnect;
        public override string Hint => Strings.Hacking.WireConnectHint;

        /// <summary>Test hook: the running puzzle (null before the first Begin).</summary>
        internal WireConnectModel Model => _model;

        /// <summary>Test hooks: same path as clicking a left or right socket.</summary>
        internal void PressLeft(int index) => OnLeft(index);

        internal void PressRight(int index) => OnRight(index);

        protected override void OnBegin(int difficulty, int seed)
        {
            int wires = Mathf.Clamp(2 + difficulty, 3, Palette.Length);
            _model = new WireConnectModel(wires, 1, seed);
            Build();
            SetStatus(Hint);
            Select(_left[0]);
        }

        private void Build()
        {
            ClearContent();
            if (content.TryGetComponent(out GridLayoutGroup oldGrid)) Destroy(oldGrid);
            if (!content.TryGetComponent(out HorizontalLayoutGroup row)) row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 160f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = false;
            row.childControlHeight = false;

            RectTransform leftColumn = CreateColumn("Left");
            RectTransform rightColumn = CreateColumn("Right");
            _left = new Button[_model.WireCount];
            _right = new Button[_model.WireCount];
            for (int i = 0; i < _model.WireCount; i++)
            {
                int index = i;
                _left[i] = CreateButton(string.Empty, Palette[_model.LeftColor(i)], () => OnLeft(index));
                _left[i].transform.SetParent(leftColumn, false);
                _right[i] = CreateButton(string.Empty, Palette[_model.RightColor(i)], () => OnRight(index));
                _right[i].transform.SetParent(rightColumn, false);
            }
        }

        private RectTransform CreateColumn(string columnName)
        {
            var go = new GameObject(columnName, typeof(RectTransform), typeof(VerticalLayoutGroup));
            var rect = (RectTransform)go.transform;
            rect.SetParent(content, false);
            rect.sizeDelta = new Vector2(90f, 420f);
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            return rect;
        }

        private void OnLeft(int index)
        {
            if (!IsRunning || _model.SelectLeft(index) != WireResult.Selected) return;
            EventBus<SfxRequested>.Publish(SfxRequested.Ui(SfxId.UiClick));
            for (int i = 0; i < _left.Length; i++) _left[i].transform.localScale = i == index ? Vector3.one * 1.2f : Vector3.one;
            Select(_right[0]);
        }

        private void OnRight(int index)
        {
            if (!IsRunning) return;
            int selected = _model.SelectedLeft;
            WireResult result = _model.ConnectRight(index);
            if (selected >= 0) _left[selected].transform.localScale = Vector3.one;

            switch (result)
            {
                case WireResult.Connected:
                case WireResult.Complete:
                    _left[selected].interactable = false;
                    _right[index].interactable = false;
                    EventBus<SfxRequested>.Publish(SfxRequested.Ui(SfxId.HackBeep));
                    if (result == WireResult.Complete) Complete(true);
                    else SelectFirstFreeLeft();
                    break;
                case WireResult.Mistake:
                    SetStatus($"{Hint} ({_model.Mistakes}/{_model.AllowedMistakes})");
                    EventBus<SfxRequested>.Publish(SfxRequested.Ui(SfxId.UiBack));
                    SelectFirstFreeLeft();
                    break;
                case WireResult.Failed:
                    Complete(false);
                    break;
            }
        }

        private void SelectFirstFreeLeft()
        {
            for (int i = 0; i < _left.Length; i++)
            {
                if (_model.IsLeftDone(i)) continue;
                Select(_left[i]);
                return;
            }
        }
    }
}
