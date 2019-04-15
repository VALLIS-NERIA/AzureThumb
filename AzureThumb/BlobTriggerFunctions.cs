using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AzureThumb;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureThumb {

    public static partial class ThumbnailFunc {
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

#if DEBUG
        [Disable]
#else
        [Disable("Disable_Image")]
#endif
        [FunctionName("ImageThumbnail")]
        public static async Task RunImage(
            [BlobTrigger("ero/{name}", Connection = "AzureWebJobsStorage")]
            CloudBlockBlob input,
            [Blob("thumb-sm/{name}.thumb.jpg", FileAccess.ReadWrite)]
            CloudBlockBlob output_sm,
            [Blob("thumb-md/{name}.thumb.jpg", FileAccess.ReadWrite)]
            CloudBlockBlob output_md,
            [Blob("thumb-lg/{name}.thumb.jpg", FileAccess.ReadWrite)]
            CloudBlockBlob output_lg,
            string name,
            TraceWriter log) {
            try {
                if (IsImage(name)) {
                    await ImageThumbnailer.ImageThumb(input, output_sm, output_md, output_lg, name, log);
                }
            }
            catch (Exception e) {
                log.Error($"error while processing {name}", e);
                throw;
            }
        }

#if DEBUG
        [Disable]
#else
        [Disable("Disable_Video")]
#endif
        [FunctionName("VideoThumbnail")]
        public static async Task RunVideo(
            [BlobTrigger("ero/{name}", Connection = "AzureWebJobsStorage")]
            CloudBlockBlob input,
            [Blob("thumb-sm/{name}.thumb.jpg", FileAccess.ReadWrite)]
            CloudBlockBlob output_sm,
            [Blob("thumb-md/{name}.thumb.jpg", FileAccess.ReadWrite)]
            CloudBlockBlob output_md,
            [Blob("thumb-lg/{name}.thumb.jpg", FileAccess.ReadWrite)]
            CloudBlockBlob output_lg,
            string name,
            TraceWriter log) {
            try {
                if (IsVideo(name)) {
                    await VideoThumbnailer.ThumbVideo(input, output_sm, output_md, output_lg, name, log);
                }
            }
            catch (Exception e) {
                log.Error($"error while processing {name}", e);
                throw;
            }
        }
    }
}