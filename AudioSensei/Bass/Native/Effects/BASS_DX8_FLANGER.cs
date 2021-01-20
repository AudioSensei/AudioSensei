namespace AudioSensei.Bass.Native.Effects
{
    internal struct BASS_DX8_FLANGER : IEffect
    {
        public float fWetDryMix;
        public float fDepth;
        public float fFeedback;
        public float fFrequency;
        public WaveForm lWaveform;
        public float fDelay;
        public ChannelPhase lPhase;
    }
}
