using System.Collections.Generic;
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
using Microsoft.WindowsAzure.Storage.Queue;

namespace AzureThumb {
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Linq.Expressions;
    using System.Text.RegularExpressions;
    using Microsoft.WindowsAzure.Storage.Blob;

    public static partial class ThumbnailFunc {
        private static string[] availableSizes = {"sm", "md", "lg"};
        private static string defaultSize = "md";
        private static string storageBaseUrl;
        private static string storageName;
        private static string storageKey;

        private static string pendingUri;

        static ThumbnailFunc() {
            var dict = Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process)
                              .Split(';')
                              .Select(s => Regex.Match(s, "(.+?)=(.+)").Groups)
                              .ToDictionary(g => g[1].ToString(), g => g[2].ToString());
            var suffix = dict.ContainsKey("EndpointSuffix") ? dict["EndpointSuffix"] : "core.windows.net";

            storageBaseUrl = $"{dict["DefaultEndpointsProtocol"]}://{dict["AccountName"]}.blob.{suffix}";
            storageName = dict["AccountName"];
            storageKey = dict["AccountKey"];

            var pending = new CloudBlockBlob(
                new Uri($"{storageBaseUrl}/static/pending.jpg"),
                new StorageCredentials(storageName, storageKey));
            pendingUri = pending.Uri.AbsoluteUri;
        }

        [FunctionName("RestThumb")]
        public static HttpResponseMessage RunRest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "thumb")]
            HttpRequestMessage req, 
            [Queue("pending-thumbs", Connection = "AzureWebJobsStorage")]
            CloudQueue pendingQueue,
            TraceWriter log) {
            string name = req.GetQueryNameValuePairs()
                             .FirstOrDefault(q => string.Compare(q.Key, "name", StringComparison.OrdinalIgnoreCase) == 0)
                             .Value;

            string size = req.GetQueryNameValuePairs()
                             .FirstOrDefault(q => string.Compare(q.Key, "size", StringComparison.OrdinalIgnoreCase) == 0)
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
                new Uri($"{storageBaseUrl}/ero/{name}"),
                new StorageCredentials(storageName, storageKey));

            if (!input.Exists()) {
                return new HttpResponseMessage(HttpStatusCode.NotFound) {Content = new StringContent($"doesn't exist:\n{storageBaseUrl}/ero/{name}")};
            }

            var searchingThumb = new CloudBlockBlob(
                new Uri($"{storageBaseUrl}/thumb-{size}/" + name + ".thumb.jpg"),
                new StorageCredentials(storageName, storageKey));

            if (searchingThumb.Exists()) {
                return new HttpResponseMessage(HttpStatusCode.Redirect) {Headers = {{"Location", searchingThumb.Uri.AbsoluteUri}}};
            }

            /* Generate thumbnails */

            pendingQueue.AddMessage(new CloudQueueMessage(name),options: new QueueRequestOptions());

            return new HttpResponseMessage(HttpStatusCode.Redirect) {
                Headers = {{"Location", pendingUri}},
                Content = new StringContent($"thumbnail for {name} is generating, please try again later.")
            };
            return new HttpResponseMessage(HttpStatusCode.OK) {Content = new StringContent($"thumbnail for {name} is generating, please try again later.")};
        }


        [FunctionName("QueueThumb")]
        public static async Task QueueThumb(
            [QueueTrigger("pending-thumbs", Connection = "AzureWebJobsStorage")]
            string pendingName,
            TraceWriter log) {
            var name = pendingName;

            var input = new CloudBlockBlob(
                new Uri($"{storageBaseUrl}/ero/{name}"),
                new StorageCredentials(storageName, storageKey));


            var sm = new CloudBlockBlob(
                new Uri($"{storageBaseUrl}/thumb-sm/" + name + ".thumb.jpg"),
                new StorageCredentials(storageName, storageKey));

            var md = new CloudBlockBlob(
                new Uri($"{storageBaseUrl}/thumb-md/" + name + ".thumb.jpg"),
                new StorageCredentials(storageName, storageKey));

            var lg = new CloudBlockBlob(
                new Uri($"{storageBaseUrl}/thumb-lg/" + name + ".thumb.jpg"),
                new StorageCredentials(storageName, storageKey));

            if (sm.Exists() && md.Exists() && lg.Exists()) {
                return;
            } 

            bool isImage = IsImage(name), isVideo = IsVideo(name);
            if (isImage) {
                await ImageThumbnailer.ImageThumb(input, sm, md, lg, name, log);
            }
            else if (isVideo) {
                await VideoThumbnailer.ThumbVideo(input, sm, md, lg, name, log);
            }
        }
    }
}