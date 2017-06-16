using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices;
using Microsoft.WindowsAzure.MobileServices.Eventing;
using Microsoft.WindowsAzure.MobileServices.Files;
using Microsoft.WindowsAzure.MobileServices.Files.Eventing;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;
using Microsoft.WindowsAzure.MobileServices.Files.Operations;
using Microsoft.WindowsAzure.MobileServices.Files.Sync;
using Moq;
using Xunit;

namespace Microsoft.WindowsAzure.Mobile.Files.Test.Scenarios
{
    [Trait("MobileServiceFileSyncContext: A new file is added", "")]
    public sealed class AddNewFileScenario : MobileServiceFileSyncContextScenario
    {
        private readonly MobileServiceFile inputFile;

        public AddNewFileScenario()
        {
            this.inputFile = new MobileServiceFile("id", "name", "tableName", "parentId");
            this.inputFile.ContentMD5 = "md5";
            this.inputFile.Length = 12345;
            this.inputFile.LastModified = new DateTimeOffset(new DateTime(2015, 1, 1, 1, 1, 1));

            SyncContext.AddFileAsync(inputFile).Wait();
        }

        [Fact(DisplayName = "File metadata is created with expected values")]
        public void FileMetadataIsCreatedWithExpectedPropertyValues()
        {
            FileMetadataStoreMock.Verify(s => s.CreateOrUpdateAsync(It.Is<MobileServiceFileMetadata>(m => IsMetadataValid(m))), Times.Once());
        }

        private bool IsMetadataValid(MobileServiceFileMetadata metadata)
        {
            return inputFile.Id == metadata.Id &&
            string.Compare(inputFile.Name, metadata.FileName) == 0 &&
            string.Compare(inputFile.ParentId, metadata.ParentDataItemId) == 0 &&
            string.Compare(inputFile.TableName, metadata.ParentDataItemType) == 0 &&
            string.Compare(inputFile.ContentMD5, metadata.ContentMD5) == 0 &&
            inputFile.Length == metadata.Length;
        }

        [Fact(DisplayName = "File creation notification is sent once")]
        public void FileCreationNotificationIsSentOnce()
        {
            EventManagerMock.Verify(m => m.PublishAsync(It.Is<FileOperationCompletedEvent>(e => IsNotificationValid(e))), Times.Once());
        }

        private bool IsNotificationValid(FileOperationCompletedEvent e)
        {
            return e.Kind == FileOperationKind.Create &&
                e.Source == FileOperationSource.Local &&
                this.inputFile.Equals(e.File);
        }

        [Fact(DisplayName = "The create file operation is queued with the expected values")]
        private void CreateFileOperationIsQueuedWithExpectedValues()
        {
            FileOperationQueueMock.Verify(m => m.EnqueueAsync(It.Is<CreateMobileServiceFileOperation>(o => IsOperationValid(o))), Times.Once());
        }

        private bool IsOperationValid(CreateMobileServiceFileOperation operation)
        {
            return operation.FileId == this.inputFile.Id &&
                operation.Kind == FileOperationKind.Create &&
                operation.State == FileOperationState.Pending;
        }
    }
}
