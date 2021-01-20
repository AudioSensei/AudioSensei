namespace AudioSensei.Bass.Native.Effects
{
    internal struct BASS_DX8_I3DL2REVERB : IEffect
    {
        public int lRoom;
        public int lRoomHF;
        public float flRoomRolloffFactor;
        public float flDecayTime;
        public float flDecayHFRatio;
        public int lReflections;
        public float flReflectionsDelay;
        public int lReverb;
        public float flReverbDelay;
        public float flDiffusion;
        public float flDensity;
        public float flHFReference;
    }
}
