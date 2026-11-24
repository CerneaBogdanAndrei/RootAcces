using System;
using System.Collections.Generic;

namespace RootAccess.Hacking
{
    public enum PatternInputResult
    {
        Ignored,
        Correct,
        RoundComplete,
        PuzzleComplete,
        Wrong
    }

    /// <summary>
    /// "Simon" style memory puzzle. Each round shows a longer prefix of a fixed random sequence; the player must
    /// repeat it exactly. Deterministic for a given seed (testable).
    /// </summary>
    public sealed class PatternMemoryModel
    {
        private readonly int[] _sequence;
        private int _inputIndex;

        public PatternMemoryModel(int cellCount, int startLength, int rounds, int seed)
        {
            if (cellCount < 2) throw new ArgumentOutOfRangeException(nameof(cellCount));
            if (startLength < 1) throw new ArgumentOutOfRangeException(nameof(startLength));
            if (rounds < 1) throw new ArgumentOutOfRangeException(nameof(rounds));

            CellCount = cellCount;
            StartLength = startLength;
            Rounds = rounds;
            var random = new Random(seed);
            _sequence = new int[startLength + rounds - 1];
            for (int i = 0; i < _sequence.Length; i++)
            {
                int cell;
                do cell = random.Next(cellCount);
                while (i > 0 && cell == _sequence[i - 1]);
                _sequence[i] = cell;
            }
        }

        public int CellCount { get; }
        public int StartLength { get; }
        public int Rounds { get; }
        public int Round { get; private set; }
        public bool AwaitingInput { get; private set; }
        public bool IsComplete { get; private set; }
        public bool IsFailed { get; private set; }
        public int CurrentLength => StartLength + Math.Min(Round, Rounds - 1);
        public IReadOnlyList<int> Sequence => _sequence;

        /// <summary>The cells to flash for the current round, in order.</summary>
        public int GetShownCell(int step) => _sequence[step];

        public void BeginInput()
        {
            if (IsComplete || IsFailed) return;
            _inputIndex = 0;
            AwaitingInput = true;
        }

        public PatternInputResult Submit(int cell)
        {
            if (!AwaitingInput) return PatternInputResult.Ignored;
            if (cell != _sequence[_inputIndex])
            {
                AwaitingInput = false;
                IsFailed = true;
                return PatternInputResult.Wrong;
            }

            _inputIndex++;
            if (_inputIndex < CurrentLength) return PatternInputResult.Correct;

            AwaitingInput = false;
            Round++;
            if (Round < Rounds) return PatternInputResult.RoundComplete;
            IsComplete = true;
            return PatternInputResult.PuzzleComplete;
        }
    }
}
