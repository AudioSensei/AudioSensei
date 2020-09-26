using System;

namespace AudioSensei.Bass
{
    public class BassException : Exception
    {
        public uint ErrorCode { get; }

        public BassException(string message) : this(message, BassNative.GetLastErrorCode()) { }

        public BassException(string message, uint errorCode) : base($"{message} (Error: {errorCode})")
        {
            ErrorCode = errorCode;
        }
    }
}
