using System;

namespace AudioSensei.Bass.Native
{
    [Flags]
    internal enum StreamFlags : uint
    {
        None = 0x0,

        Sample8Bits = 0x1,
        SampleFloat = 0x100,
        SampleMono = 0x2,
        SampleLoop = 0x4,

        SampleSoftware = 0x10,
        SampleFx = 0x80,
        SampleVam = 0x40,

        Sample3D = 0x8,
        SampleMuteMax = 0x20,

        StreamPrescan = 0x20000,

        StreamRestrate = 0x80000,
        StreamBlock = 0x100000,
        StreamStatus = 0x800000,

        StreamAutoFree = 0x40000,
        StreamDecode = 0x200000,

        MusicRamp = 0x200,
        MusicRamps = 0x400,
        MusicSurround = 0x800,
        MusicSurround2 = 0x1000,
        MusicNonInter = 0x10000,
        MusicFt2Mod = 0x2000,
        MusicPt1Mod = 0x4000,
        MusicStopback = 0x80000,

        SpeakerFront = 0x1000000,
        SpeakerRear = 0x2000000,
        SpeakerCenlfe = 0x3000000,
        SpeakerRear2 = 0x4000000,

        SpeakerFrontLeft = SpeakerFront | 0x10000000,
        SpeakerFrontRight = SpeakerFront | 0x20000000,
        SpeakerRearLeft = SpeakerRear | 0x10000000,
        SpeakerRearRight = SpeakerRear | 0x20000000,
        SpeakerCenter = SpeakerCenlfe | 0x10000000,
        SpeakerLfe = SpeakerCenlfe | 0x20000000,
        SpeakerRear2Left = SpeakerRear2 | 0x10000000,
        SpeakerRear2Right = SpeakerRear2 | 0x20000000,

        AsyncFile = 0x40000000,
        Unicode = 0x80000000
    }
}
