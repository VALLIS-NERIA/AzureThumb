using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace ImageResize {
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Text.RegularExpressions;
    using ImageResizer;
    using Microsoft.WindowsAzure.Storage.Blob;
    using NReco.VideoConverter;
    using NReco.VideoInfo;

    public static class VideoThumbFunc {
        private static float[] points = { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f };
        private static int sizeCap = 400;
        private static int row = 3, col = 3;
        private static int yOffset = 100;

        private static bool IsVideo(string filename) {
            var extension = Path.GetExtension(filename)?.Replace(".", "");
            if (extension == null) {
                return false;
            }

            return Regex.IsMatch(extension, "avi|mov|mp4|m4v", RegexOptions.IgnoreCase);
        }

        private static Size GetSize(MediaInfo info) {
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

            var videoSize = new Size(vidWidth, vidHeight);

            if (!ok) throw new InvalidDataException("There is no video stream");
            int longer = vidWidth > vidHeight ? vidWidth : vidHeight;

            if (longer > sizeCap) {
                int picWidth = (int)((float)vidWidth * sizeCap / longer);
                int picHeight = (int)((float)vidHeight * sizeCap / longer);
                return new Size(picWidth, picHeight);
            }

            return videoSize;
        }

        [FunctionName("VideoThumbFunc")]
        public static void Run(
            [BlobTrigger("ero/{name}", Connection = "AzureWebJobsStorage")]
            CloudBlockBlob input,
            [Blob("thumbnails/{name}.thumb.jpg", FileAccess.ReadWrite)]
            CloudBlockBlob output,
            string name,
            TraceWriter log) {
            if (!IsVideo(name)) {
                return;
            }

            var path = input.Uri.AbsoluteUri;
            var shortPath = input.Uri.AbsolutePath;
            var ffmpeg = new FFMpegConverter();
            var probe = new FFProbe();
            var info = probe.GetMediaInfo(path);
            var newSize = GetSize(info);

            var font = new Font(FontFamily.GenericSansSerif, 20, FontStyle.Bold);

            var outputImg = new Bitmap(col * newSize.Width, row * newSize.Height + yOffset);
            var og = Graphics.FromImage(outputImg);
            og.Clear(Color.White);
            var length = info.Duration.TotalSeconds;
            for (int i = 0; i < col; i++) {
                for (int j = 0; j < row; j++) {
                    var idx = i * col + j;
                    var point = points[idx];
                    var time = (int)(length * point);
                    using (var buffjpg = new MemoryStream()) {
                        var x = i * (newSize.Width + 2);
                        var y = j * (newSize.Height + 2) + yOffset;
                        ffmpeg.GetVideoThumbnail(path, buffjpg, time);
                        var curImg = Image.FromStream(buffjpg);
                        og.DrawString(TimeSpan.FromSeconds(time).ToString(@"hh\:mm\:ss"), font, Brushes.Cyan, x, y);
                        og.DrawImage(curImg, x, y, newSize.Width, newSize.Height);
                        curImg.Dispose();
                    }
                }
            }

            og.DrawString($"{shortPath}\n{(int)info.Duration.TotalMinutes:D2}:{info.Duration.Seconds}", font, Brushes.Black, new RectangleF(0, 0, col * newSize.Width, yOffset));
            var stream = output.OpenWrite();
            outputImg.Save(stream, ImageFormat.Jpeg);
            outputImg.Dispose();
            og.Dispose();
            stream.Flush();
            stream.Close();
            output.Properties.ContentType = "image/jpeg";
            output.SetProperties();

            log.Info($"Success: {name}", "VideoThumbnail");
        }
    }
}
