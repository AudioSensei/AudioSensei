using System;

namespace AudioSensei.Bass
{
    public class BassException : Exception
    {
        public BassError ErrorCode { get; }

        public BassException(string message) : this(message, BassNative.GetLastErrorCode()) { }

        public BassException(string message, BassError errorCode) : base($"{message} (Error: {errorCode})")
        {
            ErrorCode = errorCode;
        }
    }
}
