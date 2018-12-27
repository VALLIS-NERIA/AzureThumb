using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Media;

namespace ConsoleTest {
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using MediaToolkit.Options;
    using NReco.VideoConverter;
    using NReco.VideoInfo;

    public static class VideoThumbFunc {
        private static bool IsVideo(string filename) {
            var extension = Path.GetExtension(filename)?.Replace(".", "");
            if (extension == null) {
                return false;
            }

            return Regex.IsMatch(extension, "avi|mov|mp4|m4v", RegexOptions.IgnoreCase);
        }

        private static float[] points = { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f };
        private static int sizeCap = 400;
        private static int row = 3, col = 3;
        private static int yOffset = 100;
        public static void Main() {
            var path = "test.mp4";
            var ffmpeg = new FFMpegConverter();
            var probe = new FFProbe();
            var info = probe.GetMediaInfo("test.mp4");
            var needResize = GetSize(info, out var videoSize, out var newSize);

            var font = new Font(FontFamily.GenericSansSerif, 20, FontStyle.Bold);

            var imgs = new List<Image>();
            var length = info.Duration.TotalSeconds;
            foreach (var point in points) {
                var time = (int)(length * point);
                using (var buffjpg = new MemoryStream()) {
                    ffmpeg.GetVideoThumbnail("test.mp4", buffjpg, time);
                    var outImg =  Image.FromStream(buffjpg);
                    using (var g = Graphics.FromImage(outImg)) {
                        g.DrawString(TimeSpan.FromSeconds(time).ToString(@"hh\:mm\:ss"), font, Brushes.Cyan, 0, 0);
                    }
                    imgs.Add(outImg);
                }
            }

            var output = new Bitmap(col * newSize.Width, row * newSize.Height + yOffset);
            var og = Graphics.FromImage(output);
            og.Clear(Color.White);
            for (int i = 0; i < col; i++) {
                for (int j = 0; j < row; j++) {
                    var curImg = imgs[i * col + j];
                    var x = i * (newSize.Width + 2);
                    var y = j * (newSize.Height + 2);
                    og.DrawImage(curImg, x, y + yOffset, newSize.Width, newSize.Height);
                }
            }

            og.DrawString($"test.mp4\n{(int)info.Duration.TotalMinutes:D2}:{info.Duration.Seconds}", font, Brushes.Black, 0, 0);
            output.Save("test.png");
            og.Dispose();
        }
        
        private static bool GetSize(MediaInfo info, out Size videoSize, out Size newSize) {
            int vidWidth = -1, vidHeight = -1;
            bool ok = false;
            foreach (var s in info.Streams) {
                if (s.CodecType == "video") {
                    vidWidth = s.Width;
                    vidHeight = s.Height;
                    ok = true;
                    break;
                }
            }
            videoSize = new Size(vidWidth, vidHeight);

            if (!ok) throw new InvalidDataException("There is no video stream");
            int longer = vidWidth > vidHeight ? vidWidth : vidHeight;
            
            if (longer > sizeCap) {
                int picWidth = (int)((float)vidWidth * sizeCap / longer);
                int picHeight = (int)((float)vidHeight * sizeCap / longer);
                newSize = new Size(picWidth, picHeight);
                return true;
            }

            newSize = videoSize;
            return false;
        }
    }
}
