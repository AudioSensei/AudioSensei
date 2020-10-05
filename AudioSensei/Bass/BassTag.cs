using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSensei.Bass
{
    internal enum BassTag : uint
    {
        Id3 = 0, // ID3v1 tags : TAG_ID3 structure
        Id3V2 = 1, // ID3v2 tags : variable length block
        Ogg = 2, // OGG comments : series of null-terminated UTF-8 strings
        Http = 3, // HTTP headers : series of null-terminated ANSI strings
        Icy = 4, // ICY headers : series of null-terminated ANSI strings
        Meta = 5, // ICY metadata : ANSI string
        Ape = 6, // APE tags : series of null-terminated UTF-8 strings
        Mp4 = 7, // MP4/iTunes metadata : series of null-terminated UTF-8 strings
        Wma = 8, // WMA tags : series of null-terminated UTF-8 strings
        Vendor = 9, // OGG encoder : UTF-8 string
        Lyrics3 = 10, // Lyric3v2 tag : ASCII string
        CaCodec = 11, // CoreAudio codec info : TAG_CA_CODEC structure
        Mf = 13, // Media Foundation tags : series of null-terminated UTF-8 strings
        WaveFormat = 14, // WAVE format : WAVEFORMATEEX structure
        AmMime = 15, // Android Media MIME type : ASCII string
        AmName = 16, // Android Media codec name : ASCII string
        RiffInfo = 0x100, // RIFF "INFO" tags : series of null-terminated ANSI strings
        RiffBext = 0x101, // RIFF/BWF "bext" tags : TAG_BEXT structure
        RiffCart = 0x102, // RIFF/BWF "cart" tags : TAG_CART structure
        RiffDisp = 0x103, // RIFF "DISP" text tag : ANSI string
        RiffCue = 0x104, // RIFF "cue " chunk : TAG_CUE structure
        RiffSmpl = 0x105, // RIFF "smpl" chunk : TAG_SMPL structure
        ApeBinary = 0x1000, // + index #, binary APE tag : TAG_APE_BINARY structure
        MusicName = 0x10000, // MOD music name : ANSI string
        MusicMessage = 0x10001, // MOD message : ANSI string
        MusicOrders = 0x10002, // MOD order list : BYTE array of pattern numbers
        MusicAuth = 0x10003, // MOD author : UTF-8 string
        MusicInst = 0x10100, // + instrument #, MOD instrument name : ANSI string
        MusicSample = 0x10300 // + sample #, MOD sample name : ANSI string
    }
}
