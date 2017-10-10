using System;

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
