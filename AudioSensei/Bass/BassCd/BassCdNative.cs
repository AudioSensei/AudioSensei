using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using AudioSensei.Bass.Native;
using AudioSensei.Bass.Native.Handles;
using Serilog;

namespace AudioSensei.Bass.BassCd
{
    internal static class BassCdNative
    {
        private const string BassCd = "basscd";

        public static bool TryGetUpc(DriveHandle drive, out string upc)
        {
            upc = Marshal.PtrToStringUTF8(BASS_CD_GetID(drive, BassCdId.Upc));
            if (upc == null)
            {
                Log.Information($"Getting Upc for drive {drive} failed due to {BassNative.GetLastErrorCode()}");
                return false;
            }

            if (upc.All(c => c == '0'))
            {
                Log.Information($"Rejecting invalid Upc for drive {drive}: {upc}");
                return false;
            }

            return true;
        }

        public static bool TryGetCddb(DriveHandle drive, out string[] cddb)
        {
            var temp = Marshal.PtrToStringUTF8(BASS_CD_GetID(drive, BassCdId.Cddb));
            if (temp == null)
            {
                Log.Information($"Getting Cddb for drive {drive} failed due to {BassNative.GetLastErrorCode()}");
                cddb = null;
                return false;
            }

            cddb = temp.Split(' ');
            return true;
        }

        public static bool TryGetCddb2(DriveHandle drive, out string[] cddb5)
        {
            var temp = Marshal.PtrToStringUTF8(BASS_CD_GetID(drive, BassCdId.Cddb2));
            if (temp == null)
            {
                Log.Information($"Getting Cddb2 for drive {drive} failed due to {BassNative.GetLastErrorCode()}");
                cddb5 = null;
                return false;
            }

            cddb5 = temp.Split(' ');
            return true;
        }

        public static bool TryGetMusicbrainz(DriveHandle drive, out string musicbrainz)
        {
            musicbrainz = Marshal.PtrToStringUTF8(BASS_CD_GetID(drive, BassCdId.Musicbrainz));
            if (musicbrainz == null)
            {
                Log.Information($"Getting Musicbrainz for drive {drive} failed due to {BassNative.GetLastErrorCode()}");
                return false;
            }

            return true;
        }

        public static bool TryGetIsrc(DriveHandle drive, uint track, out string isrc)
        {
            isrc = Marshal.PtrToStringUTF8(BASS_CD_GetID(drive, BassCdId.Isrc + track));
            if (isrc == null)
            {
                Log.Information($"Getting Isrc for drive {drive} failed due to {BassNative.GetLastErrorCode()}");
                return false;
            }

            return true;
        }

        public static bool TryGetCdPlayer(DriveHandle drive, out Dictionary<string, string> cdplayer)
        {
            // todo: maybe we should allow additional locations for it?
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cdplayer.ini");
            if (!File.Exists(path))
            {
                Log.Information($"Getting CdPlayer for drive {drive} failed due to {path} not existing");
                cdplayer = null;
                return false;
            }
            var cd = Marshal.PtrToStringUTF8(BASS_CD_GetID(drive, BassCdId.CdPlayer));
            if (string.IsNullOrEmpty(cd))
            {
                Log.Information($"Getting CdPlayer for drive {drive} failed due to {BassNative.GetLastErrorCode()}");
                cdplayer = null;
                return false;
            }
            // todo: read cdplayer.ini
            cdplayer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return true;
        }

        public static unsafe bool TryGetCdText(DriveHandle drive, out Dictionary<string, string> cdtext)
        {
            var cd = BASS_CD_GetIDArray(drive, BassCdId.Text);
            if (cd == IntPtr.Zero)
            {
                Log.Information($"Getting CdText for drive {drive} failed due to {BassNative.GetLastErrorCode()}");
                cdtext = null;
                return false;
            }

            try
            {
                cdtext = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var tag in UnmanagedUtils.GetDoubleTerminatedStringArray((byte*)cd.ToPointer()))
                {
                    var index = tag.IndexOf('=');
                    if (index == -1)
                    {
                        continue;
                    }

                    cdtext[tag.Substring(0, index)] = tag.Substring(index + 1, tag.Length - index - 1);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Information(ex, $"Getting CdText for drive {drive} failed");
                cdtext = null;
                return false;
            }
        }

        [DllImport(BassCd)]
        private static extern bool BASS_CD_GetInfo(DriveHandle drive, out BassCdInfo info);

        [DllImport(BassCd)]
        private static extern bool BASS_CD_Release(DriveHandle drive);

        [DllImport(BassCd)]
        private static extern bool BASS_CD_IsReady(DriveHandle drive);

        [DllImport(BassCd)]
        private static extern IntPtr BASS_CD_GetID(DriveHandle drive, BassCdId id);

        [DllImport(BassCd, EntryPoint = "BASS_CD_GetID")]
        private static extern IntPtr BASS_CD_GetIDArray(DriveHandle drive, BassCdId id);

        [DllImport(BassCd)]
        private static extern bool BASS_CD_GetTOC(DriveHandle drive, uint mode, out CdToc toc);

        [DllImport(BassCd)]
        private static extern uint BASS_CD_GetTracks(DriveHandle drive);

        [DllImport(BassCd)]
        private static extern uint BASS_CD_GetTrackLength(DriveHandle drive, uint track);

        [DllImport(BassCd)]
        private static extern StreamHandle BASS_CD_StreamCreate(DriveHandle drive, uint track, StreamFlags flags);

        #region Analog
        [DllImport(BassCd)]
        private static extern uint BASS_CD_Analog_GetPosition(DriveHandle drive);

        [DllImport(BassCd)]
        private static extern ChannelStatus BASS_CD_Analog_IsActive(DriveHandle drive);

        [DllImport(BassCd)]
        private static extern bool BASS_CD_Analog_Play(DriveHandle drive, uint track, uint pos);

        [DllImport(BassCd)]
        private static extern uint BASS_CD_Analog_Stop(DriveHandle drive);
        #endregion
    }
}
