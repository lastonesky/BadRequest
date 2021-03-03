
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bad
{
    public class RequestItem
    {
        public string url { get; set; }
        public string cookie { get; set; }
        public string[] cookies { get; set; }
        private string _method = "GET";
        public string method
        {
            get { return _method; }
            set
            {
                string tmp = value.ToUpper();
                if (tmp != "GET" && tmp != "POST" && tmp != "HEAD")
                {
                    throw new Exception(value + " Method Not Support");
                }
                _method = value;
            }
        }
        public Dictionary<string, string> headers { get; set; }
        public string postdata { get; set; }
        public string proxyip { get; set; }
        public WebProxy Proxy { get; set; }

    }
    public class BadHttp
    {

        public static async Task<RequestResult> Request(RequestItem request)
        {
            var uri = new Uri(request.url);

            string result = string.Empty;
            var isSSL = uri.Scheme.ToLower() == "https";
            bool hasProxy = false;
            string proxyIp = string.Empty;
            int proxyPort = 0;
            if (!string.IsNullOrWhiteSpace(request.proxyip))
            {
                hasProxy = true;
                var proxy_a = request.proxyip.Split(':');
                proxyIp = string.IsNullOrWhiteSpace(proxy_a[0]) ? "127.0.0.1" : proxy_a[0];
                proxyPort = proxy_a.Length > 1 ? Convert.ToInt32(proxy_a[1]) : 80;
            }

            using (var tcp = hasProxy ? new TcpClient(proxyIp, proxyPort) : new TcpClient(uri.Host, uri.Port))
            using (Stream clientStream = (isSSL ? new SslStream(tcp.GetStream()) as Stream : tcp.GetStream()))
            {
                if (isSSL)
                {
                    ((SslStream)clientStream).AuthenticateAsClient(uri.Host);
                }
                tcp.SendTimeout = 2500;
                tcp.ReceiveTimeout = 8000;
                // Send request headers
                var builder = new StringBuilder();
                //uri.PathAndQuery = "/?scope=images&nr=1"
                builder.AppendLine(request.method + " " + uri.AbsolutePath + " HTTP/1.1");
                builder.AppendLine("Host: " + uri.Host);
                if (request.headers != null)
                {
                    foreach (var item in request.headers)
                    {
                        builder.AppendLine(item.Key + ": " + item.Value);
                    }
                }
                if (request.method == "POST")
                {
                    if ((request.headers == null || !request.headers.ContainsKey("Content-Length")) && !string.IsNullOrWhiteSpace(request.postdata))
                    {
                        builder.AppendLine("Content-Length: " + request.postdata.Length);
                    }
                }
                if (!string.IsNullOrWhiteSpace(request.cookie) && (request.headers == null || !request.headers.ContainsKey("Cookie")))
                {
                    builder.AppendLine("Cookie: " + request.cookie);
                }
                builder.AppendLine();
                var header = Encoding.ASCII.GetBytes(builder.ToString());
                await clientStream.WriteAsync(header, 0, header.Length);
                RequestResult _result = new RequestResult();

                // receive data
                using (var memory = new MemoryStream())
                {
                    await clientStream.CopyToAsync(memory);
                    memory.Position = 0;
                    var data = memory.ToArray();
                    //File.WriteAllBytes(@"D:\http_response.bin", data);
                    var index = BinaryMatch(data, Encoding.ASCII.GetBytes("\r\n\r\n")) + 4;
                    if (index > data.Length) index = data.Length;
                    var headers = Encoding.ASCII.GetString(data, 0, index);
                    _result.Headers = headers;
                    _result.Cookies = extractCookies(headers);
                    memory.Position = index;
                    if (headers.IndexOf("Content-Encoding: gzip") > 0)
                    {
                        if (headers.IndexOf("Transfer-Encoding: chunked") > 0)
                        {
                            byte[] buff = new byte[data.Length - index];
                            memory.Read(buff, 0, data.Length - index);
                            var cd = new ChunkDecode(buff);
                            buff = cd.ToDecode();
                            buff = GZip.GZipDecompress(buff);
                            result = Encoding.UTF8.GetString(buff);
                        }
                        else
                        {
                            result = Encoding.UTF8.GetString(GZip.GZipDecompress(data));
                        }
                    }
                    else
                    {
                        result = Encoding.UTF8.GetString(data, index, data.Length - index);
                        //result = Encoding.GetEncoding("gbk").GetString(data, index, data.Length - index);
                    }
                }
                _result.Html = result;
                return _result;
            }
        }

        private static int BinaryMatch(byte[] input, byte[] pattern)
        {
            int sLen = input.Length - pattern.Length + 1;
            for (int i = 0; i < sLen; ++i)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; ++j)
                {
                    if (input[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    return i;
                }
            }
            return -1;
        }
        private static string[] extractCookies(string headerText)
        {
            MatchCollection mm = Regex.Matches(headerText, @"^Set-Cookie:\s?(.+?)$", RegexOptions.Multiline);
            if (mm.Count > 0)
            {
                List<string> cks = new List<string>();
                foreach (Match item in mm)
                {
                    if (!string.IsNullOrWhiteSpace(item.Groups[1].Value))
                    {
                        int sepIdx = item.Groups[1].Value.IndexOf(';');
                        if (sepIdx > -1)
                        {
                            var cookieStr = item.Groups[1].Value.Substring(0, sepIdx);
                            cks.Add(cookieStr + ";");
                        }
                        else
                        {
                            cks.Add(item.Groups[1].Value);
                        }
                    }
                }
                return cks.ToArray();
            }
            else
            {
                return new string[0];
            }
        }
    }
    public class RequestResult
    {
        public RequestResult()
        {
            Cookies = new string[0];
        }
        public string[] Cookies { get; set; }
        public string Html { get; set; }
        public string Headers { get; set; }
    }
}
