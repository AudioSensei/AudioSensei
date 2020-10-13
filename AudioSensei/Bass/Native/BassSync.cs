using System;

namespace AudioSensei.Bass.Native
{
    [Flags]
    internal enum BassSync : uint
    {
        Pos = 0,
        End = 2,
        Meta = 4,
        Slide = 5,
        Stall = 6,
        Download = 7,
        Free = 8,
        Setpos = 11,
        MusicPos = 10,
        MusicInst = 1,
        MusicFx = 3,
        OggChange = 12,
        DevFail = 14,
        DevFormat = 15,
        Thread = 0x20000000, // flag: call sync in other thread
        MixTime = 0x40000000, // flag: sync at mixtime, else at playtime
        OneTime = 0x80000000 // flag: sync only once, else continuously
    }
}
