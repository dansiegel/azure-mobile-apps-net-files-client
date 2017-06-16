// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;
using Microsoft.WindowsAzure.MobileServices.Files.Operations;

namespace Microsoft.WindowsAzure.MobileServices.Files.Sync
{
    public interface IFileSyncContext
    {
        Task AddFileAsync(MobileServiceFile file);

        Task<bool> QueueOperationAsync(IMobileServiceFileOperation operation);

        Task PushChangesAsync(CancellationToken cancellationToken);

        Task PullFilesAsync(string tableName, string itemId);

        Task DeleteFileAsync(MobileServiceFile file);

        IFileSyncHandler SyncHandler { get; }

        IFileMetadataStore MetadataStore { get; }

        IMobileServiceFilesClient MobileServiceFilesClient { get; }
    }
}
