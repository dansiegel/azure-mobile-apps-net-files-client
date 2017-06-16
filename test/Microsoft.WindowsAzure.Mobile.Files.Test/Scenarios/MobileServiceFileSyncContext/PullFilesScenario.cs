using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Files;
using Microsoft.WindowsAzure.MobileServices.Files.Eventing;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;
using Microsoft.WindowsAzure.MobileServices.Files.Sync;
using Moq;
using Xunit;

namespace Microsoft.WindowsAzure.Mobile.Files.Test.Scenarios
{
    [Trait("MobileServiceFileSyncContext: Pull with unchanged, new, updated and deleted files.", "")]
    public sealed class PullFilesScenario : MobileServiceFileSyncContextScenario
    {
        private string testTableName = "testtable";
        private string testRecordId = "id";
        private List<MobileServiceFileMetadata> testMetadata;
        private List<MobileServiceFile> testFiles;

        public PullFilesScenario()
        {
            initializeInput();

            FilesClientMock.Setup(m => m.GetFilesAsync(It.Is<string>(s => string.Compare(this.testTableName, s) == 0),
                It.Is<string>(s => string.Compare(this.testRecordId, s) == 0)))
                .Returns(() => Task.FromResult<IEnumerable<MobileServiceFile>>(testFiles));

            FileMetadataStoreMock.Setup(m => m.GetFileMetadataAsync(It.IsAny<string>()))
                .Returns<string>(id => Task.FromResult(testMetadata.FirstOrDefault(m => string.Compare(m.FileId, id) == 0)));

            FileMetadataStoreMock.Setup(m => m.GetMetadataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((table, id) => Task.FromResult<IEnumerable<MobileServiceFileMetadata>>(testMetadata.ToList()));

            FileMetadataStoreMock.Setup(m => m.CreateOrUpdateAsync(It.IsAny<MobileServiceFileMetadata>()))
                .Callback<MobileServiceFileMetadata>(m =>
                {
                    if (!testMetadata.Any(tm => string.Compare(tm.FileId, m.FileId) == 0))
                    {
                        testMetadata.Add(m);
                    }
                })
                .Returns(Task.FromResult(0));

            FileMetadataStoreMock.Setup(m => m.DeleteAsync(It.IsAny<MobileServiceFileMetadata>()))
                .Callback<MobileServiceFileMetadata>(m =>
                {
                    MobileServiceFileMetadata metadata = testMetadata.FirstOrDefault(tm => string.Compare(tm.FileId, m.FileId) == 0);

                    if (metadata != null)
                    {
                        testMetadata.Remove(metadata);
                    }
                })
                .Returns(Task.FromResult(0));

            SyncContext.PullFilesAsync("testtable", "id").Wait();
        }

        private void initializeInput()
        {
            this.testFiles = new List<MobileServiceFile>
            {
                new MobileServiceFile
                {
                    Id = "new-123",
                    Name = "newfile",
                    ContentMD5 = "0",
                    LastModified = DateTimeOffset.Now,
                    ParentId = this.testRecordId,
                    TableName = this.testTableName
                },
                new MobileServiceFile
                {
                    Id = "existing-123",
                    Name = "existing-file",
                    ContentMD5 = "0",
                    LastModified = DateTimeOffset.Now,
                    ParentId = this.testRecordId,
                    TableName = this.testTableName
                },
                new MobileServiceFile
                {
                    Id = "updated-123",
                    Name = "updated-file",
                    ContentMD5 = "1",
                    LastModified = DateTimeOffset.Now,
                    ParentId = this.testRecordId,
                    TableName = this.testTableName
                },
                new MobileServiceFile
                {
                    Id = "deleted-123",
                    Name = "deleted-file",
                    ContentMD5 = "2",
                    LastModified = DateTimeOffset.Now,
                    ParentId = this.testRecordId,
                    TableName = this.testTableName
                },
            };

            this.testMetadata = new List<MobileServiceFileMetadata>(testFiles.Skip(1).Select(f =>
            {
                var metadata = MobileServiceFileMetadata.FromFile(f);

                if (string.Compare(metadata.ContentMD5, "1") == 0)
                {
                    metadata.ContentMD5 = "0";
                    metadata.LastModified = DateTimeOffset.Now.AddMinutes(-5);
                }

                return metadata;
            }));

            this.testFiles = new List<MobileServiceFile>(testFiles.Where(f => string.Compare(f.ContentMD5, "2") != 0));
        }

        [Fact(DisplayName = "Changed file's metadata MD5 and LastUpdated properties are updated")]
        public void ChangedFileMetadataIsUpdated()
        {
            MobileServiceFileMetadata metadata = this.testMetadata.FirstOrDefault(f => string.Compare(f.FileId, "updated-123") == 0);
            MobileServiceFile file = this.testFiles.FirstOrDefault(f => string.Compare(f.Id, "updated-123") == 0);

            Assert.Equal(file.ContentMD5, metadata.ContentMD5);
            Assert.Equal(file.LastModified, metadata.LastModified);
        }

        [Fact(DisplayName = "The deleted file's metadata is deleted")]
        public void DeletedFileMetadataIsDeleted()
        {
            bool fileExists = this.testMetadata.Any(m => string.Compare(m.FileId, "deleted-123") == 0);

            Assert.False(fileExists);
        }

        [Fact(DisplayName = "Sync handler is invoked for deleted file")]
        public void SyncHandlerIsInvokedForDeletedFile()
        {
            SyncHandlerMock.Verify(m => m.ProcessFileSynchronizationAction(It.Is<MobileServiceFile>(f => string.Compare(f.Id, "deleted-123") == 0), 
                FileSynchronizationAction.Delete));
        }

        [Fact(DisplayName = "Sync handler is invoked for new file")]
        public void SyncHandlerIsInvokedForNewFile()
        {
            SyncHandlerMock.Verify(m => m.ProcessFileSynchronizationAction(It.Is<MobileServiceFile>(f => string.Compare(f.Id, "new-123") == 0),
                FileSynchronizationAction.Create));
        }

        [Fact(DisplayName = "Sync handler is invoked for updated file")]
        public void SyncHandlerIsInvokedForUpdatedFile()
        {
            SyncHandlerMock.Verify(m => m.ProcessFileSynchronizationAction(It.Is<MobileServiceFile>(f => string.Compare(f.Id, "updated-123") == 0),
                FileSynchronizationAction.Update));
        }

        [Fact(DisplayName = "Expected number of notifications are sent")]
        public void ExpectedNumberOfNotificationsAreSent()
        {
            EventManagerMock.Verify(m => m.PublishAsync(It.IsAny<FileOperationCompletedEvent>()), Times.Exactly(3));
        }

        [Fact(DisplayName = "Delete notification is sent once")]
        public void DeleteNotificationIsSentOnce()
        {
            EventManagerMock.Verify(m => m.PublishAsync(It.Is<FileOperationCompletedEvent>(e => ValidateDeleteNotification(e))), Times.Once());
        }

        private bool ValidateDeleteNotification(FileOperationCompletedEvent e)
        {
            return e.Kind == FileOperationKind.Delete &&
                e.Source == FileOperationSource.ServerPull &&
                string.Compare(e.File.Id, "deleted-123") == 0;
        }

        [Fact(DisplayName = "Create notification is sent once")]
        public void CreateNotificationIsSentOnce()
        {
            EventManagerMock.Verify(m => m.PublishAsync(It.Is<FileOperationCompletedEvent>(e => ValidateCreateNotification(e))), Times.Once());
        }

        private bool ValidateCreateNotification(FileOperationCompletedEvent e)
        {
            return e.Kind == FileOperationKind.Create &&
                e.Source == FileOperationSource.ServerPull &&
                string.Compare(e.File.Id, "new-123") == 0;
        }

        [Fact(DisplayName = "Update notification is sent once")]
        public void UpdateNotificationIsSentOnce()
        {
            EventManagerMock.Verify(m => m.PublishAsync(It.Is<FileOperationCompletedEvent>(e => ValidateUpdateNotification(e))), Times.Once());
        }

        private bool ValidateUpdateNotification(FileOperationCompletedEvent e)
        {
            return e.Kind == FileOperationKind.Update &&
                e.Source == FileOperationSource.ServerPull &&
                string.Compare(e.File.Id, "updated-123") == 0;
        }
    }
}
