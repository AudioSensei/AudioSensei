using System;

namespace AudioSensei.Bass.BassCd
{
    internal enum BassCdId : uint
    {
        Upc = 1,
        Cddb = 2,
        Cddb2 = 3,
        Text = 4,
        CdPlayer = 5,
        Musicbrainz = 6,
        Isrc = 0x100, // + track #
        [Obsolete("Since FreeDB shutdown there is no available CDDB database", true)]
        CddbQuery = 0x200,
        [Obsolete("Since FreeDB shutdown there is no available CDDB database", true)]
        CddbRead = 0x201, // + entry #
        [Obsolete("Since FreeDB shutdown there is no available CDDB database", true)]
        CddbReadCache = 0x2FF,
    }
}
