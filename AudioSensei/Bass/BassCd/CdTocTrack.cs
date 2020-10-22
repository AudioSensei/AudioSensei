using System;
using System.Runtime.InteropServices;

namespace AudioSensei.Bass.BassCd
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct CdTocTrack
    {
        byte res1;
        byte adrcon;    // ADR + control
        public byte track;     // track number
        byte res2;
        byte hours;
        byte minutes;
        byte seconds;
        byte frames;

        public TimeSpan StartTme => new TimeSpan(0, hours, minutes, seconds, (int)TimeSpanUtils.FromCdPosition(frames).TotalMilliseconds);
    }
}
