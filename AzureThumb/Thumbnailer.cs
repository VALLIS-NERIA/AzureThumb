using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureThumb {
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Text.RegularExpressions;
    using ImageResizer;
    using Microsoft.Azure.WebJobs.Host;
    using Microsoft.WindowsAzure.Storage.Blob;
    using GleamTech.VideoUltimate;

    public static class Thumbnailer {
        private static float[] points = {0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f};
        private static int sizeCap = 400;
        private static int row = 3, col = 3;
        private static int yOffset = 100;

        internal static bool IsImage(string filename) {
            var extension = Path.GetExtension(filename)?.Replace(".", "");
            if (extension == null) {
                return false;
            }

            return Regex.IsMatch(extension, "gif|png|jpe?g", RegexOptions.IgnoreCase);
        }

        internal static bool IsVideo(string filename) {
            var extension = Path.GetExtension(filename)?.Replace(".", "");
            if (extension == null) {
                return false;
            }

            return Regex.IsMatch(extension, "avi|mov|mp4|m4v|mpg|flv", RegexOptions.IgnoreCase);
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


        internal static void ResizeImage(
            CloudBlockBlob input,
            CloudBlockBlob output,
            string name,
            TraceWriter log) {
            var begin = DateTime.Now;
            var settings = new ResizeSettings
            {
                MaxWidth = 400,
                Format = "jpg"
            };

            Stream stream = output.OpenWrite();

            ImageBuilder.Current.Build(input.OpenRead(), stream, settings, false);
            stream.Flush();
            stream.Close();
            output.Properties.ContentType = "image/jpeg";
            output.SetProperties();
            var time = DateTime.Now - begin;

            log.Info($"Success in {time.TotalMilliseconds:F2}ms: {name}", "ImageThumbnail");
        }

        internal static void ThumbVideo(
            CloudBlockBlob input,
            CloudBlockBlob output,
            string name,
            TraceWriter log) {
            var begin = DateTime.Now;
            var inputStream = input.OpenRead();


            var reader = new VideoFrameReader(inputStream);
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
                $"{name}\n" +
                $"{(int) reader.Duration.TotalMinutes:D2}:{reader.Duration.Seconds} - {reader.BitRate}kbps, {reader.CodecDescription}\n",
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

            log.Info($"Success in {(DateTime.Now - begin).TotalMilliseconds:F2}ms: {name}", "VideoThumbnail");
        }
    }
}