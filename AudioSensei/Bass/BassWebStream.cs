using System;
using AudioSensei.Bass.Native;

namespace AudioSensei.Bass
{
    internal class BassWebStream : BassStream, IWebStream
    {
        public Uri Uri { get; }

        internal BassWebStream(Uri link, string[] headers = null) :
            base(BassNative.Singleton.CreateStreamFromUrl(link.IsAbsoluteUri ? link.AbsoluteUri : Uri.EscapeUriString(link.ToString()), headers))
        {
            Uri = link;
        }

        internal BassWebStream(string link, string[] headers = null) : this(new Uri(link), headers)
        {
        }
    }
}
