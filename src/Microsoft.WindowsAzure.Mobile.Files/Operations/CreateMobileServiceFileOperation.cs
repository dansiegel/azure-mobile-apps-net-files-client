// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;
using Microsoft.WindowsAzure.MobileServices;
using Microsoft.WindowsAzure.MobileServices.Files.Sync;

namespace Microsoft.WindowsAzure.MobileServices.Files.Operations
{
    public class CreateMobileServiceFileOperation : MobileServiceFileOperation
    {
        public CreateMobileServiceFileOperation(string id, string fileId)
            : base(id, fileId)
        {
        }

        public override FileOperationKind Kind
        {
            get
            {
                return FileOperationKind.Create;
            }
        }

        protected async override Task ExecuteOperation(IFileMetadataStore metadataStore, IFileSyncContext context)
        {
            MobileServiceFileMetadata metadata = await metadataStore.GetFileMetadataAsync(FileId);

            if (metadata != null)
            {
                IMobileServiceFileDataSource dataSource = await context.SyncHandler.GetDataSource(metadata);
                
                await context.MobileServiceFilesClient.UploadFileAsync(metadata, dataSource);

                await metadataStore.CreateOrUpdateAsync(metadata);
            }
        }

        public override void OnQueueingNewOperation(IMobileServiceFileOperation operation)
        {
        }
    }
}
