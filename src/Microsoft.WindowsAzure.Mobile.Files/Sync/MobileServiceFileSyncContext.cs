// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Eventing;
using Microsoft.WindowsAzure.MobileServices.Files.Eventing;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;
using Microsoft.WindowsAzure.MobileServices.Files.Operations;
using Microsoft.WindowsAzure.MobileServices.Files.StorageProviders;
using Microsoft.WindowsAzure.MobileServices.Files.Sync;
using Microsoft.WindowsAzure.MobileServices.Files.Sync.Triggers;

namespace Microsoft.WindowsAzure.MobileServices.Files
{
    public class MobileServiceFileSyncContext : IFileSyncContext, IDisposable
    {
        private readonly IFileOperationQueue operationsQueue;
        private readonly IMobileServiceFilesClient mobileServiceFilesClient;
        private readonly IFileMetadataStore metadataStore;
        private readonly SemaphoreSlim processingSemaphore = new SemaphoreSlim(1);
        private readonly IFileSyncHandler syncHandler;
        private readonly IMobileServiceEventManager eventManager;
        private bool disposed = false;
        private readonly IList<IFileSyncTrigger> triggers;

        public MobileServiceFileSyncContext(IMobileServiceClient client, IFileMetadataStore metadataStore, IFileOperationQueue operationsQueue,
            IFileSyncTriggerFactory syncTriggerFactory, IFileSyncHandler syncHandler)
            : this(client, metadataStore, operationsQueue, syncTriggerFactory, syncHandler, null)
        { }

        internal MobileServiceFileSyncContext(IMobileServiceClient client, IFileMetadataStore metadataStore, IFileOperationQueue operationsQueue, 
            IFileSyncTriggerFactory syncTriggerFactory, IFileSyncHandler syncHandler, IMobileServiceFilesClient filesClient)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            if (metadataStore == null)
            {
                throw new ArgumentNullException("metadataStore");
            }

            if (operationsQueue == null)
            {
                throw new ArgumentNullException("operationsQueue");
            }

            if (syncTriggerFactory == null)
            {
                throw new ArgumentNullException("syncTriggerFactory");
            }

            if (syncHandler == null)
            {
                throw new ArgumentNullException("syncHandler");
            }

            this.metadataStore = metadataStore;
            this.syncHandler = syncHandler;
            this.operationsQueue = operationsQueue;
            this.mobileServiceFilesClient = filesClient ?? new MobileServiceFilesClient(client, new AzureBlobStorageProvider(client));
            this.eventManager = client.EventManager;
            this.triggers = syncTriggerFactory.CreateTriggers(this);
        }

        internal void NotifyFileOperationCompletion(MobileServiceFile file, FileOperationKind fileOperationKind, FileOperationSource source)
        {
            var operationCompletedEvent = new FileOperationCompletedEvent(file, fileOperationKind, source);

            this.eventManager.PublishAsync(operationCompletedEvent).ContinueWith(t => t.Exception.Handle(e => true), TaskContinuationOptions.OnlyOnFaulted);
        }

        public async Task AddFileAsync(MobileServiceFile file)
        {
            var metadata = new MobileServiceFileMetadata
            {
                FileId = file.Id,
                FileName = file.Name,
                Length = file.Length,
                Location = FileLocation.Local,
                ContentMD5 = file.ContentMD5,
                ParentDataItemType = file.TableName,
                ParentDataItemId = file.ParentId
            };

            await metadataStore.CreateOrUpdateAsync(metadata);

            var operation = new CreateMobileServiceFileOperation(Guid.NewGuid().ToString(), file.Id);

            await QueueOperationAsync(operation);

            NotifyFileOperationCompletion(file, FileOperationKind.Create, FileOperationSource.Local);
        }

        public async Task DeleteFileAsync(MobileServiceFile file)
        {
            var operation = new DeleteMobileServiceFileOperation(Guid.NewGuid().ToString(), file.Id);

            await QueueOperationAsync(operation);

            NotifyFileOperationCompletion(file, FileOperationKind.Delete, FileOperationSource.Local);
        }

        public async Task PushChangesAsync(CancellationToken cancellationToken)
        {
            await processingSemaphore.WaitAsync(cancellationToken);
            try
            {
                while (this.operationsQueue.Count > 0)
                {
                    IMobileServiceFileOperation operation = await operationsQueue.PeekAsync();

                    // This would also take the cancellation token
                    await operation.Execute(this.metadataStore, this);

                    await operationsQueue.RemoveAsync(operation.Id);
                }
            }
            finally
            {
                processingSemaphore.Release();
            }
        }

