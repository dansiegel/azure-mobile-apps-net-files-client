using Microsoft.WindowsAzure.MobileServices.Files;
using Microsoft.WindowsAzure.MobileServices.Files.Eventing;
using Microsoft.WindowsAzure.MobileServices.Files.Operations;
using Moq;
using Xunit;

namespace Microsoft.WindowsAzure.Mobile.Files.Test.Scenarios
{
    [Trait("MobileServiceFileSyncContext: A file is deleted", "")]
    public sealed class DeleteFileScenario : MobileServiceFileSyncContextScenario
    {
        private readonly MobileServiceFile inputFile;

        public DeleteFileScenario()
        {
            this.inputFile = new MobileServiceFile("id", "name", "tableName", "parentId");
            
            SyncContext.DeleteFileAsync(inputFile).Wait();
        }

        [Fact(DisplayName = "The delete file operation is queued with the expected values")]
        private void DeleteFileOperationIsQueuedWithExpectedValues()
        {
            FileOperationQueueMock.Verify(m => m.EnqueueAsync(It.Is<DeleteMobileServiceFileOperation>(o => ValidateOperation(o))), Times.Once());
        }

        private bool ValidateOperation(DeleteMobileServiceFileOperation operation)
        {
            return operation.FileId == this.inputFile.Id &&
                operation.Kind == FileOperationKind.Delete &&
                operation.State == FileOperationState.Pending;
        }

        [Fact(DisplayName = "Delete notification is sent once")]
        public void FileCreationNotificationIsSentOnce()
        {
            EventManagerMock.Verify(m => m.PublishAsync(It.Is<FileOperationCompletedEvent>(e => ValidateNotification(e))), Times.Once());
        }

        private bool ValidateNotification(FileOperationCompletedEvent e)
        {
            return e.Kind == FileOperationKind.Delete &&
                e.Source == FileOperationSource.Local &&
                this.inputFile.Equals(e.File);
        }
    }
}
