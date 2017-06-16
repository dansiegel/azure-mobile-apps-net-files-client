// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using Microsoft.WindowsAzure.MobileServices.Files.Eventing;
using Microsoft.WindowsAzure.MobileServices.Sync;
using Newtonsoft.Json.Linq;

namespace Microsoft.WindowsAzure.MobileServices.Files.Sync.Triggers
{
    public sealed class EntityDataFileSyncTrigger : IFileSyncTrigger, IDisposable
    {
        private readonly IDisposable dataChangeNotificationSubscription;
        private readonly IDisposable fileChangeNotificationSubscription;
        private readonly IFileSyncContext fileSyncContext;
        private readonly IMobileServiceClient mobileServiceClient;

        public EntityDataFileSyncTrigger(IFileSyncContext fileSyncContext, IMobileServiceClient mobileServiceClient, bool autoUpdateParentRecords)
        {
            if (fileSyncContext == null)
            {
                throw new ArgumentNullException("fileSyncContext");
            }

            if (mobileServiceClient == null)
            {
                throw new ArgumentNullException("mobileServiceClient");
            }

            this.fileSyncContext = fileSyncContext;
            this.mobileServiceClient = mobileServiceClient;

            this.dataChangeNotificationSubscription = mobileServiceClient.EventManager.Subscribe<StoreOperationCompletedEvent>(OnStoreOperationCompleted);

            if (autoUpdateParentRecords)
            {
                this.fileChangeNotificationSubscription = mobileServiceClient.EventManager.Subscribe<FileOperationCompletedEvent>(OnFileOperationCompleted);
            }
        }

        ~EntityDataFileSyncTrigger()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.dataChangeNotificationSubscription != null)
                {
                    this.dataChangeNotificationSubscription.Dispose();
                }

                if (this.fileChangeNotificationSubscription != null)
                {
                    this.fileChangeNotificationSubscription.Dispose();
                }
            }
        }

        private async void OnFileOperationCompleted(FileOperationCompletedEvent obj)
        {
            if (obj.Source == FileOperationSource.Local)
            {
                IMobileServiceSyncTable table = this.mobileServiceClient.GetSyncTable(obj.File.TableName);
                JObject item = await table.LookupAsync(obj.File.ParentId);

                if (item != null)
                {
                    await table.UpdateAsync(item);
                }
            }
        }

        private async void OnStoreOperationCompleted(StoreOperationCompletedEvent storeOperationEvent)
        {
            switch (storeOperationEvent.Operation.Kind)
            {
                case LocalStoreOperationKind.Insert:
                case LocalStoreOperationKind.Update:
                case LocalStoreOperationKind.Upsert:
                    if (storeOperationEvent.Operation.Source == StoreOperationSource.ServerPull
                        || storeOperationEvent.Operation.Source == StoreOperationSource.ServerPush)
                    {
                        await this.fileSyncContext.PullFilesAsync(storeOperationEvent.Operation.TableName, storeOperationEvent.Operation.RecordId);                        
                    }
                    break;
                case LocalStoreOperationKind.Delete:
                    await this.fileSyncContext.MetadataStore.PurgeAsync(storeOperationEvent.Operation.TableName, storeOperationEvent.Operation.RecordId);
                    break;
                default:
                    break;
            }
        }
    }
}
