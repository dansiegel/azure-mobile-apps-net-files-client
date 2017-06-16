// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;

namespace Microsoft.WindowsAzure.MobileServices.Files.Sync
{
    public interface IFileSyncHandler
    {
        /// <summary>
        /// Gets the data source that will be used to retrieve the file data.
        /// </summary>
        /// <param name="metadata">A <see cref="MobileServiceFileMetadata"/> instance describing the target file.</param>
        /// <returns>A <see cref="IMobileServiceFileDataSource"/> that will be used to retrieve the file data.</returns>
        Task<IMobileServiceFileDataSource> GetDataSource(MobileServiceFileMetadata metadata);

        /// <summary>
        /// Invoked when, as a result of a synchronization operation, a file is created, updtaded or deleted.
        /// </summary>
        /// <param name="file">The <see cref="MobileServiceFile"/>.</param>
        /// <returns>A <see cref="Task"/> that is completed when the new file is processed.</returns>
        Task ProcessFileSynchronizationAction(MobileServiceFile file, FileSynchronizationAction action);
    }

    public enum FileSynchronizationAction
    {
        Create,
        Update,
        Delete
    }
}
