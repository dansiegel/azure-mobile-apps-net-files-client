// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.WindowsAzure.MobileServices.Files.Sync.Triggers
{
    internal sealed class DefaultFileSyncTriggerFactory : IFileSyncTriggerFactory
    {
        private readonly bool autoUpdateRecords;
        private readonly IMobileServiceClient mobileServiceClient;

        public DefaultFileSyncTriggerFactory(IMobileServiceClient mobileServiceClient, bool autoUpdateParentRecords)
        {
            if (mobileServiceClient == null)
            {
                throw new ArgumentNullException("mobileServiceClient");
            }

            this.mobileServiceClient = mobileServiceClient;
            this.autoUpdateRecords = autoUpdateParentRecords;
        }

        public IList<IFileSyncTrigger> CreateTriggers(IFileSyncContext fileSyncContext)
        {
            return new List<IFileSyncTrigger> { new EntityDataFileSyncTrigger(fileSyncContext, this.mobileServiceClient, this.autoUpdateRecords) };
        }
    }
}
