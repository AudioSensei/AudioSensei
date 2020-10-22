namespace AudioSensei.Bass.BassCd
{
    internal enum RwFlags : uint
    {
        ReadCdR = 1,
        ReadCdRw = 2,
        ReadCdRw2 = 4,
        ReadDvd = 8,
        ReadDvdR = 16,
        ReadDvdRam = 32,
        ReadAnalog = 0x10000,
        ReadM2F1 = 0x100000,
        ReadM2F2 = 0x200000,
        ReadMulti = 0x400000,
        ReadCdDa = 0x1000000,
        ReadCdDasIa = 0x2000000,
        ReadSubChan = 0x4000000,
        ReadSubChanDi = 0x8000000,
        ReadC2 = 0x10000000,
        ReadIsrc = 0x20000000,
        ReadUpc = 0x40000000
    }
}
