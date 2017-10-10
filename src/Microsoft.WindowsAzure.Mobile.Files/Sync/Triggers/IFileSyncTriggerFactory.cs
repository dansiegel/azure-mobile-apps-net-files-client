using System.Collections.Generic;

namespace Microsoft.WindowsAzure.MobileServices.Files.Sync.Triggers
{
    public interface IFileSyncTriggerFactory
    {
        IList<IFileSyncTrigger> CreateTriggers(IFileSyncContext fileSyncContext);
    }
}
