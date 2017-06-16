// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;
using Microsoft.WindowsAzure.MobileServices.Files.Operations;
using Microsoft.WindowsAzure.MobileServices.Files.Sync;
using Microsoft.WindowsAzure.MobileServices.Files.Sync.Triggers;
using Microsoft.WindowsAzure.MobileServices.Sync;

namespace Microsoft.WindowsAzure.MobileServices.Files
{
    public static class MobileServiceClientExtensions
    {
        private readonly static Dictionary<IMobileServiceClient, IFileSyncContext> contexts = new Dictionary<IMobileServiceClient, IFileSyncContext>();
        private readonly static object contextsSyncRoot = new object();

        public static IFileSyncContext InitializeFileSyncContext(this IMobileServiceClient client, IFileSyncHandler syncHandler)
        {
            if (!client.SyncContext.IsInitialized)
            {
                throw new InvalidOperationException(@"The file sync context cannot be initialized without a MobileServiceLocalStore if offline sync has not been initialized. 
Please initialize offline sync by invoking InializeAsync on the sync context or provide an IMobileServiceLocalStore instance to the InitializeFileSyncContext method.");
            }

            return InitializeFileSyncContext(client, syncHandler, client.SyncContext.Store);
        }

        public static IFileSyncContext InitializeFileSyncContext(this IMobileServiceClient client, IFileSyncHandler syncHandler, IMobileServiceLocalStore store)
        {
            return InitializeFileSyncContext(client, syncHandler, store, new DefaultFileSyncTriggerFactory(client, true));
        }

        public static IFileSyncContext InitializeFileSyncContext(this IMobileServiceClient client, IFileSyncHandler syncHandler, 
            IMobileServiceLocalStore store, IFileSyncTriggerFactory fileSyncTriggerFactory)
        {
            lock (contextsSyncRoot)
            {
                IFileSyncContext context;

                if (!contexts.TryGetValue(client, out context))
                {
                    context = new MobileServiceFileSyncContext(client, new FileMetadataStore(store), new FileOperationQueue(store), fileSyncTriggerFactory, syncHandler);
                    contexts.Add(client, context);
                }

                return context;
            }
        }

        public static IFileSyncContext GetFileSyncContext(this IMobileServiceClient client)
        {
            IFileSyncContext context;
            if (!contexts.TryGetValue(client, out context))
            {
                if (!client.SyncContext.IsInitialized)
                {
                    throw new InvalidOperationException("The file sync context has not been initialized. Pleae initialize the context by invoking InitializeFileAsync.");
                }
            }

            return context;
        }
    }
}
