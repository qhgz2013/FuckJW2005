using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Threading;

namespace ocr_project
{
    public class util
    {
        public static CookieContainer default_cc = new CookieContainer();
        public delegate void NoArgSTA();
        public static event NoArgSTA RequestSucceed, RequestFailed;
        public static Stream http_get(ref string url, string origin = null, string referer = null, bool enable_event_callback = true, int retry_delay = 3000)
        {
            do
            {
                var http_request = (HttpWebRequest)WebRequest.Create(url);
                http_request.ContentLength = 0;
                http_request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                http_request.Accept = "*/*";
                if (!string.IsNullOrEmpty(referer)) http_request.Referer = referer;
                if (!string.IsNullOrEmpty(origin)) http_request.Headers.Add("Origin", origin);
                http_request.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
                http_request.CookieContainer = default_cc;
                http_request.Method = "GET";
                http_request.Timeout = 10000;

                HttpWebResponse http_response = null;
                try
                {
                    http_response = (HttpWebResponse)http_request.GetResponse();
                    url = http_response.ResponseUri.ToString();

                    //todo: fixing the known issue
                    default_cc.Add(http_response.Cookies);

                    if (http_response.StatusCode == HttpStatusCode.OK)
                    {
                        var stream = http_response.GetResponseStream();
                        if (http_response.ContentEncoding == "gzip")
                            stream = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress);
                        else if (http_response.ContentEncoding == "deflate")
                            stream = new System.IO.Compression.DeflateStream(stream, System.IO.Compression.CompressionMode.Decompress);

                        var stream_out = new MemoryStream();
                        int read_bytes = 0;
                        const int buffer_size = 16384;
                        byte[] buffer = new byte[buffer_size];
                        do
                        {
                            read_bytes = stream.Read(buffer, 0, buffer_size);
                            stream_out.Write(buffer, 0, read_bytes);
                        } while (read_bytes > 0);
                        stream.Close();

                        stream_out.Seek(0, SeekOrigin.Begin);
                        if (enable_event_callback && RequestSucceed != null) RequestSucceed.Invoke();
                        return stream_out;
                    }
                }
                catch (Exception)
                {
                    if (enable_event_callback && RequestFailed != null) RequestFailed.Invoke();
                }
                finally
                {
                    if (http_response != null) http_response.Close();
                }
                if (retry_delay > 0) Thread.Sleep(retry_delay);
            } while (true);
        }
        public static Stream http_post(ref string url, byte[] post_param, string content_type, string origin = null, string referer = null, bool enable_event_callback = true, int retry_delay = 3000)
        {
            do
            {
                var http_request = (HttpWebRequest)WebRequest.Create(url);
                http_request.ContentLength = post_param.Length;
                http_request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                http_request.Accept = "*/*";
                if (!string.IsNullOrEmpty(referer)) http_request.Referer = referer;
                if (!string.IsNullOrEmpty(origin)) http_request.Headers.Add("Origin", origin);
                if (!string.IsNullOrEmpty(content_type)) http_request.ContentType = content_type;
                http_request.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
                http_request.CookieContainer = default_cc;
                http_request.Method = "POST";
                http_request.Timeout = 10000;

                HttpWebResponse http_response = null;
                try
                {
                    try
                    {
                        var post_stream = http_request.GetRequestStream();
                        post_stream.Write(post_param, 0, post_param.Length);
                        post_stream.Close();
                    }
                    catch (Exception)
                    {
                        throw;
                    }

                    http_response = (HttpWebResponse)http_request.GetResponse();
                    url = http_request.RequestUri.ToString();

                    //todo: fixing the known issue
                    default_cc.Add(http_response.Cookies);

                    if (http_response.StatusCode == HttpStatusCode.OK)
                    {
                        var stream = http_response.GetResponseStream();
                        if (http_response.ContentEncoding == "gzip")
                            stream = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress);
                        else if (http_response.ContentEncoding == "deflate")
                            stream = new System.IO.Compression.DeflateStream(stream, System.IO.Compression.CompressionMode.Decompress);

                        var stream_out = new MemoryStream();
                        int read_bytes = 0;
                        const int buffer_size = 16384;
                        byte[] buffer = new byte[buffer_size];
                        do
                        {
                            read_bytes = stream.Read(buffer, 0, buffer_size);
                            stream_out.Write(buffer, 0, read_bytes);
                        } while (read_bytes > 0);
                        stream.Close();

                        stream_out.Seek(0, SeekOrigin.Begin);
                        if (enable_event_callback && RequestSucceed != null) RequestSucceed.Invoke();
                        return stream_out;
                    }
                }
                catch (Exception)
                {
                    if (enable_event_callback && RequestFailed != null) RequestFailed.Invoke();
                }
                finally
                {
                    if (http_response != null) http_response.Close();
                }
                if (retry_delay > 0) Thread.Sleep(retry_delay);
            } while (true);
        }
    }
}