        public async Task PullFilesAsync(string tableName, string itemId)
        {
            IEnumerable<MobileServiceFile> files = await this.mobileServiceFilesClient.GetFilesAsync(tableName, itemId);

            foreach (var file in files)
            {
                FileSynchronizationAction syncAction = FileSynchronizationAction.Update;

                MobileServiceFileMetadata metadata = await this.metadataStore.GetFileMetadataAsync(file.Id);

                if (metadata == null)
                {
                    syncAction = FileSynchronizationAction.Create;

                    metadata = MobileServiceFileMetadata.FromFile(file);

                    metadata.ContentMD5 = null;
                    metadata.LastModified = null;
                }

                if (string.Compare(metadata.ContentMD5, file.ContentMD5, StringComparison.Ordinal) != 0 ||
                    (metadata.LastModified == null || metadata.LastModified.Value.ToUniversalTime() != file.LastModified.Value.ToUniversalTime()))
                {
                    metadata.LastModified = file.LastModified;
                    metadata.ContentMD5 = file.ContentMD5;

                    await this.metadataStore.CreateOrUpdateAsync(metadata);
                    await this.syncHandler.ProcessFileSynchronizationAction(file, syncAction);

                    NotifyFileOperationCompletion(file, syncAction.ToFileOperationKind(), FileOperationSource.ServerPull);
                }
            }

            var fileMetadata = await this.metadataStore.GetMetadataAsync(tableName, itemId);
            var deletedItemsMetadata = fileMetadata.Where(m => !files.Any(f => string.Compare(f.Id, m.FileId) == 0));

            foreach (var metadata in deletedItemsMetadata)
            {
                IMobileServiceFileOperation pendingOperation = await this.operationsQueue.GetOperationByFileIdAsync(metadata.FileId);

                // TODO: Need to call into the sync handler for conflict resolution here...
                if (pendingOperation == null || pendingOperation is DeleteMobileServiceFileOperation)
                {
                    await metadataStore.DeleteAsync(metadata);

                    await this.syncHandler.ProcessFileSynchronizationAction(MobileServiceFile.FromMetadata(metadata), FileSynchronizationAction.Delete);

                    NotifyFileOperationCompletion(MobileServiceFile.FromMetadata(metadata), FileOperationKind.Delete, FileOperationSource.ServerPull);
                }
            }
        }

        public async Task<bool> QueueOperationAsync(IMobileServiceFileOperation operation)
        {
            bool operationEnqueued = false;

            await processingSemaphore.WaitAsync();

            try
            {
                var pendingItemOperation = await this.operationsQueue.GetOperationByFileIdAsync(operation.FileId);

                if (pendingItemOperation != null)
                {
                    pendingItemOperation.OnQueueingNewOperation(operation);

                    if (pendingItemOperation.State == FileOperationState.Cancelled)
                    {
                        await this.operationsQueue.RemoveAsync(pendingItemOperation.Id);
                    }
                }

                if (operation.State != FileOperationState.Cancelled)
                {
                    await this.operationsQueue.EnqueueAsync(operation);
                    operationEnqueued = true;
                }

            }
            finally
            {
                processingSemaphore.Release();
            }

            return operationEnqueued;
        }

        public IMobileServiceFilesClient MobileServiceFilesClient
        {
            get { return this.mobileServiceFilesClient; }
        }

        public IFileSyncHandler SyncHandler
        {
            get { return this.syncHandler; }
        }

        public IFileMetadataStore MetadataStore
        {
            get { return this.metadataStore; }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                foreach (var trigger in triggers.OfType<IDisposable>())
                {
                    trigger.Dispose();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }

    internal static class FileSynchronizationActionExtensions
    {
        public static FileOperationKind ToFileOperationKind(this FileSynchronizationAction synchronizationAction)
        {
            switch (synchronizationAction)
            {
                case FileSynchronizationAction.Create:
                    return FileOperationKind.Create;
                case FileSynchronizationAction.Update:
                    return FileOperationKind.Update;
                case FileSynchronizationAction.Delete:
                    return FileOperationKind.Delete;
                default:
                    throw new InvalidOperationException("Unknown FileSynchronizationAction value.");
            }
        }
    }

}
