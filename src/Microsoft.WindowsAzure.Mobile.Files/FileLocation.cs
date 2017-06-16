// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.WindowsAzure.MobileServices.Files
{
    [Flags]
    public enum FileLocation
    {
        Local,
        Server,
        LocalAndServer = Local | Server
    }
}
