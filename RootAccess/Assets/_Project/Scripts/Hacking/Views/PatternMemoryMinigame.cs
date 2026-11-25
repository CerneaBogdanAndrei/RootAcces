using System.Collections;
using RootAccess.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RootAccess.Hacking
{
    /// <summary>3×3 grid view for <see cref="PatternMemoryModel"/> (mouse, keyboard or gamepad via UI navigation).</summary>
    public sealed class PatternMemoryMinigame : HackingMinigame
    {
        private const int GridSize = 3;

        [SerializeField] private Color idleColor = new(0.05f, 0.22f, 0.3f, 1f);
        [SerializeField] private Color flashColor = new(0.3f, 1f, 1f, 1f);
        [SerializeField] private Color correctColor = new(0.25f, 1f, 0.55f, 1f);
        [SerializeField] private Color wrongColor = new(1f, 0.25f, 0.3f, 1f);
        [SerializeField] private float flashSeconds = 0.45f;
        [SerializeField] private float gapSeconds = 0.15f;

        private PatternMemoryModel _model;
        private Button[] _cells;
        private Coroutine _routine;

        public override HackingMinigameType Type => HackingMinigameType.PatternMemory;

        /// <summary>Test hook: the running puzzle (null before the first Begin).</summary>
        internal PatternMemoryModel Model => _model;
        public override string Hint => Strings.Hacking.PatternMemoryHint;

        protected override void OnBegin(int difficulty, int seed)
        {
            _model = new PatternMemoryModel(GridSize * GridSize, 2 + difficulty, 2 + (difficulty + 1) / 2, seed);
            BuildGrid();
            _routine = StartCoroutine(PlayRound());
        }

        protected override void OnAbort()
        {
            if (_routine != null) StopCoroutine(_routine);
        }

        private void BuildGrid()
        {
            ClearContent();
            if (!content.TryGetComponent(out GridLayoutGroup grid)) grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(110f, 110f);
            grid.spacing = new Vector2(14f, 14f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = GridSize;
            grid.childAlignment = TextAnchor.MiddleCenter;

            _cells = new Button[GridSize * GridSize];
            for (int i = 0; i < _cells.Length; i++)
            {
                int index = i;
                _cells[i] = CreateButton(string.Empty, idleColor, () => OnCellPressed(index));
            }
        }

        private IEnumerator PlayRound()
        {
            SetInteractable(false);
            SetStatus(Strings.Hacking.Watch);
            yield return new WaitForSecondsRealtime(0.5f);

            for (int step = 0; step < _model.CurrentLength; step++)
            {
                int cell = _model.GetShownCell(step);
                Paint(cell, flashColor);
                EventBus<SfxRequested>.Publish(SfxRequested.Ui(SfxId.HackBeep));
                yield return new WaitForSecondsRealtime(flashSeconds);
                Paint(cell, idleColor);
                yield return new WaitForSecondsRealtime(gapSeconds);
            }

            _model.BeginInput();
            SetInteractable(true);
            Select(_cells[GridSize * GridSize / 2]);
            SetStatus(Strings.Hacking.YourTurn);
            _routine = null;
        }

        /// <summary>Test hook: same path as clicking a grid cell.</summary>
        internal void PressCell(int index) => OnCellPressed(index);

        private void OnCellPressed(int index)
        {
            if (!IsRunning) return;
            PatternInputResult result = _model.Submit(index);
            switch (result)
            {
                case PatternInputResult.Correct:
                    EventBus<SfxRequested>.Publish(SfxRequested.Ui(SfxId.UiClick));
                    StartCoroutine(Blink(index, correctColor));
                    break;
                case PatternInputResult.RoundComplete:
                    StartCoroutine(Blink(index, correctColor));
                    _routine = StartCoroutine(PlayRound());
                    break;
                case PatternInputResult.PuzzleComplete:
                    SetInteractable(false);
                    Paint(index, correctColor);
                    Complete(true);
                    break;
                case PatternInputResult.Wrong:
                    SetInteractable(false);
                    Paint(index, wrongColor);
                    Complete(false);
                    break;
            }
        }

        private IEnumerator Blink(int index, Color color)
        {
            Paint(index, color);
            yield return new WaitForSecondsRealtime(0.15f);
            Paint(index, idleColor);
        }

        private void Paint(int index, Color color)
        {
            if (_cells[index].TryGetComponent(out Image image)) image.color = color;
        }

        private void SetInteractable(bool interactable)
        {
            foreach (Button cell in _cells) cell.interactable = interactable;
        }
    }
}
