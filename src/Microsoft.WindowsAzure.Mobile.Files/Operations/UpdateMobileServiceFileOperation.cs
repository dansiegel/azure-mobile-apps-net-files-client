using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;
using Microsoft.WindowsAzure.MobileServices.Files.Sync;

namespace Microsoft.WindowsAzure.MobileServices.Files.Operations
{
    public class UpdateMobileServiceFileOperation : MobileServiceFileOperation
    {
        public UpdateMobileServiceFileOperation(string id, string fileId)
            : base(id, fileId)
        {
        }

        public override FileOperationKind Kind
        {
            get
            {
                return FileOperationKind.Update;
            }
        }

        protected async override Task ExecuteOperation(IFileMetadataStore metadataStore, IFileSyncContext context)
        {
            MobileServiceFileMetadata metadata = await metadataStore.GetFileMetadataAsync(FileId);

            if (metadata != null)
            {
                var dataSource = await context.SyncHandler.GetDataSource(metadata);

                await context.MobileServiceFilesClient.UploadFileAsync(metadata, dataSource);
            }
        }

        public override void OnQueueingNewOperation(IMobileServiceFileOperation operation)
        {

        }
    }
}
