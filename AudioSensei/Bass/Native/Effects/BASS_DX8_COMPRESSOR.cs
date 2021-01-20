namespace AudioSensei.Bass.Native.Effects
{
    internal struct BASS_DX8_COMPRESSOR : IEffect
    {
        public float fGain;
        public float fAttack;
        public float fRelease;
        public float fThreshold;
        public float fRatio;
        public float fPredelay;
    }
}
