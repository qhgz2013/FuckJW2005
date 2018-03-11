using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Drawing;
using System.Text;
using System.Threading;

namespace ocr_project
{
    public class OCR_Wrapper : IDisposable
    {
        //python 路径
        private string _python_path;

        //默认的服务器ip和端口
        private const string _default_server_ip = "127.0.0.1";
        private const int _default_server_port = 10086;

        //python 进程
        private Process _python_process;
        public OCR_Wrapper(string python_path)
        {
            //这里修改为只要能连接到端口就不再重新创建进程
            var ipaddr = IPAddress.Parse(_default_server_ip);
            var ip_endpoint = new IPEndPoint(ipaddr, _default_server_port);
            try
            {
                var server_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server_socket.Connect(ip_endpoint);
            }
            catch (SocketException)
            {
                _python_path = python_path;
                _python_process = new Process();
                _python_process.StartInfo.Arguments = "cnn_server.py";
                _python_process.StartInfo.FileName = _python_path;
                _python_process.Start();
            }
        }

        public string OCR_Image(Image img)
        {
            var ipaddr = IPAddress.Parse(_default_server_ip);
            var ip_endpoint = new IPEndPoint(ipaddr, _default_server_port);
            var server_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server_socket.ReceiveTimeout = 5000;
            server_socket.SendTimeout = 5000;
            try
            {
                try
                {
                    server_socket.Connect(ip_endpoint);
                }
                catch
                {
                    server_socket = null;
                    return string.Empty;
                }

                var converter = new ImageConverter();
                var img_data = (byte[])converter.ConvertTo(img, typeof(byte[]));

                var length = img_data.Length;
                var length_bytes = new byte[4];
                for (int i = 3; i >= 0; i--)
                {
                    length_bytes[i] = (byte)(length & 0xff);
                    length >>= 8;
                }

                var send_bytes = new byte[4 + img_data.Length];
                Array.Copy(length_bytes, 0, send_bytes, 0, 4);
                Array.Copy(img_data, 0, send_bytes, 4, img_data.Length);
                server_socket.Send(send_bytes);
                var buf = new byte[256];
                length = server_socket.Receive(buf);
                var result = Encoding.UTF8.GetString(buf, 0, length);
                server_socket.Send(new byte[] { 0xff });
                return result;
            }
            catch (Exception)
            {
                return string.Empty;
            }
            finally
            {
                if (server_socket != null)
                {
                    server_socket.Close();
                    server_socket.Dispose();
                }
            }
        }
        ~OCR_Wrapper()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (_python_process != null)
            {
                _python_process.Kill();
                _python_process = null;
            }
        }
    }
}
