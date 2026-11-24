using System;

namespace RootAccess.Hacking
{
    public enum PortScanResult
    {
        Ignored,
        Hit,
        Complete,
        Miss,
        Failed
    }

    /// <summary>
    /// Timing puzzle: a cursor ping-pongs along [0,1]; pressing while it is inside the open port window scores a hit.
    /// Each hit moves the window and speeds the cursor up.
    /// </summary>
    public sealed class PortScanModel
    {
        private readonly Random _random;
        private float _direction = 1f;

        public PortScanModel(int hitsRequired, int allowedMisses, float speed, float windowWidth, int seed)
        {
            if (hitsRequired < 1) throw new ArgumentOutOfRangeException(nameof(hitsRequired));
            HitsRequired = hitsRequired;
            AllowedMisses = Math.Max(0, allowedMisses);
            Speed = speed;
            WindowWidth = Math.Clamp(windowWidth, 0.05f, 0.9f);
            _random = new Random(seed);
            MoveWindow();
        }

        public int HitsRequired { get; }
        public int AllowedMisses { get; }
        public float Speed { get; private set; }
        public float WindowWidth { get; }
        public float Cursor { get; private set; }
        public float WindowStart { get; private set; }
        public float WindowEnd => WindowStart + WindowWidth;
        public int Hits { get; private set; }
        public int Misses { get; private set; }
        public bool IsComplete => Hits >= HitsRequired;
        public bool IsFailed => Misses > AllowedMisses;
        public bool CursorInWindow => Cursor >= WindowStart && Cursor <= WindowEnd;

        public void Tick(float deltaTime)
        {
            if (IsComplete || IsFailed) return;
            Cursor += _direction * Speed * deltaTime;
            if (Cursor >= 1f)
            {
                Cursor = 2f - Cursor;
                _direction = -1f;
            }
            else if (Cursor <= 0f)
            {
                Cursor = -Cursor;
                _direction = 1f;
            }
        }

        /// <summary>Test hook: place the cursor directly.</summary>
        public void SetCursor(float value) => Cursor = Math.Clamp(value, 0f, 1f);

        public PortScanResult Submit()
        {
            if (IsComplete || IsFailed) return PortScanResult.Ignored;
            if (!CursorInWindow)
            {
                Misses++;
                return IsFailed ? PortScanResult.Failed : PortScanResult.Miss;
            }

            Hits++;
            if (IsComplete) return PortScanResult.Complete;
            Speed *= 1.15f;
            MoveWindow();
            return PortScanResult.Hit;
        }

        private void MoveWindow() => WindowStart = (float)_random.NextDouble() * (1f - WindowWidth);
    }
}
