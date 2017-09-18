using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ocr_project
{
    public class ocr
    {
        public static bool ocr_image(Image img, out string result)
        {
            throw new NotImplementedException();
        }

        //reducing noise (escaping noise dot)
        public static Image _noise_reduce_and_to_binary(Image img)
        {
            var bmp_in = new Bitmap(img);
            var bmp_out = new Bitmap(bmp_in.Width, bmp_in.Height);
            var lck_in = bmp_in.LockBits(new Rectangle(0, 0, bmp_in.Width, bmp_in.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var lck_out = bmp_out.LockBits(new Rectangle(0, 0, bmp_out.Width, bmp_out.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            var scale_width = lck_in.Stride;

            var bits_in = new byte[scale_width * bmp_in.Height];
            var bits_out = new byte[scale_width * bmp_out.Height];

            Marshal.Copy(lck_in.Scan0, bits_in, 0, bits_in.Length);

            //processing
            var val_dic = new Dictionary<Color, int>();
            for (int x = 0; x < bmp_in.Width; x++)
            {
                for (int y = 0; y < bmp_in.Height; y++)
                {
                    int offset = y * scale_width + x * 3;
                    Color pixel_color = Color.FromArgb(255, bits_in[offset + 2], bits_in[offset + 1], bits_in[offset]);
                    if (pixel_color.ToArgb() != Color.White.ToArgb())
                    {
                        if (val_dic.ContainsKey(pixel_color))
                            val_dic[pixel_color]++;
                        else
                            val_dic.Add(pixel_color, 1);
                    }
                }
            }
            var max_color_count = from KeyValuePair<Color, int> color_count in val_dic orderby color_count.Value descending select color_count;
            var max_color = max_color_count.First().Key;
            //building new image
            for (int x = 0; x < bmp_in.Width; x++)
            {
                for (int y = 0; y < bmp_in.Height; y++)
                {
                    int offset = y * scale_width + x * 3;
                    Color pixel_color = Color.FromArgb(255, bits_in[offset + 2], bits_in[offset + 1], bits_in[offset]);

                    if (pixel_color.ToArgb() == max_color.ToArgb())
                    {
                        bits_out[offset] = 0;
                        bits_out[offset + 1] = 0;
                        bits_out[offset + 2] = 0;
                    }
                    else
                    {
                        bits_out[offset] = 255;
                        bits_out[offset + 1] = 255;
                        bits_out[offset + 2] = 255;
                    }
                }
            }

            Marshal.Copy(bits_out, 0, lck_out.Scan0, bits_out.Length);
            bmp_in.UnlockBits(lck_in);
            bmp_out.UnlockBits(lck_out);
            return bmp_out;
        }
        //upscaling
        public static Image _upscale_4x(Image origin)
        {
            var bmp_in = new Bitmap(origin);
            var bmp_out = new Bitmap(bmp_in.Width * 4, bmp_in.Height * 4);
            var lck_in = bmp_in.LockBits(new Rectangle(0, 0, bmp_in.Width, bmp_in.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var lck_out = bmp_out.LockBits(new Rectangle(0, 0, bmp_out.Width, bmp_out.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            var scale_width = lck_in.Stride;

            var bits_in = new byte[lck_in.Stride * bmp_in.Height];
            var bits_out = new byte[lck_out.Stride * bmp_out.Height];

            Marshal.Copy(lck_in.Scan0, bits_in, 0, bits_in.Length);

            for (int y = 0; y < bmp_in.Height; y++)
            {
                for (int x = 0; x < bmp_in.Width; x++)
                {
                    int offset_in = y * lck_in.Stride + x * 3;

                    for (int dy = 0; dy < 4; dy++)
                    {
                        for (int dx = 0; dx < 4; dx++)
                        {
                            int offset_out = (4 * y + dy) * lck_out.Stride + (4 * x + dx) * 3;
                            for (int i = 0; i < 3; i++)
                                bits_out[offset_out + i] = bits_in[offset_in + i];
                        }
                    }
                }
            }

            Marshal.Copy(bits_out, 0, lck_out.Scan0, bits_out.Length);
            bmp_in.UnlockBits(lck_in);
            bmp_out.UnlockBits(lck_out);
            return bmp_out;
        }

        private static Rectangle _bfs(ref byte[] visited, Point entry_point, Size matrix_size)
        {
            var bfs_queue = new Queue<Point>();
            int min_x = entry_point.X, max_x = entry_point.X, min_y = entry_point.Y, max_y = entry_point.Y;
            bfs_queue.Enqueue(entry_point);
            visited[entry_point.Y * matrix_size.Width + entry_point.X] = 1;
            
            while (bfs_queue.Count > 0)
            {
                Point current_point = bfs_queue.Dequeue();

                if (current_point.X < min_x) min_x = current_point.X;
                if (current_point.X > max_x) max_x = current_point.X;
                if (current_point.Y < min_y) min_y = current_point.Y;
                if (current_point.Y > max_y) max_y = current_point.Y;

                int up_offset = (current_point.Y - 1) * matrix_size.Width + current_point.X;
                int down_offset = (current_point.Y + 1) * matrix_size.Width + current_point.X;
                int left_offset = current_point.Y * matrix_size.Width + current_point.X - 1;
                int right_offset = current_point.Y * matrix_size.Width + current_point.X + 1;

                if (current_point.Y > 0 && visited[up_offset] == 0)
                {
                    visited[up_offset] = 1;
                    bfs_queue.Enqueue(new Point(current_point.X, current_point.Y - 1));
                }
                if (current_point.Y < matrix_size.Height - 1 && visited[down_offset] == 0)
                {
                    visited[down_offset] = 1;
                    bfs_queue.Enqueue(new Point(current_point.X, current_point.Y + 1));
                }
                if (current_point.X > 0 && visited[left_offset] == 0)
                {
                    visited[left_offset] = 1;
                    bfs_queue.Enqueue(new Point(current_point.X - 1, current_point.Y));
                }
                if (current_point.X < matrix_size.Width - 1 && visited[right_offset] == 0)
                {
                    visited[right_offset] = 1;
                    bfs_queue.Enqueue(new Point(current_point.X + 1, current_point.Y));
                }
            }

            return new Rectangle(min_x, min_y, max_x - min_x + 1, max_y - min_y + 1);
        }
        public static Rectangle[] _bfs_detection(Image img)
        {
            var ret = new List<Rectangle>();

            var bmp = new Bitmap(img);
            var visited = new byte[bmp.Width * bmp.Height];
            var lck = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            var bits = new byte[lck.Stride * bmp.Height];
            Marshal.Copy(lck.Scan0, bits, 0, bits.Length);

            //set white background as visited status
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    int offset = y * lck.Stride + 3 * x;
                    if (bits[offset] == 255 && bits[offset] == bits[offset + 1] && bits[offset + 1] == bits[offset + 2])
                        visited[y * bmp.Width + x] = 1;
                }
            }

            //scanning every unvisited point
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    if (visited[y * bmp.Width + x] == 0)
                    {
                        ret.Add(_bfs(ref visited, new Point(x, y), bmp.Size));
                    }
                }
            }
            bmp.UnlockBits(lck);
            return ret.ToArray();
        }
        public static Rectangle[] _bfs_noise_reduce(Rectangle[] rects)
        {
            var ret = new List<Rectangle>();
            foreach (var item in rects)
            {
                if (item.Width * item.Height > 1) ret.Add(item);
            }
            return ret.ToArray();
        }
        public static Rectangle[] _split_into_4_rect(Rectangle[] rects)
        {
            var ret = new List<Rectangle>();
            int total_length = 0;
            foreach (var item in rects)
                total_length += item.Width;
            foreach (var item in rects)
            {
                int div_weight = (int)Math.Round(4.0 * item.Width / total_length);
                for (int i = 0; i < div_weight; i++)
                {
                    ret.Add(new Rectangle(item.X + i * item.Width / div_weight, item.Y, ((i + 1) * item.Width / div_weight) - i * item.Width / div_weight, item.Height));
                }
            }
            return ret.ToArray();
        }
        private static void load_module_data()
        {

        }
        private static void save_module_data()
        {

        }
    }
}
