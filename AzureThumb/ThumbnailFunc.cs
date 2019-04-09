using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using ImageResizer;

namespace AzureThumb {
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Linq.Expressions;
    using System.Text.RegularExpressions;
    using Microsoft.WindowsAzure.Storage.Blob;

    public static class ThumbnailFunc {
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

        [FunctionName("ImageThumbnail")]
        public static void RunImage(
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
                    ImageThumbnailer.ImageThumb(input, output_sm, output_md, output_lg, name, log);
                }
            }
            catch (Exception e) {
                log.Error($"error while processing {name}", e);
                throw;
            }
        }

        [Disable("Disable_Video")]
        [FunctionName("VideoThumbnail")]
        public static void RunVideo(
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
                    VideoThumbnailer.ThumbVideo(input, output_sm, output_md, output_lg, name, log);
                }
            }
            catch (Exception e) {
                log.Error($"error while processing {name}", e);
                throw;
            }
        }
    }
}