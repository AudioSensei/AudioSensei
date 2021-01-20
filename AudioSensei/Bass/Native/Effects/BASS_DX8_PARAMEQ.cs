namespace AudioSensei.Bass.Native.Effects
{
    internal struct BASS_DX8_PARAMEQ : IEffect
    {
        /// <summary>
        /// Center frequency, in hertz.
        /// </summary>
        public float fCenter;

        /// <summary>
        /// Bandwidth, in semitones, in the range from 1 to 36. The default value is 12.
        /// </summary>
        /// 
        public float fBandwidth;

        /// <summary>
        /// Gain, in the range from -15 to 15. The default value is 0 dB.
        /// </summary>
        public float fGain;

        public BASS_DX8_PARAMEQ(float center = 80, float bandwidth = 12, float gain = 0)
        {
            fCenter = center;
            fBandwidth = bandwidth;
            fGain = gain;
        }
    }
}
