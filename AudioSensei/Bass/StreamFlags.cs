using System;

namespace AudioSensei.Bass
{
    [Flags]
    internal enum StreamFlags : uint
    {
        SampleFloat = 0x100,
        SampleMono = 0x2,
        SampleSoftware = 0x10,
        Sample3D = 0x8,
        SampleLoop = 0x4,
        SampleFx = 0x80,
        StreamPrescan = 0x20000,
        StreamAutofree = 0x40000,
        StreamDecode = 0x200000,

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
