namespace AudioSensei.Bass
{
    public enum BassError
    {
        /// <summary>
        /// All is OK
        /// </summary>
        Ok = 0,
        /// <summary>
        /// Memory error
        /// </summary>
        Memory = 1,
        /// <summary>
        /// Can't open the file
        /// </summary>
        FileOpen = 2,
        /// <summary>
        /// Can't find a free/valid driver
        /// </summary>
        Driver = 3,
        /// <summary>
        /// The sample buffer was lost
        /// </summary>
        BufferLost = 4,
        /// <summary>
        /// Invalid handle
        /// </summary>
        Handle = 5,
        /// <summary>
        /// Unsupported sample format
        /// </summary>
        SampleFormat = 6,
        /// <summary>
        /// Invalid position
        /// </summary>
        Position = 7,
        /// <summary>
        /// BASS_Init has not been successfully called
        /// </summary>
        Init = 8,
        /// <summary>
        /// BASS_Start has not been successfully called
        /// </summary>
        Start = 9,
        /// <summary>
        /// SSL/HTTPS support isn't available
        /// </summary>
        Ssl = 10,
        /// <summary>
        /// Already initialized/paused/whatever
        /// </summary>
        Already = 14,
        /// <summary>
        /// File does not contain audio
        /// </summary>
        NotAudio = 17,
        /// <summary>
        /// Can't get a free channel
        /// </summary>
        NoChannel = 18,
        /// <summary>
        /// An illegal type was specified
        /// </summary>
        IllegalType = 19,
        /// <summary>
        /// An illegal parameter was specified
        /// </summary>
        IllegalParam = 20,
        /// <summary>
        /// No 3D support
        /// </summary>
        No3D = 21,
        /// <summary>
        /// No EAX support
        /// </summary>
        NoEax = 22,
        /// <summary>
        /// Illegal device number
        /// </summary>
        Device = 23,
        /// <summary>
        /// Not playing
        /// </summary>
        NoPlay = 24,
        /// <summary>
        /// Illegal sample rate
        /// </summary>
        Frequency = 25,
        /// <summary>
        /// The stream is not a file stream
        /// </summary>
        NotFile = 27,
        /// <summary>
        /// No hardware voices available
        /// </summary>
        NoHw = 29,
        /// <summary>
        /// The MOD music has no sequence data
        /// </summary>
        Empty = 31,
        /// <summary>
        /// No internet connection could be opened
        /// </summary>
        NoNet = 32,
        /// <summary>
        /// Couldn't create the file
        /// </summary>
        Create = 33,
        /// <summary>
        /// Effects are not available
        /// </summary>
        NoFx = 34,
        /// <summary>
        /// Requested data/action is not available
        /// </summary>
        NotAvailable = 37,
        /// <summary>
        /// The channel is/isn't a "decoding channel"
        /// </summary>
        Decode = 38,
        /// <summary>
        /// A sufficient DirectX version is not installed
        /// </summary>
        Dx = 39,
        /// <summary>
        /// Connection timedout
        /// </summary>
        Timeout = 40,
        /// <summary>
        /// Unsupported file format
        /// </summary>
        FileForm = 41,
        /// <summary>
        /// Unavailable speaker
        /// </summary>
        Speaker = 42,
        /// <summary>
        /// Invalid BASS version (used by add-ons)
        /// </summary>
        Version = 43,
        /// <summary>
        /// Codec is not available/supported
        /// </summary>
        Codec = 44,
        /// <summary>
        /// The channel/file has ended
        /// </summary>
        Ended = 45,
        /// <summary>
        /// The device is busy
        /// </summary>
        Busy = 46,
        /// <summary>
        /// Unstreamable file
        /// </summary>
        Unstreamable = 47,
        /// <summary>
        /// Some other mystery problem
        /// </summary>
        Unknown = -1
    }
}
