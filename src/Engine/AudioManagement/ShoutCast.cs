using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MiniEngine.AudioManagement
{
    public sealed class ShoutCast : IDisposable
    {
        public class Station
        {
            public long ID { get; set; }
            public string Name { get; set; }
            public string Format { get; set; }
            public int Bitrate { get; set; }
            public string Genre { get; set; }
            public string CurrentTrack { get; set; }
            public int Listeners { get; set; }
            public bool IsRadionomy { get; set; }
            public string IceUrl { get; set; }
            public string StreamUrl { get; set; }
            public int AACEnabled { get; set; }
            public bool IsPlaying { get; set; }
            public bool IsAACEnabled { get; set; }        
        }

        private readonly HttpClient client;
        private readonly HttpClientHandler handler;
        private readonly CookieContainer cookieContainer;
        private readonly string userAgent = "Mozilla/5.0 (X11; Linux x86_64; rv:143.0) Gecko/20100101 Firefox/143.0";
        private readonly string cookiePath = "ShoutcastCookie.bin";
        private string cookie;

        public ShoutCast()
        {
            cookieContainer = new CookieContainer();
            handler = new HttpClientHandler();
            
            handler.CookieContainer = cookieContainer;
            handler.UseCookies = true;
            handler.AllowAutoRedirect = true;
            client = new HttpClient(handler);

            // Technically a cookie is not required to make requests and get a valid response
            // In this implementation we do however pretend to be somewhat legit
            GetCookieFromFile();
        }

        public void Dispose()
        {
            client?.Dispose();
            handler?.Dispose();
        }

        public async Task<string> GetStationUrl(Station station)
        {
            if(station == null)
                return null;
            return await GetStationUrl(station.ID);
        }

        public async Task<string> GetStationUrl(long id)
        {
            string url = "https://directory.shoutcast.com/Player/GetStreamUrl";

            using(var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                SetHeadersForPostRequest(request);

                Dictionary<string,string> formContent = new Dictionary<string, string>();
                formContent["station"] = id.ToString();

                using (FormUrlEncodedContent formData = new FormUrlEncodedContent(formContent))
                {
                    request.Content = formData;

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            string result = await response.Content.ReadAsStringAsync();
                            result = result.Replace("\"", "");
                            return result;
                        }
                        else
                        {
                            Console.WriteLine("Error: " + response.StatusCode);
                        }
                    }
                }
            }

            return string.Empty;
        }

        public async Task<string> GetTopStations()
        {
            string url =  "https://directory.shoutcast.com/Home/Top";

            using(var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                    SetHeadersForPostRequest(request);

                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        Console.WriteLine("Error: " + response.StatusCode);
                    }
                }
            }

            return string.Empty;
        }

        public async Task<string> GetStationsByGenre(string genre)
        {
            string url =  "https://directory.shoutcast.com/Home/BrowseByGenre";

            using(var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                SetHeadersForPostRequest(request);

                Dictionary<string,string> formContent = new Dictionary<string, string>();
                formContent["genrename"] = genre;

                using (FormUrlEncodedContent formData = new FormUrlEncodedContent(formContent))
                {
                    request.Content = formData;

                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            return await response.Content.ReadAsStringAsync();
                        }
                        else
                        {
                            Console.WriteLine("Error: " + response.StatusCode);
                        }
                    }
                }
            }

            return string.Empty;
        }

        private void GetCookieFromFile()
        {
            if(File.Exists(cookiePath))
            {
                if(!LoadCookie())
                {
                    _ = Task.Run(async () =>
                    {
                        bool result = await GetCookie();
                    }); 
                }
            }
            else
            {
                _ = Task.Run(async () =>
                {
                    bool result = await GetCookie();
                });                
            }            
        }

        private async Task<bool> GetCookie()
        {
            string url = "https://directory.shoutcast.com";

            using(var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
                request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                request.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.5");
                request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br, zstd");
                request.Headers.TryAddWithoutValidation("Sec-GPC", "1");
                request.Headers.TryAddWithoutValidation("Connection", "keep-alive");
                request.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
                request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
                request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
                request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "none");
                request.Headers.TryAddWithoutValidation("Sec-Fetch-User", "?1");
                request.Headers.TryAddWithoutValidation("Priority", "u=0, i");

                try
                {
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            Uri uri = new Uri(url);
                            CookieCollection cookies = cookieContainer.GetCookies(uri);

                            if (cookies.Count > 0)
                            {
                                SaveCookie(cookies[0]);
                                return true;
                            }
                        }
                    }
                    
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private void SetHeadersForPostRequest(HttpRequestMessage request)
        {
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
            request.Headers.TryAddWithoutValidation("Accept", "*/*");
            request.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.5");
            request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br, zstd");
            request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
            request.Headers.TryAddWithoutValidation("Origin", " https://directory.shoutcast.com");
            request.Headers.TryAddWithoutValidation("Sec-GPC", "1");
            request.Headers.TryAddWithoutValidation("Connection", "keep-alive");
            request.Headers.TryAddWithoutValidation("Referer", "https://directory.shoutcast.com/");
            request.Headers.TryAddWithoutValidation("Cookie",  cookie);
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "empty");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "cors");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "same-origin");
            request.Headers.TryAddWithoutValidation("Priority", "u=0");
        }

        private unsafe void SaveCookie(Cookie c)
        {
            cookie = c.Name + "=" + c.Value;

            DateTimeOffset offset = new DateTimeOffset(c.Expires);
            long expires = offset.ToUnixTimeSeconds();
            int cookieLength = Encoding.UTF8.GetByteCount(cookie);
            byte[] cookieData = new byte[8 + 4 + cookieLength];

            fixed(byte *pCookieData = &cookieData[0])
            {
                long *pExpires = (long*)pCookieData;
                *pExpires = expires;
                int *pLength = (int*)&pCookieData[8];
                *pLength = cookieLength;
                Encoding.UTF8.GetBytes(cookie, 0, cookie.Length, cookieData, 12);
                File.WriteAllBytes(cookiePath, cookieData);
            }
        }

        private unsafe bool LoadCookie()
        {
            if(!File.Exists(cookiePath))
            {
                Console.WriteLine("File does not exist: " + cookiePath);
                return false;
            }
            
            byte[] cookieData = File.ReadAllBytes(cookiePath);

            if(cookieData?.Length <= 0)
            {
                Console.WriteLine("Invalid cookie data");
                return false;
            }

            int payloadLength = cookieData.Length - 12;
            
            fixed(byte *pCookieData = &cookieData[0])
            {
                long expires = *(long*)pCookieData;
                int length = *(int*)&pCookieData[8];
                
                if(length == 0 || length > payloadLength)
                {
                    Console.WriteLine("Payload too big");
                    return false;
                }

                cookie = Encoding.UTF8.GetString(cookieData, 12, length);
            }

            return true;
        }
    }
}