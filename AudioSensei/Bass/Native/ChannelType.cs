namespace AudioSensei.Bass.Native
{
    internal enum ChannelType : uint
    {
        Sample = 0x1,
        Record = 0x2,
        Stream = 0x10000,
        StreamVorbis = 0x10002,
        StreamOgg = 0x10002,
        StreamMp1 = 0x10003,
        StreamMp2 = 0x10004,
        StreamMp3 = 0x10005,
        StreamAiff = 0x10006,
        StreamCa = 0x10007,
        StreamMf = 0x10008,
        StreamAm = 0x10009,
        StreamDummy = 0x18000,
        StreamDevice = 0x18001,
        StreamWav = 0x40000, // WAVE flag, LOWORD=codec
        StreamWavPcm = 0x50001,
        StreamWavFloat = 0x50003,
        MusicMod = 0x20000,
        MusicMtm = 0x20001,
        MusicS3M = 0x20002,
        MusicXm = 0x20003,
        MusicIt = 0x20004,
        MusicMo3 = 0x00100 // MO3 flag
    }
}
