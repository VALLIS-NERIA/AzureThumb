using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using GleamTech.VideoUltimate;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureThumb {
    public class VideoThumbnailer: Thumbnailer {

        protected static readonly float[,] points = {{0.1f, 0.2f, 0.3f}, {0.4f, 0.5f, 0.6f}, {0.7f, 0.8f, 0.9f}};
        protected static readonly int[] fontSizes = {12, 18, 24};
        protected static readonly int row = 3, col = 3;
        protected static readonly int yOffset = 100;


        private class Screenshot : IDisposable {
            public Bitmap Image;
            public int Second;

            public void Dispose() {
                this.Image?.Dispose();
            }

            public static implicit operator Bitmap(Screenshot s) {
                return s.Image;
            }
        }

        private class VideoMeta {
            public int Width;
            public int Height;
            public string Name;
            public TimeSpan Duration;
            public int BitRate;
            public string Codec;
        }

        private static Screenshot[] GetVideoScreenshots(VideoFrameReader reader) {
            var array = new Screenshot[points.Length];
            var length = reader.Duration.TotalSeconds;

            for (int i = 0; i < col; i++) {
                for (int j = 0; j < row; j++) {
                    var index = j * col + i;
                    var point = points[j, i];
                    var time = (int)Math.Ceiling(length * point);
                    reader.Seek(time);
                    if (!reader.Read()) {
                        continue;
                    }

                    array[index] = new Screenshot { Image = reader.GetFrame(), Second = time };
                }
            }

            return array;
        }

        private static Task VideoThumbSingle(
            VideoMeta videoInfo,
            Screenshot[] caps,
            CloudBlockBlob input,
            CloudBlockBlob output,
            int maxLen,
            int fontSize) {
            var newSize = GetCappedSize(caps[0].Image.Width, caps[0].Image.Height, maxLen);
            var font = new Font(new FontFamily("Microsoft YaHei UI"), fontSize, FontStyle.Bold);

            var outputImg = new Bitmap(col * newSize.Width, row * newSize.Height + yOffset);
            var og = Graphics.FromImage(outputImg);
            og.Clear(Color.White);
            for (int i = 0; i < col; i++) {
                for (int j = 0; j < row; j++) {
                    var index = j * col + i;
                    var x = i * (newSize.Width + 2);
                    var y = j * (newSize.Height + 2) + yOffset;
                    og.DrawImage(caps[index], x, y, newSize.Width, newSize.Height);
                    var p = new GraphicsPath();
                    var timeString = $"{caps[index].Second / 60:D2}:{caps[index].Second % 60:D2}";
                    p.AddString(timeString, font.FontFamily, (int)font.Style, font.Size * 2, new Point(x, y), StringFormat.GenericDefault);
                    og.FillPath(Brushes.White, p);
                    og.DrawPath(new Pen(Color.Black, 2f), p);
                }
            }

            og.DrawString(
                $"{videoInfo.Name}\n" +
                $"{(int)videoInfo.Duration.TotalMinutes:D2}:{videoInfo.Duration.Seconds} - {videoInfo.BitRate}kbps, {videoInfo.Codec}\n",
                font,
                Brushes.Black,
                new RectangleF(0, 0, col * newSize.Width, yOffset));

            var outputStream = output.OpenWrite();
            outputImg.Save(outputStream, ImageFormat.Jpeg);
            outputImg.Dispose();
            outputStream.Flush();
            outputStream.Close();
            outputStream.Dispose();
            og.Dispose();
            output.Properties.ContentType = "image/jpeg";
            output.SetProperties();
            return SetMetadata(input, output, videoInfo.Width, videoInfo.Height, outputImg.Width, outputImg.Height);
        }


        internal static async void ThumbVideo(
            CloudBlockBlob input,
            CloudBlockBlob output_sm,
            CloudBlockBlob output_md,
            CloudBlockBlob output_lg,
            string name,
            TraceWriter log) {
            var begin = DateTime.Now;

            var inputStream = input.OpenRead();
            var reader = new VideoFrameReader(inputStream);

            var meta = new VideoMeta
            {
                Width = reader.Width,
                Height = reader.Height,
                Name = Path.GetFileName(input.Name),
                Duration = reader.Duration,
                BitRate = reader.BitRate,
                Codec = reader.CodecDescription
            };

            var caps = GetVideoScreenshots(reader);


            var tasks = new[]
            {
                VideoThumbSingle(meta, caps, input, output_sm, maxLength[0], fontSizes[0]),
                VideoThumbSingle(meta, caps, input, output_md, maxLength[1], fontSizes[1]),
                VideoThumbSingle(meta, caps, input, output_lg, maxLength[2], fontSizes[2]),
            };

            await Task.WhenAll(tasks);

            reader.Dispose();
            inputStream.Dispose();
            foreach (var cap in caps) {
                cap.Dispose();
            }
            log.Info($"Success in {(DateTime.Now - begin).TotalMilliseconds:F2}ms: {name}", "VideoThumbnail");
        }
    }
}