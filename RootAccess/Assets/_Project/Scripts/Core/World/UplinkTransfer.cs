using UnityEngine;

namespace RootAccess.Core
{
    /// <summary>
    /// The broadcast at the top of the Halden antenna (Level 6), as plain state. Once started it fills from 0 to 1 over
    /// <see cref="Seconds"/>, but only while the player stays at the transmitter: stepping away pauses it, it never
    /// resets. Defender waves are due at even fractions of the progress (the first one as soon as it starts).
    /// </summary>
    public sealed class UplinkTransfer
    {
        public UplinkTransfer(float seconds, int waves)
        {
            Seconds = Mathf.Max(0.1f, seconds);
            Waves = Mathf.Max(0, waves);
        }

        public float Seconds { get; }
        public int Waves { get; }
        public float Progress { get; private set; }
        public bool Started { get; private set; }
        public bool Completed => Progress >= 1f;
        public bool Running => Started && !Completed;
        public int Percent => Mathf.FloorToInt(Progress * 100f + 0.0001f);

        /// <summary>How many defender waves should be out by now: one at the start, then one per 1/<see cref="Waves"/>.</summary>
        public int WavesDue => !Started || Waves == 0 ? 0 : Mathf.Min(Waves, 1 + Mathf.FloorToInt(Progress * Waves));

        /// <summary>Starts the transfer; false if it was already started.</summary>
        public bool Start()
        {
            if (Started) return false;
            Started = true;
            return true;
        }

        /// <summary>Advances while <paramref name="present"/>; returns true only on the tick that completes the transfer.</summary>
        public bool Tick(float deltaTime, bool present)
        {
            if (!Running || !present || deltaTime <= 0f) return false;
            Progress = Mathf.Min(1f, Progress + deltaTime / Seconds);
            return Completed;
        }
    }
}
