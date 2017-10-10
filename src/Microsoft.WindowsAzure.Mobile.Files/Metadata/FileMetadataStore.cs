using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Query;
using Microsoft.WindowsAzure.MobileServices.Sync;
using Newtonsoft.Json.Linq;

namespace Microsoft.WindowsAzure.MobileServices.Files.Metadata
{
    public class FileMetadataStore : IFileMetadataStore
    {
        public const string FileMetadataTableName = "__filesmetadata";

        private IMobileServiceLocalStore store;

        public FileMetadataStore(IMobileServiceLocalStore store)
        {
            this.store = store;
            DefineTable(store);
        }

        private static void DefineTable(IMobileServiceLocalStore store)
        {
            var tableLocalStore = store as MobileServiceLocalStore;

            if (tableLocalStore == null)
            {
                return;
            }

            tableLocalStore.DefineTable(FileMetadataTableName, new JObject()
            {
                { MobileServiceSystemColumns.Id, String.Empty },
                { "fileId", string.Empty },
                { "fileName", string.Empty },
                { "length", 0 },
                { "contentMD5", string.Empty },
                { "localPath", string.Empty },
                { "location", FileLocation.Local.ToString() },
                { "lastModified", string.Empty },
                { "parentDataItemType", string.Empty },
                { "parentDataItemId", string.Empty },
                { "pendingDeletion", false },
                { "fileStoreUri", string.Empty },
                { "metadata", string.Empty }
            });
        }

        public async Task CreateOrUpdateAsync(MobileServiceFileMetadata metadata)
        {
            JObject jsonObject = JObject.FromObject(metadata);
            await this.store.UpsertAsync(FileMetadataTableName, new[] { jsonObject }, true);
        }

        public async Task<MobileServiceFileMetadata> GetFileMetadataAsync(string fileId)
        {
            JObject metadata = await this.store.LookupAsync(FileMetadataTableName, fileId);

            if (metadata != null)
            {
                return metadata.ToObject<MobileServiceFileMetadata>();
            }

            return null;
        }

        public async Task DeleteAsync(MobileServiceFileMetadata metadata)
        {
            await this.store.DeleteAsync(FileMetadataTableName, new[] { metadata.Id });
        }

        public async Task<IEnumerable<MobileServiceFileMetadata>> GetMetadataAsync(string tableName, string objectId)
        {
            var query = MobileServiceTableQueryDescription.Parse(FileMetadataTableName, string.Format("$filter=parentDataItemType eq '{0}' and parentDataItemId eq '{1}'", tableName, objectId));

            var result = await this.store.ReadAsync(query);

            return result.ToObject<List<MobileServiceFileMetadata>>();
        }


        public async Task PurgeAsync(string tableName)
        {
            await PurgeAsync(tableName, null);
        }

        public async Task PurgeAsync(string tableName, string itemId)
        {
            string queryString = string.Format("$filter=parentDataItemType eq '{0}'", tableName);

            if (!string.IsNullOrEmpty(itemId))
            {
                queryString += string.Format(" and parentDataItemId eq '{0}'", itemId);
            }

            var query = MobileServiceTableQueryDescription.Parse(FileMetadataTableName, queryString);
            await this.store.DeleteAsync(query);
        }
    }
}
