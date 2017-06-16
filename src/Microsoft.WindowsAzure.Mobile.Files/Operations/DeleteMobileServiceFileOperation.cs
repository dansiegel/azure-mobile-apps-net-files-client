// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;
using Microsoft.WindowsAzure.MobileServices.Files.Sync;

namespace Microsoft.WindowsAzure.MobileServices.Files.Operations
{
    public sealed class DeleteMobileServiceFileOperation : MobileServiceFileOperation
    {
        public DeleteMobileServiceFileOperation(string id, string fileId)
            : base(id, fileId)
        {
        }

        public override FileOperationKind Kind
        {
            get
            {
                return FileOperationKind.Delete;
            }
        }

        protected async override Task ExecuteOperation(IFileMetadataStore metadataStore, IFileSyncContext context)
        {
            MobileServiceFileMetadata metadata = await metadataStore.GetFileMetadataAsync(FileId);

            if (metadata != null)
            {
                await metadataStore.DeleteAsync(metadata);

                await context.MobileServiceFilesClient.DeleteFileAsync(metadata);
            }
        }

        public override void OnQueueingNewOperation(IMobileServiceFileOperation operation)
        {
            //
        }
    }
}
