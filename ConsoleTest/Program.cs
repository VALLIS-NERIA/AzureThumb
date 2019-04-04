using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleTest {
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Text.RegularExpressions;
    using GleamTech.VideoUltimate;

    public static class VideoThumbFunc {
        private static float[] points = {0.002f, 0.2f, 0.4f, 0.6f, 0.8f, 1.00f};
        private static int sizeCap = 400;
        private static int row = 2, col = 3;
        private static int yOffset = 100;

        static VideoThumbFunc() {
            Debug.Assert(row * col == points.Length);
            Debug.Assert(yOffset > 0);
            Debug.Assert(sizeCap > 0);
        }

        private static bool IsVideo(string filename) {
            var extension = Path.GetExtension(filename)?.Replace(".", "");
            if (extension == null) {
                return false;
            }

            return Regex.IsMatch(extension, "avi|mov|mp4|m4v", RegexOptions.IgnoreCase);
        }

        private static Size GetSize(VideoFrameReader reader) {
            int vidWidth = reader.Width;
            int vidHeight = reader.Height;

            int longer = vidWidth > vidHeight ? vidWidth : vidHeight;

            if (longer > sizeCap) {
                int picWidth = (int) ((float) vidWidth * sizeCap / longer);
                int picHeight = (int) ((float) vidHeight * sizeCap / longer);
                return new Size(picWidth, picHeight);
            }

            return new Size(vidWidth, vidHeight);
        }

        static Task T1() {
            return Task.Run(
                () => {
                    Thread.Sleep(1000);
                    Console.WriteLine("222222");
                });
        }

        static async Task Main() {
            var t = T1();
            Console.WriteLine("11111");
            await t;
            Console.ReadKey();
        }

        public static void _Main() {
            var path = "test.mp4";
            var shortPath = "short";
            Stream s = new FileStream("test.mp4", FileMode.Open);
            var reader = new VideoFrameReader(s);
            var newSize = GetSize(reader);

            var font = new Font(new FontFamily("Microsoft YaHei UI"), 20, FontStyle.Bold);

            var outputImg = new Bitmap(col * newSize.Width, row * newSize.Height + yOffset);
            var og = Graphics.FromImage(outputImg);
            og.Clear(Color.White);
            var length = reader.Duration.TotalSeconds;
            for (int i = 0; i < col; i++) {
                for (int j = 0; j < row; j++) {
                    var idx = j * col + i;
                    var point = points[idx];
                    var time = (int) Math.Ceiling(length * point);
                    var x = i * (newSize.Width + 2);
                    var y = j * (newSize.Height + 2) + yOffset;
                    reader.Seek(time);
                    if (!reader.Read()) {
                        continue;
                    }

                    using (var curImg = reader.GetFrame()) {
                        og.DrawImage(curImg, x, y, newSize.Width, newSize.Height);
                        var p = new GraphicsPath();
                        var timeString = $"{time / 60:D2}:{time % 60:D2}";
                        p.AddString(timeString, font.FontFamily, (int) font.Style, font.Size * 2, new Point(x, y), StringFormat.GenericDefault);
                        og.FillPath(Brushes.White, p);
                        og.DrawPath(new Pen(Color.Black, 2f), p);
                    }
                }
            }

            og.DrawString(
                $"{shortPath}\n" +
                $"{(int) reader.Duration.TotalMinutes:D2}:{reader.Duration.Seconds} - {reader.BitRate}kbps, {reader.CodecDescription}\n",
                font,
                Brushes.Black,
                new RectangleF(0, 0, col * newSize.Width, yOffset));
            outputImg.Save("out.jpg", ImageFormat.Jpeg);
            outputImg.Dispose();
            og.Dispose();
        }
    }
}