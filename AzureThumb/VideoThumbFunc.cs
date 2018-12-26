using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace ImageResize {
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Text.RegularExpressions;
    using ImageResizer;
    using Microsoft.WindowsAzure.Storage.Blob;
    using NReco.VideoConverter;

    public static class VideoThumbFunc {
        private static bool IsVideo(string filename) {
            var extension = Path.GetExtension(filename)?.Replace(".", "");
            if (extension == null) {
                return false;
            }

            return Regex.IsMatch(extension, "avi|mov|mp4|m4v", RegexOptions.IgnoreCase);
        }

        [FunctionName("VideoThumbFunc")]
        public static void Run(
            [BlobTrigger("ero/{name}", Connection = "")]
            CloudBlockBlob input,
            [Blob("thumbnails/{name}.thumb.jpg", FileAccess.Write)]
            Stream output,
            string name,
            TraceWriter log) {
            if (IsVideo(name)) {
                //var reader = new GleamTech.VideoUltimate.VideoFrameReader(input);
                //reader.Seek(30);
                //reader.Read();
                //var thumb = reader.GetFrame();
                //thumb.Save(output, ImageFormat.Jpeg);
                ////var length = reader.Duration.TotalSeconds;
                ////var span = length / 10;
                ////while(reader.Seek())
                
                var buffjpg = new MemoryStream();
                var ffmpeg = new FFMpegConverter();
                ffmpeg.GetVideoThumbnail(input.Uri.AbsoluteUri, buffjpg);
                var buffResize = new MemoryStream();
                var settings = new ResizeSettings
                {
                    MaxWidth = 400,
                    MaxHeight = 400,
                    Format = "png"
                };
                ImageBuilder.Current.Build(buffjpg, buffResize, settings);

                log.Info($"input string is {input.Uri}");
            }

            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {input.Properties.Length} Bytes");
        }
    }
}
