namespace AudioSensei.Bass.Native.Effects
{
    internal struct BASS_DX8_ECHO : IEffect
    {
        public float fWetDryMix;
        public float fFeedback;
        public float fLeftDelay;
        public float fRightDelay;
        public bool lPanDelay;
    }
}
