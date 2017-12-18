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
        private Socket _server_socket;
        private Guid _wrapper_guid;
        public OCR_Wrapper(string python_path)
        {
            _python_path = python_path;
            _python_process = new Process();
            _python_process.StartInfo.Arguments = "cnn_server.py";
            _python_process.StartInfo.FileName = _python_path;
            _python_process.StartInfo.UseShellExecute = false;
            _python_process.StartInfo.CreateNoWindow = true;
            _python_process.Start();

            var ipaddr = IPAddress.Parse(_default_server_ip);
            bool connect_suc = false;
            var ip_endpoint = new IPEndPoint(ipaddr, _default_server_port);

            _server_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            while (!connect_suc)
            {
                try
                {
                    _server_socket.Connect(ip_endpoint);
                    connect_suc = true;
                }
                catch
                {
                    Thread.Sleep(100);
                }
            }

            _wrapper_guid = Guid.NewGuid();
        }

        public string OCR_Image(Image img)
        {
            var converter = new ImageConverter();
            var img_data = (byte[])converter.ConvertTo(img, typeof(byte[]));
            var guid_data = _wrapper_guid.ToByteArray();

            var send_data = new byte[guid_data.Length + img_data.Length];
            Array.Copy(guid_data, 0, send_data, 0, guid_data.Length);
            Array.Copy(img_data, 0, send_data, guid_data.Length, img_data.Length);

            _server_socket.Send(send_data);

            do
            {
                var buffer = new byte[4096];
                var length = _server_socket.Receive(buffer);

                var recv_guid_data = new byte[guid_data.Length];
                var recv_img_result = new byte[length - guid_data.Length];

                Array.Copy(buffer, 0, recv_guid_data, 0, recv_guid_data.Length);
                Array.Copy(buffer, recv_guid_data.Length, recv_img_result, 0, recv_img_result.Length);

                var recv_guid = new Guid(recv_guid_data);
                if (recv_guid == _wrapper_guid)
                {
                    var result = Encoding.UTF8.GetString(recv_img_result);
                    return result;
                }
            } while (true);
        }
        ~OCR_Wrapper()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (_server_socket != null)
            {
                _server_socket.Close();
                _server_socket.Dispose();
                _server_socket = null;
            }
            if (_python_process != null)
            {
                _python_process.Kill();
                _python_process = null;
            }
        }
    }
}
