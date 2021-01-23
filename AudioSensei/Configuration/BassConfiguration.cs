namespace AudioSensei.Configuration
{
    public sealed class BassConfiguration
    {
        public int Device { get; set; } = -1;
        public uint Frequency { get; set; } = 44000;
        public bool Restrate { get; set; } = false;
    }
}
