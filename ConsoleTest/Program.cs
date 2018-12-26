using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest {
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using ImageResizer;
    using NReco.VideoConverter;

    public static class VideoThumbFunc {
        private static bool IsVideo(string filename) {
            var extension = Path.GetExtension(filename)?.Replace(".", "");
            if (extension == null) {
                return false;
            }

            return Regex.IsMatch(extension, "avi|mov|mp4|m4v", RegexOptions.IgnoreCase);
        }

        public static void Main() {
            var ffmpeg = new FFMpegConverter();
            var probe = new NReco.VideoInfo.FFProbe();
            var info = probe.GetMediaInfo("test.mp4");
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

            if (!ok) return;
            int longer = vidWidth > vidHeight ? vidWidth : vidHeight;

            int cap = 400;
            Size? newSize = null;
            if (longer > cap) {
                int picWidth = (int)((float)vidWidth * cap / longer);
                int picHeight = (int)((float)vidHeight * cap / longer);
                newSize = new Size(picWidth, picHeight);
            }

            var buffjpg = new MemoryStream();
            ffmpeg.GetVideoThumbnail("test.mp4", buffjpg);
            Image outImg = newSize == null ? Image.FromStream(buffjpg) : new Bitmap(Image.FromStream(buffjpg), newSize.Value);

            using (var g = Graphics.FromImage(outImg)) {
                g.DrawString("00:00:00", SystemFonts.DefaultFont, Brushes.Cyan, 0, 0);
            }

            outImg.Save("test.png");
        }
    }
}
