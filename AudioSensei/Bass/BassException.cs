using System;
using AudioSensei.Bass.Native;
using JetBrains.Annotations;

namespace AudioSensei.Bass
{
    public class BassException : Exception
    {
        public BassError ErrorCode { get; }

        public BassException([CanBeNull] string message) : this(message, BassNative.GetLastErrorCode()) { }

        public BassException([CanBeNull] string message, BassError errorCode) : base($"{message} (Error: {errorCode})")
        {
            ErrorCode = errorCode;
        }
    }
}
