using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using ImageResizer;
using NReco.VideoConverter;

namespace AzureThumb {
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Linq.Expressions;
    using System.Text.RegularExpressions;
    using Microsoft.WindowsAzure.Storage.Blob;
    using NReco.VideoInfo;
    using static Thumbnailer;
    public static class ThumbnailFunc {
        [FunctionName(nameof(ThumbnailFunc))]
        public static void Run(
            [BlobTrigger("ero/{name}", Connection = "AzureWebJobsStorage")]
            CloudBlockBlob input,
            [Blob("thumbnails/{name}.thumb.jpg", FileAccess.ReadWrite)]
            CloudBlockBlob output,
            string name,
            TraceWriter log) {
            try {
                if (IsImage(name)) {
                    ResizeImage(input, output, name, log);
                }
                else if (IsVideo(name)) {
                    ThumbVideo(input, output, name, log);
                }
                else {
                    log.Info($"Skipping {name}");
                }
            }
            catch(Exception e) {
                log.Error($"error while processing {name}", e);
            }
        }
    }
}
