// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;
using Microsoft.WindowsAzure.MobileServices.Files.Sync;

namespace Microsoft.WindowsAzure.MobileServices.Files.Operations
{
    public interface IMobileServiceFileOperation
    {
        string Id { get; }

        string FileId { get; }

        FileOperationKind Kind { get; }

        FileOperationState State { get; }

        Task Execute(IFileMetadataStore metadataStore, IFileSyncContext context);

        void OnQueueingNewOperation(IMobileServiceFileOperation operation);
    }
}
