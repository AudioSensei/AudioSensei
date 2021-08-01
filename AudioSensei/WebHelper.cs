using System;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace AudioSensei
{
    internal static class WebHelper
    {
        [NotNull]
        public static readonly string UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:90.0) Gecko/20100101 Firefox/90.0";

        static WebHelper()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                UserAgent = @$"Mozilla/5.0 (Windows NT {Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor}; Win{(Environment.Is64BitOperatingSystem ? "64" : "32")}; {(Environment.Is64BitProcess ? "x64" : "x86")}; rv:90.0) Gecko/20100101 Firefox/90.0";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                UserAgent = @$"Mozilla/5.0 (X11; Ubuntu; Linux {(Environment.Is64BitProcess ? "x86_64" : "x86")}; rv:90.0) Gecko/20100101 Firefox/90.0";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                UserAgent = @$"Mozilla/5.0 (Macintosh; Intel Mac OS X {ConvertDarwinVersionToMacOsVersion($"{Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor}")}; rv:90.0) Gecko/20100101 Firefox/90.0";
            }
        }

        [NotNull]
        public static HttpClient CreateHttpClient()
        {
            var c = new HttpClient(new SocketsHttpHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.All,
                PooledConnectionLifetime = TimeSpan.FromMinutes(15)
            });
            c.DefaultRequestHeaders.UserAgent.Clear();
            c.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            return c;
        }

        private static string ConvertDarwinVersionToMacOsVersion(string darwinVersion)
        {
            return darwinVersion switch
            {
                "5.1" => "10.1",
                "5.5" => "10.1",
                "6.0" => "10.2",
                "6.8" => "10.2",
                "7.0" => "10.3",
                "7.9" => "10.4",
                "8.0" => "10.4",
                "8.11" => "10.4",
                "9.0" => "10.5",
                "9.8" => "10.5",
                "10.0" => "10.6",
                "10.8" => "10.6",
                "11.0" => "10.7",
                "11.4" => "10.8",
                "12.6" => "10.8",
                "13.0" => "10.9",
                "13.4" => "10.9",
                "14.0" => "10.10",
                "14.5" => "10.10",
                "15.0" => "10.11",
                "15.6" => "10.11",
                "16.0" => "10.12",
                "16.5" => "10.12",
                "16.6" => "10.12",
                "17.0" => "10.13",
                "17.1" => "10.13",
                "17.2" => "10.13",
                "17.3" => "10.13",
                "17.4" => "10.13",
                "17.5" => "10.13",
                "17.6" => "10.13",
                "17.7" => "10.13",
                "18.0" => "10.14",
                "18.1" => "10.14",
                "18.2" => "10.14",
                "19.0" => "10.15",
                "19.1" => "10.15",
                "19.2" => "10.15",
                "19.3" => "10.15",
                "19.4" => "10.15",
                "19.5" => "10.15",
                "19.6" => "10.15",
                "20.0" => "11.0",
                "20.1" => "11.0",
                "20.2" => "11.1",
                "20.3" => "11.2",
                "20.4" => "11.3",
                "20.5" => "11.4",
                "20.6" => "11.5",
                "21.0" => "12.0",
                _ => "12.0"
            };
        }
    }
}
