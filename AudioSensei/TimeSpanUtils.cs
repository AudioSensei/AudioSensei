using System;
using JetBrains.Annotations;

namespace AudioSensei
{
    public static class TimeSpanUtils
    {
        public static uint ToCdPosition(this TimeSpan ts)
        {
            return (uint)(75u * ts.TotalSeconds);
        }

        public static TimeSpan FromCdPosition(uint position)
        {
            return TimeSpan.FromSeconds(position / 75d);
        }

        [NotNull]
        public static string ToPlaybackPosition(this TimeSpan ts)
        {
            return ts.TotalHours < 1 ? ts.ToString(@"mm\:ss") : ((int)Math.Floor(ts.TotalHours)) + ts.ToString(@"\:mm\:ss");
        }
    }
}
