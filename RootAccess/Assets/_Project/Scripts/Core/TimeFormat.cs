using UnityEngine;

namespace RootAccess.Core
{
    /// <summary>Clock-style durations for the UI: "4:07" under an hour, "1:02:45" above.</summary>
    public static class TimeFormat
    {
        public static string Clock(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int hours = total / 3600;
            int minutes = total / 60 % 60;
            int secs = total % 60;
            return hours > 0 ? $"{hours}:{minutes:00}:{secs:00}" : $"{minutes}:{secs:00}";
        }
    }
}
