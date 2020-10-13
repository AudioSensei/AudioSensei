using System.Runtime.InteropServices;

namespace AudioSensei.Bass.Native
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct BassInfo
    {
        public readonly uint flags;    // device capabilities (DSCAPS_xxx flags)
        public readonly uint hwsize;   // size of total device hardware memory
        public readonly uint hwfree;   // size of free device hardware memory
        public readonly uint freesam;  // number of free sample slots in the hardware
        public readonly uint free3d;   // number of free 3D sample slots in the hardware
        public readonly uint minrate;  // min sample rate supported by the hardware
        public readonly uint maxrate;  // max sample rate supported by the hardware
        public readonly bool eax;       // device supports EAX? (always FALSE if BASS_DEVICE_3D was not used)
        public readonly uint minbuf;   // recommended minimum buffer length in ms (requires BASS_DEVICE_LATENCY)
        public readonly uint dsver;    // DirectSound version
        public readonly uint latency;  // delay (in ms) before start of playback (requires BASS_DEVICE_LATENCY)
        public readonly BassInitFlags initflags; // BASS_Init "flags" parameter
        public readonly uint speakers; // number of speakers available
        public readonly uint freq;		// current output rate
    }
}
