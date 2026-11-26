using RootAccess.Hacking;
using UnityEngine;

namespace RootAccess.Playthrough
{
    /// <summary>
    /// Solves whichever hacking minigame is open, through the real overlay and at a human pace: Port Scan presses when
    /// the cursor is in the open port, Wire Connect matches colours, Pattern Memory repeats the shown sequence.
    /// </summary>
    internal sealed class HackSolver
    {
        private const float PressInterval = 0.22f;

        private float _nextPressAt;
        private int _patternRound = -1;
        private int _patternStep;
        private bool _patternAwaiting;

        public string LastMinigame { get; private set; }

        public void Reset()
        {
            _patternRound = -1;
            _patternStep = 0;
            _patternAwaiting = false;
            _nextPressAt = 0f;
        }

        public void Tick(HackingOverlay overlay)
        {
            if (overlay == null || Time.unscaledTime < _nextPressAt) return;
            foreach (HackingMinigame minigame in overlay.GetComponentsInChildren<HackingMinigame>())
            {
                if (!minigame.isActiveAndEnabled) continue;
                switch (minigame)
                {
                    case PortScanMinigame portScan:
                        LastMinigame = "Port Scan";
                        SolvePortScan(portScan);
                        return;
                    case WireConnectMinigame wires:
                        LastMinigame = "Wire Connect";
                        SolveWires(wires);
                        return;
                    case PatternMemoryMinigame pattern:
                        LastMinigame = "Pattern Memory";
                        SolvePattern(pattern);
                        return;
                }
            }
        }

        private void SolvePortScan(PortScanMinigame minigame)
        {
            PortScanModel model = minigame.Model;
            if (model == null || model.IsComplete || model.IsFailed) return;
            model.SetCursor(model.WindowStart + model.WindowWidth * 0.5f);
            minigame.OnSubmit();
            _nextPressAt = Time.unscaledTime + PressInterval;
        }

        private void SolveWires(WireConnectMinigame minigame)
        {
            WireConnectModel model = minigame.Model;
            if (model == null || model.IsComplete || model.IsFailed) return;
            for (int left = 0; left < model.WireCount; left++)
            {
                if (model.IsLeftDone(left)) continue;
                for (int right = 0; right < model.WireCount; right++)
                {
                    if (model.IsRightDone(right) || model.RightColor(right) != model.LeftColor(left)) continue;
                    minigame.PressLeft(left);
                    minigame.PressRight(right);
                    _nextPressAt = Time.unscaledTime + PressInterval;
                    return;
                }
            }
        }

        private void SolvePattern(PatternMemoryMinigame minigame)
        {
            PatternMemoryModel model = minigame.Model;
            if (model == null || model.IsComplete || model.IsFailed) return;
            if (!model.AwaitingInput)
            {
                _patternAwaiting = false;
                return;
            }

            if (!_patternAwaiting || _patternRound != model.Round)
            {
                _patternAwaiting = true;
                _patternRound = model.Round;
                _patternStep = 0;
            }

            if (_patternStep >= model.CurrentLength) return;
            minigame.PressCell(model.Sequence[_patternStep]);
            _patternStep++;
            _nextPressAt = Time.unscaledTime + PressInterval;
        }
    }
}
