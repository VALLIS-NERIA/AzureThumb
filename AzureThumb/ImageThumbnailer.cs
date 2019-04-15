using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using ImageResizer;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureThumb {
    public class ImageThumbnailer : Thumbnailer {
        private static Task ImageThumbSingle(
            CloudBlockBlob input,
            CloudBlockBlob output,
            int maxLen) {
            Stream outStream = output.OpenWrite();
            var img = Image.FromStream(input.OpenRead());
            var thumbSize = GetCappedSize(img.Width, img.Height, maxLen);
            var settings = new ResizeSettings
            {
                Width = thumbSize.Width,
                Height = thumbSize.Height,
                Quality = 60,
                Format = "jpg"
            };
            ImageBuilder.Current.Build(img, outStream, settings, false);
            outStream.Flush();
            outStream.Close();

            output.Properties.ContentType = "image/jpeg";
            output.SetProperties();
            return SetMetadata(input, output, img.Width, img.Height, thumbSize.Width, thumbSize.Height);
        }

        internal static async Task ImageThumb(
            CloudBlockBlob input,
            CloudBlockBlob output_sm,
            CloudBlockBlob output_md,
            CloudBlockBlob output_lg,
            string name,
            TraceWriter log) {
            var begin = DateTime.Now;

            var tasks = new[]
            {
                ImageThumbSingle(input, output_sm, maxLength[0]),
                ImageThumbSingle(input, output_md, maxLength[1]),
                ImageThumbSingle(input, output_lg, maxLength[2]),
            };

            await Task.WhenAll(tasks);

            var time = DateTime.Now - begin;

            log.Info($"Success in {time.TotalMilliseconds:F2}ms: {name}", "ImageThumbnail");
        }
    }
}