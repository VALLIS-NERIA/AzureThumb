using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using ImageResizer;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;

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

#if DEBUG
        [Disable]
#endif
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

#if DEBUG
        [Disable]
#else
        [Disable("Disable_Video")]
#endif
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


        private static string[] availableSizes = {"sm", "md", "lg"};
        private static string defaultSize = "md";
        private static string storageBaseUrl = System.Environment.GetEnvironmentVariable("StorageBaseUrl", EnvironmentVariableTarget.Process);
        private static string storageName = System.Environment.GetEnvironmentVariable("StorageName", EnvironmentVariableTarget.Process);
        private static string storageKey = System.Environment.GetEnvironmentVariable("StorageKey", EnvironmentVariableTarget.Process);


        [FunctionName("RestThumb")]
        public static async Task<HttpResponseMessage> RunRest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "thumb")]
            HttpRequestMessage req, TraceWriter log) {
            string name = req.GetQueryNameValuePairs()
                             .FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0)
                             .Value;

            string size = req.GetQueryNameValuePairs()
                             .FirstOrDefault(q => string.Compare(q.Key, "size", true) == 0)
                             .Value;

            if (string.IsNullOrEmpty(name)) {
                return new HttpResponseMessage(HttpStatusCode.BadRequest) {Content = new StringContent($"must give a name")};
            }

            if (string.IsNullOrEmpty(size)) {
                size = defaultSize;
            }

            if (!availableSizes.Contains(size)) {
                return new HttpResponseMessage(HttpStatusCode.BadRequest) {Content = new StringContent($"invalid size")};
            }

            bool isImage = IsImage(name), isVideo = IsVideo(name);

            if (!(isImage || isVideo)) {
                return new HttpResponseMessage(HttpStatusCode.BadRequest) {Content = new StringContent($"given name is neither image or video")};
            }


            var input = new CloudBlockBlob(
                new Uri($"{storageBaseUrl}/ero/" + name),
                new StorageCredentials(storageName, storageKey));

            if (!input.Exists()) {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            var searchingThumb = new CloudBlockBlob(
                new Uri($"{storageBaseUrl}/thumb-{size}/" + name + ".thumb.jpg"),
                new StorageCredentials(storageName, storageKey));

            if (searchingThumb.Exists()) {
                return new HttpResponseMessage(HttpStatusCode.Redirect) {Headers = {{"Location", searchingThumb.Uri.AbsoluteUri}}};
            }

            /* Generate thumbnails */

            var sm = new CloudBlockBlob(
                new Uri($"{storageBaseUrl}/thumb-sm/" + name + ".thumb.jpg"),
                new StorageCredentials(storageName, storageKey));

            var md = new CloudBlockBlob(
                new Uri($"{storageBaseUrl}/thumb-md/" + name + ".thumb.jpg"),
                new StorageCredentials(storageName, storageKey));

            var lg = new CloudBlockBlob(
                new Uri($"{storageBaseUrl}/thumb-lg/" + name + ".thumb.jpg"),
                new StorageCredentials(storageName, storageKey));


            if (isImage) {
                ImageThumbnailer.ImageThumb(input, sm, md, lg, name, log);
            }
            else if (isVideo) {
                VideoThumbnailer.ThumbVideo(input, sm, md, lg, name, log);
            }

            return new HttpResponseMessage(HttpStatusCode.OK) {Content = new StringContent($"thumbnail for {name} is generating, please try again later.")};
        }
    }
}