﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.MobileServices.Files.Metadata
{
    public interface IFileMetadataStore
    {
        Task CreateOrUpdateAsync(MobileServiceFileMetadata metadata);

        Task<MobileServiceFileMetadata> GetFileMetadataAsync(string fileId);

        Task DeleteAsync(MobileServiceFileMetadata metadata);

        Task<IEnumerable<MobileServiceFileMetadata>> GetMetadataAsync(string tableName, string objectId);

        Task PurgeAsync(string tableName);

        Task PurgeAsync(string tableName, string itemId);
    }
}
