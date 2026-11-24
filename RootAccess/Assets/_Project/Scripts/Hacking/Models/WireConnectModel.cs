using System;

namespace RootAccess.Hacking
{
    public enum WireResult
    {
        Ignored,
        Selected,
        Connected,
        Complete,
        Mistake,
        Failed
    }

    /// <summary>Match each left socket to the right socket with the same colour. Limited mistakes allowed.</summary>
    public sealed class WireConnectModel
    {
        private readonly int[] _rightColors;
        private readonly bool[] _leftDone;
        private readonly bool[] _rightDone;
        private int _selectedLeft = -1;

        public WireConnectModel(int wireCount, int allowedMistakes, int seed)
        {
            if (wireCount < 2) throw new ArgumentOutOfRangeException(nameof(wireCount));
            WireCount = wireCount;
            AllowedMistakes = Math.Max(0, allowedMistakes);
            _rightColors = new int[wireCount];
            _leftDone = new bool[wireCount];
            _rightDone = new bool[wireCount];
            for (int i = 0; i < wireCount; i++) _rightColors[i] = i;

            var random = new Random(seed);
            for (int i = wireCount - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (_rightColors[i], _rightColors[j]) = (_rightColors[j], _rightColors[i]);
            }
        }

        public int WireCount { get; }
        public int AllowedMistakes { get; }
        public int Mistakes { get; private set; }
        public int Connected { get; private set; }
        public int SelectedLeft => _selectedLeft;
        public bool IsComplete => Connected == WireCount;
        public bool IsFailed => Mistakes > AllowedMistakes;

        /// <summary>Left socket i carries colour i.</summary>
        public int LeftColor(int index) => index;
        public int RightColor(int index) => _rightColors[index];
        public bool IsLeftDone(int index) => _leftDone[index];
        public bool IsRightDone(int index) => _rightDone[index];

        public WireResult SelectLeft(int index)
        {
            if (IsComplete || IsFailed || index < 0 || index >= WireCount || _leftDone[index]) return WireResult.Ignored;
            _selectedLeft = index;
            return WireResult.Selected;
        }

        public WireResult ConnectRight(int index)
        {
            if (IsComplete || IsFailed || _selectedLeft < 0 || index < 0 || index >= WireCount || _rightDone[index])
                return WireResult.Ignored;

            if (_rightColors[index] != LeftColor(_selectedLeft))
            {
                Mistakes++;
                _selectedLeft = -1;
                return IsFailed ? WireResult.Failed : WireResult.Mistake;
            }

            _leftDone[_selectedLeft] = true;
            _rightDone[index] = true;
            _selectedLeft = -1;
            Connected++;
            return IsComplete ? WireResult.Complete : WireResult.Connected;
        }
    }
}
