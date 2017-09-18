using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace ocr_project
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private const int _MAX_SAMPLE_SIZE = 500;
        const int _PARALLEL_THREAD_COUNT = 4;
        private bool _enable_operation = false;
        private void Form1_Load(object sender, EventArgs e)
        {
            var data_dir = new DirectoryInfo("data");
            if (!data_dir.Exists || data_dir.GetFiles().Length < _MAX_SAMPLE_SIZE)
            {
                label1.Text = "Initializing OCR sample data... (" + _MAX_SAMPLE_SIZE + " in total, " + _PARALLEL_THREAD_COUNT + " in parallel)";
                _download_captcha_list();
            }
        }
        private void _download_captcha_list()
        {
            if (!Directory.Exists("data")) Directory.CreateDirectory("data");
            int count = Directory.GetFiles("data").Length;

            var url = "http://110.65.10.231/(1mnvspqmtpkayozrsa5lt045)/checkcode.aspx";

            //4 in parallel
            int thread_in_used = _PARALLEL_THREAD_COUNT;
            for (int i = 0; i < _PARALLEL_THREAD_COUNT; i++)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    var md5_server = new System.Security.Cryptography.MD5CryptoServiceProvider();
                    while (count < _MAX_SAMPLE_SIZE)
                    {
                        var stream = util.http_get(ref url);
                        //md5 hash
                        var bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, bytes.Length);
                        var md5_bytes = md5_server.ComputeHash(bytes);
                        var md5 = "";
                        for (int c = 0; c < md5_bytes.Length; c++) md5 += md5_bytes[c].ToString("X2").ToLower();

                        if (count < _MAX_SAMPLE_SIZE)
                        {
                            var last_exist = File.Exists("data/" + md5);
                            if (last_exist)
                                Debug.Print("Warning: MD5 hash file: " + md5 + " has already existed");
                            else
                                count++;
                            var fs = new FileStream("data/" + md5, FileMode.Create, FileAccess.Write);
                            fs.Write(bytes, 0, bytes.Length);
                            fs.Close();
                        }
                        stream.Close();
                        bytes = null;

                    }

                    thread_in_used--;
                });
            }

            ThreadPool.QueueUserWorkItem(delegate
            {
                while (thread_in_used > 0) Thread.Sleep(100);
                Invoke(new NoArgSTA(InitializeCompleted));
            });
        }
        private delegate void NoArgSTA();
        private void InitializeCompleted()
        {
            label1.Text = "Initialize completed";
            _enable_operation = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var sw = new Stopwatch();
            sw.Start();

            //generate random sample path
            var random = new Random();
            var file_list = Directory.GetFiles("data");
            var file_path = file_list[random.Next(file_list.Length)];

            //load into memory
            var fs = new FileStream(file_path, FileMode.Open, FileAccess.Read);
            var ms = new MemoryStream();
            var buffer = new byte[fs.Length];
            fs.Read(buffer, 0, buffer.Length);
            fs.Close();
            ms.Write(buffer, 0, buffer.Length);
            ms.Seek(0, SeekOrigin.Begin);

            //load image from memory stream
            var img = Image.FromStream(ms);

            pictureBox1.Image = img;
            var load_timestamp = sw.Elapsed;

            //noise reduce
            img = ocr._noise_reduce_and_to_binary(img);
            pictureBox2.Image = img;
            var noise_reduce_timestamp = sw.Elapsed;

            //bfs detection
            var rects = ocr._bfs_noise_reduce(ocr._bfs_detection(img));
            //rects = ocr._split_into_4_rect(rects);
            var bfs_timestamp = sw.Elapsed;

            //4x upscaling
            img = ocr._upscale_4x(img);
            pictureBox3.Image = img;
            var upscaling_timestamp = sw.Elapsed;

            var gr = Graphics.FromImage(img);
            for (int i = 0; i < rects.Length; i++)
            {
                rects[i] = new Rectangle(rects[i].X * 4 - 1, rects[i].Y * 4 - 1, rects[i].Width * 4 + 0, rects[i].Height * 4 + 0);
            }
            if (rects.Length > 0) gr.DrawRectangles(Pens.Red, rects);
            gr.Dispose();


            sw.Stop();

            label4.Text = "Ellapsed time: " + upscaling_timestamp.TotalMilliseconds.ToString("0.000") + "ms\r\n\r\n";
            label4.Text += "Load: " + (load_timestamp).TotalMilliseconds.ToString("0.000") + " ms\r\n";
            label4.Text += "Noise Reduce: " + (noise_reduce_timestamp - load_timestamp).TotalMilliseconds.ToString("0.000") + " ms\r\n";
            label4.Text += "BFS Detect: " + (bfs_timestamp - noise_reduce_timestamp).TotalMilliseconds.ToString("0.000") + " ms\r\n";
            label4.Text += "Upscale: " + (upscaling_timestamp - bfs_timestamp).TotalMilliseconds.ToString("0.000") + " ms\r\n";
        }
    }
    
}
