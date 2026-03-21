using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MiniEngine.Utilities
{
    public sealed class HttpClient
    {
        public enum Method
        {
            Get,
            Post    
        }

        public class ContentStream
        {
            private byte[] initialContent;
            private int initialContentLength;
            private int initialContentConsumed;
            private Stream networkStream;

            public ContentStream(byte[] initialContent, int initialContentLength, Stream networkStream)
            {
                this.networkStream = networkStream;

                if(initialContent == null || initialContentLength == 0)
                {
                    this.initialContent = null;
                    this.initialContentLength = 0;
                    initialContentConsumed = 0;
                }
                else
                {
                    this.initialContent = new byte[initialContentLength];
                    this.initialContentLength = initialContentLength;

                    Array.Copy(initialContent, this.initialContent, initialContentLength);
                    initialContentConsumed = 0;
                }
            }

            public int Read(byte[] buffer, int offset, int size)
            {
                if (networkStream == null)
                    return 0;

                if (buffer == null)
                    return 0;

                if (size == 0)
                    return 0;

                int totalRead = 0;
                if (initialContentConsumed < initialContentLength)
                {
                    int remaining = initialContentLength - initialContentConsumed;
                    int toConsume = (size < remaining) ? size : remaining;

                    Buffer.BlockCopy(initialContent, initialContentConsumed, buffer, offset, toConsume);
                    initialContentConsumed += toConsume;

                    totalRead += toConsume;
                    offset += toConsume;
                    size -= toConsume;
                }

                if (size > 0)
                {
                    totalRead += networkStream.Read(buffer, offset, size);
                }

                return totalRead;
            }

            public bool ReadAsString(out string str, int size)
            {
                str = string.Empty;

                if (size <= 0)
                    return false;

                byte[] buffer = new byte[4096];
                StringBuilder stringBuilder = new StringBuilder();
                Decoder decoder = Encoding.UTF8.GetDecoder();
                int remainingToRead = size;

                try
                {
                    while (remainingToRead > 0)
                    {
                        int toRead = (remainingToRead < buffer.Length) ? remainingToRead : buffer.Length;
                        int read = Read(buffer, 0, toRead);

                        if (read <= 0)
                        {
                            break;
                        }

                        remainingToRead -= read;

                        // Determine if this is the last chunk to flush the decoder
                        bool flush = (remainingToRead == 0);
                        int charCount = decoder.GetCharCount(buffer, 0, read, flush);
                        char[] chars = new char[charCount];
                        decoder.GetChars(buffer, 0, read, chars, 0, flush);

                        stringBuilder.Append(chars);
                    }

                    str = stringBuilder.ToString();
                    return str.Length > 0;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public class Request
        {
            public string url;
            public Method method;
            public Dictionary<string,string> headers;
            public List<string> cookies;
            public byte[] content;

            public Request(string url, Method method)
            {
                this.url = url;
                this.method = method;
                headers = new Dictionary<string, string>();
                cookies = new List<string>();
            }

            public void SetContent(string contents, string contentType)
            {
                content = Encoding.UTF8.GetBytes(contents);
                headers["Content-Type"] = contentType;
            }

            public void AddHeader(string key, string value)
            {
                headers[key] = value;
            }
        }

        public class Response
        {
            public int status;
            public Dictionary<string,string> headers;
            public List<string> cookies;
            public ContentStream content;
            public int contentLength;
            
            public Response()
            {
                headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                cookies = new List<string>();
            }
        }

        private Socket socket;
        private Stream networkStream;
        private StringBuilder requestBuilder;

        public HttpClient()
        {
            
        }

        public async Task<Response> Send(Request request)
        {
            if(request == null)
                return null;
            
            if(!TryParseURL(request.url, out string host, out string hostHeader, out string pathAndQuery, out int port, out bool isHttps))
                return null;

            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(host, port);
                
                NetworkStream baseStream = new NetworkStream(socket, true);
                Stream streamToUse = baseStream;

                if (isHttps)
                {
                    SslStream sslStream = new SslStream(baseStream, false);
                    await sslStream.AuthenticateAsClientAsync(host);
                    streamToUse = sslStream;
                }

                networkStream = streamToUse;

                requestBuilder = new StringBuilder();
                string verb = request.method.ToString().ToUpper();
                requestBuilder.Append($"{verb} {pathAndQuery} HTTP/1.1\r\n");
                requestBuilder.Append($"Host: {hostHeader}\r\n");
                requestBuilder.Append("Connection: close\r\n");

                foreach (KeyValuePair<string, string> header in request.headers)
                {
                    requestBuilder.Append($"{header.Key}: {header.Value}\r\n");
                }

                if (request.cookies?.Count > 0)
                {
                    requestBuilder.Append("Cookie: ");
                    requestBuilder.Append(string.Join("; ", request.cookies));
                    requestBuilder.Append("\r\n");
                }

                if (request.content != null)
                {
                    requestBuilder.Append($"Content-Length: {request.content.Length}\r\n");
                }

                requestBuilder.Append("\r\n");

                byte[] headerBytes = Encoding.UTF8.GetBytes(requestBuilder.ToString());
                await networkStream.WriteAsync(headerBytes, 0, headerBytes.Length);

                if (request.content != null)
                {
                    await networkStream.WriteAsync(request.content, 0, request.content.Length);
                }

                return await ParseResponse(networkStream);
            }
            catch (Exception)
            {
                if (socket != null)
                {
                    socket.Dispose();
                }
                return null;
            }
        }

        private async Task<Response> ParseResponse(Stream stream)
        {
            byte[] buffer = new byte[8192];
            int totalRead = 0;
            int headerEndIndex = -1;

            // Read until we find the end of the headers \r\n\r\n
            while (true)
            {
                int read = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead);
                if (read <= 0)
                {
                    return null;
                }

                totalRead += read;

                // Look for \r\n\r\n
                for (int i = 0; i <= totalRead - 4; i++)
                {
                    if (buffer[i] == '\r' && buffer[i + 1] == '\n' && buffer[i + 2] == '\r' && buffer[i + 3] == '\n')
                    {
                        headerEndIndex = i;
                        break;
                    }
                }

                if (headerEndIndex != -1)
                {
                    break;
                }

                if (totalRead == buffer.Length)
                {
                    Array.Resize(ref buffer, buffer.Length * 2);
                }
            }

            string headerString = Encoding.UTF8.GetString(buffer, 0, headerEndIndex);
            string[] lines = headerString.Split(new[] { "\r\n" }, StringSplitOptions.None);

            Response response = new Response();
            if (lines.Length > 0)
            {
                string[] statusParts = lines[0].Split(' ');
                if (statusParts.Length >= 2 && int.TryParse(statusParts[1], out int statusCode))
                {
                    response.status = statusCode;
                }
            }

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                int separatorIndex = line.IndexOf(':');
                if (separatorIndex != -1)
                {
                    string key = line.Substring(0, separatorIndex).Trim();
                    string value = line.Substring(separatorIndex + 1).Trim();

                    if (key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                    {
                        response.cookies.Add(value);
                    }
                    else
                    {
                        response.headers[key] = value;
                    }
                }
            }

            if (response.headers.TryGetValue("Content-Length", out string lengthStr) && int.TryParse(lengthStr, out int length))
            {
                response.contentLength = length;
            }

            // Calculate how much of the body was already read into the buffer
            int headerFullLength = headerEndIndex + 4;
            int extraBytesCount = totalRead - headerFullLength;
            byte[] extraBytes = null;

            if (extraBytesCount > 0)
            {
                extraBytes = new byte[extraBytesCount];
                Buffer.BlockCopy(buffer, headerFullLength, extraBytes, 0, extraBytesCount);
            }

            response.content = new ContentStream(extraBytes, extraBytesCount, stream);

            return response;
        }

        private bool TryParseURL(string url, out string host, out string hostHeader, out string pathAndQuery, out int port, out bool isHttps)
        {
            host = hostHeader = pathAndQuery = string.Empty;
            port = 0;
            isHttps = false;

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                return false; 

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            host = uri.Host;
            port = uri.Port;
            pathAndQuery = uri.PathAndQuery;
            isHttps = uri.Scheme == Uri.UriSchemeHttps;
            hostHeader = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";

            return true;
        }
    }
}