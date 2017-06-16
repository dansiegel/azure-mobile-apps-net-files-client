// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Files.Identity;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;

namespace Microsoft.WindowsAzure.MobileServices.Files.StorageProviders
{
    public interface IStorageProvider
    {
        Task DownloadFileToStreamAsync(MobileServiceFile file, Stream stream, StorageToken storageToken);

        Task UploadFileAsync(MobileServiceFileMetadata metadata, IMobileServiceFileDataSource dataSource, StorageToken storageToken);

        Task<Uri> GetFileUriAsync(StorageToken storageToken, string fileName);
    }
}
