// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;
using Microsoft.WindowsAzure.MobileServices.Files.Sync;

namespace Microsoft.WindowsAzure.MobileServices.Files.Operations
{
    public abstract class MobileServiceFileOperation : IMobileServiceFileOperation
    {
        public MobileServiceFileOperation(string id, string fileId)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (fileId == null)
            {
                throw new ArgumentNullException(nameof(fileId));
            }

            Id = id;
            FileId = fileId;
        }

        public string Id { get; }

        public string FileId { get; }

        public FileOperationState State { get; protected set; }

        public abstract FileOperationKind Kind { get; }

        public async Task Execute(IFileMetadataStore metadataStore, IFileSyncContext context)
        {
            try
            {
                this.State = FileOperationState.InProcess;

                await ExecuteOperation(metadataStore, context);
            }
            catch
            {
                this.State = FileOperationState.Failed;
                throw;
            }

            this.State = FileOperationState.Succeeded;
        }

        protected abstract Task ExecuteOperation(IFileMetadataStore metadataStore, IFileSyncContext context);


        public abstract void OnQueueingNewOperation(IMobileServiceFileOperation operation);
    }
}
