using System.Collections.Generic;
using System.Threading.Tasks;
using System.Drawing;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureThumb {
    public class Thumbnailer {
        protected static readonly int[] maxLength = {300, 550, 800};

        protected static Size GetCappedSize(int width, int height, int sizeCap) {
            int longer = width > height ? width : height;

            if (longer > sizeCap) {
                int picWidth = (int) ((float) width * sizeCap / longer);
                int picHeight = (int) ((float) height * sizeCap / longer);
                return new Size(picWidth, picHeight);
            }

            return new Size(width, height);
        }

        protected static bool SyncMetadata(IDictionary<string, string> meta, IDictionary<string, string> newMeta) {
            bool isDirty = false;
            foreach (var pair in newMeta) {
                if (!meta.ContainsKey(pair.Key) || meta[pair.Key] != pair.Value) {
                    meta[pair.Key] = pair.Value;
                    isDirty = true;
                }
            }

            return isDirty;
        }

        protected static Task SetMetadata(CloudBlockBlob input,
                                          CloudBlockBlob output,
                                          int inputWidth,
                                          int inputHeight,
                                          int outputWidth,
                                          int outputHeight) {
            var meta1 = input.Metadata;
            var inputDirty = SyncMetadata(meta1, new Dictionary<string, string>
            {
                {"width", inputWidth.ToString()},
                {"height", inputHeight.ToString()},
                //{"thumbWidth", outputWidth.ToString()},
                //{"thumbHeight", outputHeight.ToString()},
                //{"thumbPath", output.Container.Name + "/" + output.Name}
            });
            Task t1 = inputDirty ? input.SetMetadataAsync() : Task.Run(() => { });

            var meta2 = output.Metadata;
            meta2["width"] = outputWidth.ToString();
            meta2["height"] = outputHeight.ToString();

            meta2["originWidth"] = inputWidth.ToString();
            meta2["originHeight"] = inputHeight.ToString();
            meta2["originPath"] = input.Container.Name + "/" + input.Name;
            Task t2 = output.SetMetadataAsync();

            return Task.WhenAll(t1, t2);
        }
    }
}