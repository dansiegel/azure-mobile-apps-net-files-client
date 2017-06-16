// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Files.Identity;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;
using Microsoft.WindowsAzure.MobileServices.Files.Sync;

namespace Microsoft.WindowsAzure.MobileServices.Files
{
    public interface IMobileServiceFilesClient
    {
        Task DeleteFileAsync(MobileServiceFileMetadata metadata);

        Task<IEnumerable<MobileServiceFile>> GetFilesAsync(string tableName, string dataItemId);

        Task<Uri> GetFileUriAsync(MobileServiceFile file, StoragePermissions permissions);

        Task UploadFileAsync(MobileServiceFileMetadata metadata, IMobileServiceFileDataSource dataSource);

        Task DownloadToStreamAsync(MobileServiceFile file, Stream stream);
    }
}
