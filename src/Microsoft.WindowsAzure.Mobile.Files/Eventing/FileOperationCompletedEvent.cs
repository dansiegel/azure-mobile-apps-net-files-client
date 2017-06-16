// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Eventing;

namespace Microsoft.WindowsAzure.MobileServices.Files.Eventing
{
    public sealed class FileOperationCompletedEvent : IMobileServiceEvent
    {
        public const string EventName = "MobileServices.FileOperationCompleted";

        public FileOperationCompletedEvent(MobileServiceFile file, FileOperationKind operationKind, FileOperationSource source)
        {
            File = file;
            Kind = operationKind;
            Source = source;
        }

        public string Name
        {
            get { return EventName; }
        }


        public FileOperationKind Kind { get; private set; }

        public MobileServiceFile File { get; private set; }

        public FileOperationSource Source { get; private set; }
    }
}
