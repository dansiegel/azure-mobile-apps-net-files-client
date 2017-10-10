using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.WindowsAzure.MobileServices.Files.Metadata
{
    public class MobileServiceFileMetadata
    {
        public string Id
        {
            get { return this.FileId; }
            set { this.FileId = value; }
        }

        public string FileId { get; set; }

        public string FileName { get; set; }

        public long Length { get; set; }

        public string ContentMD5 { get; set; }

        public FileLocation Location { get; set; }

        public DateTimeOffset? LastModified { get; set; }

        public string ParentDataItemType { get; set; }

        public string ParentDataItemId { get; set; }

        public bool PendingDeletion { get; set; }

        public string FileStoreUri { get; set; }

        public string Metadata { get; set; }

        public static MobileServiceFileMetadata FromFile(MobileServiceFile file)
        {
            return new MobileServiceFileMetadata
            {
                FileId = file.Id,
                FileName = file.Name,
                ContentMD5 = file.ContentMD5,
                LastModified = file.LastModified,
                Length = file.Length,
                ParentDataItemId = file.ParentId,
                ParentDataItemType = file.TableName,
                FileStoreUri = file.StoreUri,
                PendingDeletion = false,
                Metadata = file.Metadata != null ? JsonConvert.SerializeObject(file.Metadata) : null
            };
        }

        internal IDictionary<string, string> ToDictionary()
        {
            if (this.Metadata == null)
            {
                return null;
            }

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(this.Metadata);
        }
    }
}
