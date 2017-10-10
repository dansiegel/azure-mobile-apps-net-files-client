using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.MobileServices.Files.Operations
{
    public interface IFileOperationQueue
    {
        int Count { get; }

        Task<IMobileServiceFileOperation> PeekAsync();

        Task<IMobileServiceFileOperation> DequeueAsync();

        Task RemoveAsync(string id);

        Task<IMobileServiceFileOperation> GetOperationByFileIdAsync(string fileId);

        Task EnqueueAsync(IMobileServiceFileOperation operation);
    }
}
