namespace RootAccess.Core
{
    public readonly struct UnderwaterChanged : IEvent
    {
        public readonly bool IsUnderwater;
        public UnderwaterChanged(bool isUnderwater) => IsUnderwater = isUnderwater;
    }

    public enum MusicMood
    {
        Menu,
        Stealth,
        Alert,
        Silence
    }

    public readonly struct MusicMoodRequested : IEvent
    {
        public readonly MusicMood Mood;
        public MusicMoodRequested(MusicMood mood) => Mood = mood;
    }
}
