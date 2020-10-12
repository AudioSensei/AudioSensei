using System;

namespace AudioSensei.Bass.Native
{
    [Flags]
    internal enum BassInitFlags : uint
    {
        Device8Bits = 1, // 8 bit
        Device16Bits = 8, // limit output to 16 bit
        DeviceMono = 2, // mono
        DeviceStereo = 0x8000, // limit output to stereo
        Device3D = 4, // enable 3D functionality
        DeviceLatency = 0x100, // calculate device latency (BASS_INFO struct)
        DeviceCpSpeakers = 0x400,// detect speakers via Windows control panel
        DeviceSpeakers = 0x800, // force enabling of speaker assignment
        DeviceNoSpeaker = 0x1000, // ignore speaker arrangement
        DeviceFreq = 0x4000, // set device sample rate
        DeviceDSound = 0x40000,	// use DirectSound output
        DeviceAudioTrack = 0x20000, // use AudioTrack output
        DeviceDmix = 0x2000, // use ALSA "dmix" plugin
        DeviceHog = 0x10000, // hog/exclusive mode
    }
}
