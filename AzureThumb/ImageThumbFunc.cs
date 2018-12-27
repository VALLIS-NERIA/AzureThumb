using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using ImageResizer;
using NReco.VideoConverter;

namespace ImageResize {
    using System;
    using System.Drawing.Imaging;
    using System.Text.RegularExpressions;
    using Microsoft.WindowsAzure.Storage.Blob;

    public static class ImageThumbFunc {
        private static bool IsImage(string filename) {
            var extension = Path.GetExtension(filename)?.Replace(".", "");
            if (extension == null) {
                return false;
            }

            return Regex.IsMatch(extension, "gif|png|jpe?g", RegexOptions.IgnoreCase);
        }

        [FunctionName(nameof(ImageThumbFunc))]
        public static void Run(
            [BlobTrigger("ero/{name}", Connection = "")]
            Stream input,
            [Blob("thumbnails/{name}", FileAccess.Write)]
            CloudBlockBlob output,
            string name,
            TraceWriter log) {

            if (IsImage(name)) {
                var settings = new ResizeSettings
                {
                    MaxWidth = 400,
                    Format = "jpg"
                };
                ImageBuilder.Current.Build(input, output, settings);
                output.Properties.ContentType = "image/jpeg";
            }


            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {input.Length} Bytes");
        }
    }
}
