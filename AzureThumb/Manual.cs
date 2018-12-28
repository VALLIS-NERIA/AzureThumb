using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace AzureThumb
{
    using System.IO;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using static Thumbnailer;

    public static class Manual
    {
        public static void Do(
            [Blob("ero/{name}", Connection = "AzureWebJobsStorage")]
            CloudBlockBlob input,
            [Blob("thumbnails/{name}", FileAccess.ReadWrite)]
            CloudBlockBlob output,
            string name,
            TraceWriter log) {
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

        //[FunctionName("Manual")]
        public static void Run([TimerTrigger("5 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                "DefaultEndpointsProtocol=https;AccountName=backupstroage;AccountKey=gH3avMwxeBmvtfN3W379tpPyeECwH3EfuDQVQNUFWY2iBH6n8JwbEfyc+i/L7gLAwn4yek7EbKZwCIDykVYpkg==;BlobEndpoint=https://backupstroage.blob.core.windows.net/;TableEndpoint=https://backupstroage.table.core.windows.net/;QueueEndpoint=https://backupstroage.queue.core.windows.net/;FileEndpoint=https://backupstroage.file.core.windows.net/");
            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference("contiainername");

            // Loop over items within the container and output the content, length and URI.
            foreach (IListBlobItem item in container.ListBlobs(null, false)) {
                if (item.GetType() == typeof(CloudBlockBlob)) {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    
                }
                else if (item.GetType() == typeof(CloudBlobDirectory)) {
                    CloudBlobDirectory directory = (CloudBlobDirectory)item;
                    Getblobcontent(directory);
                    Console.WriteLine("Directory: {0}", directory.Uri);
                }
            }



            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
        }


        private static void Getblobcontent(CloudBlobDirectory container) {
            foreach (IListBlobItem item in container.ListBlobs()) {
                if (item.GetType() == typeof(CloudBlockBlob)) {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    //int this method you could get the blob content in the directory

                    string text;
                    using (var memoryStream = new MemoryStream()) {
                        blob.DownloadToStream(memoryStream);

                        //we get the content from the blob
                        //sine in my blob this is txt file,
                        text = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
                    }
                    Console.WriteLine("Block blob of length {0}: {1}", blob.Properties.Length, blob.Uri);

                    Console.WriteLine(text);

                    Console.WriteLine("Block blob of length {0}: {1}", blob.Properties.Length, blob.Uri);

                }
                else if (item.GetType() == typeof(CloudPageBlob)) {
                    CloudPageBlob pageBlob = (CloudPageBlob)item;
                    //int this method you could get the blob content

                    Console.WriteLine("Page blob of length {0}: {1}", pageBlob.Properties.Length, pageBlob.Uri);

                }
                else if (item.GetType() == typeof(CloudBlobDirectory)) {
                    CloudBlobDirectory directory = (CloudBlobDirectory)item;
                    Getblobcontent(directory);

                    Console.WriteLine("Directory: {0}", directory.Uri);
                }
            }
        }
    }
}
