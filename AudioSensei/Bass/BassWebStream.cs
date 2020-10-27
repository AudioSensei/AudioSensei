using System;
using AudioSensei.Bass.Native;
using JetBrains.Annotations;

namespace AudioSensei.Bass
{
    internal class BassWebStream : BassStream, IWebStream
    {
        public Uri Uri { get; }

        internal BassWebStream([NotNull] Uri link, [CanBeNull] string[] headers = null) :
            base(BassNative.Singleton.CreateStreamFromUrl(link.IsAbsoluteUri ? link.AbsoluteUri : Uri.EscapeUriString(link.ToString()), headers))
        {
            Uri = link;
        }

        internal BassWebStream([NotNull] string link, [CanBeNull] string[] headers = null) : this(new Uri(link), headers)
        {
        }
    }
}
