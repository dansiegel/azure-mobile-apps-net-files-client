// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.WindowsAzure.MobileServices.Files
{
    public enum FileOperationState
    {
        Pending = 0,
        InProcess = 1,
        Succeeded = 2,
        Failed = 3,
        Cancelled = 4
    }
}
