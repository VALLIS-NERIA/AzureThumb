using System.Diagnostics;
//using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
//using Microsoft.WindowsAzure.Storage;
//using Microsoft.WindowsAzure.Storage.Auth;
//using Microsoft.WindowsAzure.Storage.Blob;
//using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Primitives;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using SixLabors.Shapes;
using SixLabors.ImageSharp.Processing.Processors.Text;
using SixLabors.ImageSharp.Processing.Processors;

namespace ConsoleTest {
    using System;
    using System.IO;
    using System.Text.RegularExpressions;
    using GleamTech.VideoUltimate;

    public static class VideoThumbFunc {
        private static float[] points = {0.002f, 0.2f, 0.4f, 0.6f, 0.8f, 1.00f};
        private static int sizeCap = 300;
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

        //static void  Main() {
        //    var secret = JsonConvert.DeserializeAnonymousType(File.ReadAllText("..\\..\\secret.json"), new {baseUri = "", storageName = "", storageKey = ""});
        //    CloudBlockBlob a = new CloudBlockBlob(new Uri(secret.baseUri + @"ero/test/kick.jpg"), new StorageCredentials(secret.storageName, secret.storageKey));
        //    ;
        //}

        public static void Main() {
            var path = "test.mp4";
            var shortPath = "short";
            Stream s = new FileStream("test.mp4", FileMode.Open);
            var reader = new VideoFrameReader(s);
            var newSize = GetSize(reader);
            var fonts = new FontCollection();
            fonts.Install("Consolas YaHei.ttf");
            var font =  fonts.CreateFont("Consolas YaHei", 12, FontStyle.Regular);

            var outputImg = new Image<Rgba32>(col * newSize.Width, row * newSize.Height + yOffset);
            outputImg.Mutate(c => c.BackgroundColor(Rgba32.White));

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

                    using (var curImg = new Image<Rgba32>(200, 150)) {
                        curImg.Mutate(c=>c.BackgroundColor(Rgba32.AliceBlue));
                        curImg.Mutate(c => c.Resize(newSize));
                        outputImg.Mutate(c => c.DrawImage(curImg, new Point(x, y), 1f));

                        var timeString = $"{time / 60:D2}:{time % 60:D2}";
                        outputImg.Mutate(c => c.DrawText(timeString, font, Brushes.Solid(Rgba32.White), Pens.Solid(Rgba32.HotPink, 1), new PointF(x+10, y+10)));
                    }
                }
            }

            outputImg.Mutate(c => c.DrawText($"{shortPath}\n" +
                                             $"{(int) reader.Duration.TotalMinutes:D2}:{reader.Duration.Seconds} - {reader.BitRate}kbps, {reader.CodecDescription}\n",
                                             font,
                                             Brushes.Solid(Rgba32.Black),
                                             Pens.Solid(Rgba32.HotPink, 1),
                                             new PointF(10, 10)));

            outputImg.Save("out.jpg", new JpegEncoder());
            outputImg.Dispose();
        }
    }
}