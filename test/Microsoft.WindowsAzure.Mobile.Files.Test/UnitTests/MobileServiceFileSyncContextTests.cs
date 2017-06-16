using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices;
using Microsoft.WindowsAzure.MobileServices.Files;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;
using Microsoft.WindowsAzure.MobileServices.Files.Operations;
using Microsoft.WindowsAzure.MobileServices.Files.Sync;
using Microsoft.WindowsAzure.MobileServices.Files.Sync.Triggers;
using Moq;
using Xunit;

namespace Microsoft.WindowsAzure.Mobile.Files.Test.UnitTests
{
    public sealed class MobileServiceFileSyncContextTests
    {
        [Fact]
        public void Constructor_WithNullClient_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("client", () => CreateContext(MobileServiceFileSyncContextArgs.Client));
        }

        [Fact]
        public void Constructor_WithNullMetadataStore_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("metadataStore", () => CreateContext(MobileServiceFileSyncContextArgs.MetadataStore));
        }

        [Fact]
        public void Constructor_WithNullOperationQueue_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("operationsQueue", () => CreateContext(MobileServiceFileSyncContextArgs.OperationQueue));
        }

        [Fact]
        public void Constructor_WithNullTriggerFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("syncTriggerFactory", () => CreateContext(MobileServiceFileSyncContextArgs.TriggerFactory));
        }

        [Fact]
        public void Constructor_WithNullSyncHandler_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("syncHandler", () => CreateContext(MobileServiceFileSyncContextArgs.FileSyncHandler));
        }

        [Fact]
        public void Constructor_WithNullFilesClient_CreatesDefaultClient()
        {
            MobileServiceFileSyncContext context = CreateContext(MobileServiceFileSyncContextArgs.FilesClient);

            Assert.NotNull(context.MobileServiceFilesClient);
            Assert.IsType<MobileServiceFilesClient>(context.MobileServiceFilesClient);
        }

        [Fact]
        public void Disposing_DisposesTriggers()
        {
            MobileServiceFileSyncContextInput input = MobileServiceFileSyncContextInput.CreateWithout(MobileServiceFileSyncContextArgs.TriggerFactory);

            var triggers = Enumerable.Range(0, 5).Select(i => new FileSyncTrigger()).ToList();

            var triggerFactoryMock = new Mock<IFileSyncTriggerFactory>();
            triggerFactoryMock.Setup(f => f.CreateTriggers(It.IsAny<IFileSyncContext>()))
                .Returns(() => new List<IFileSyncTrigger>(triggers));

            input.TriggerFactory = triggerFactoryMock.Object;

            MobileServiceFileSyncContext context = CreateContext(input);

            context.Dispose();

            Assert.True(triggers.All(t => t.Disposed));
        }

        [Fact]
        public async Task QueueOperationAsync_NotifiesExistingOperation_WhenOperationIsQueued()
        {
            var testNewOperation = new CreateMobileServiceFileOperation("id", "fileId");
            var testExistingOperation = new Mock<IMobileServiceFileOperation>();

            MobileServiceFileSyncContextInput input = MobileServiceFileSyncContextInput.CreateWithout(MobileServiceFileSyncContextArgs.OperationQueue);

            var operationQueueMock = new Mock<IFileOperationQueue>();
            operationQueueMock.Setup(m => m.GetOperationByFileIdAsync(It.Is<string>(s => string.Compare(s, testNewOperation.FileId) == 0)))
                .Returns(() => Task.FromResult<IMobileServiceFileOperation>(testExistingOperation.Object));

            input.OperationsQueue = operationQueueMock.Object;

            MobileServiceFileSyncContext context = CreateContext(input);

            await context.QueueOperationAsync(testNewOperation);

            testExistingOperation.Verify(m => m.OnQueueingNewOperation(It.Is<IMobileServiceFileOperation>(o => o.Equals(testNewOperation))), Times.Once());
        }

        [Fact]
        public async Task QueueOperationAsync_RemovesExistingOperation_WhenOperationIsCancelled()
        {
            var testNewOperation = new CreateMobileServiceFileOperation("id", "fileId");
            var testExistingOperation = new Mock<IMobileServiceFileOperation>();
            testExistingOperation.SetupGet(m => m.Id).Returns("testID");
            testExistingOperation.Setup(m => m.OnQueueingNewOperation(It.Is<IMobileServiceFileOperation>(o => o == testNewOperation)))
                .Callback(() => testExistingOperation.SetupGet(m => m.State).Returns(FileOperationState.Cancelled));


            MobileServiceFileSyncContextInput input = MobileServiceFileSyncContextInput.CreateWithout(MobileServiceFileSyncContextArgs.OperationQueue);

            var operationQueueMock = new Mock<IFileOperationQueue>();
            operationQueueMock.Setup(m => m.GetOperationByFileIdAsync(It.Is<string>(s => string.Compare(s, testNewOperation.FileId) == 0)))
                .Returns(() => Task.FromResult<IMobileServiceFileOperation>(testExistingOperation.Object));

            input.OperationsQueue = operationQueueMock.Object;

            MobileServiceFileSyncContext context = CreateContext(input);

            await context.QueueOperationAsync(testNewOperation);

            Assert.Equal(FileOperationState.Cancelled, testExistingOperation.Object.State);
            operationQueueMock.Verify(m => m.RemoveAsync(It.Is<string>(s => s == testExistingOperation.Object.Id)), Times.Once());
        }

        private MobileServiceFileSyncContext CreateContext(MobileServiceFileSyncContextArgs nullArguments)
        {
            MobileServiceFileSyncContextInput input = MobileServiceFileSyncContextInput.CreateWithout(nullArguments);

            return CreateContext(input);
        }

        private MobileServiceFileSyncContext CreateContext(MobileServiceFileSyncContextInput input)
        {
            return new MobileServiceFileSyncContext(input.Client, input.MetadataStore, input.OperationsQueue,
                input.TriggerFactory, input.SyncHandler, input.FilesClient);
        }

        private class MobileServiceFileSyncContextInput
        {
            private MobileServiceFileSyncContextInput()
            { }

            public IMobileServiceClient Client { get; set; }

            public IFileMetadataStore MetadataStore { get; set; }

            public IFileOperationQueue OperationsQueue { get; set; }

            public IFileSyncTriggerFactory TriggerFactory { get; set; }

            public IFileSyncHandler SyncHandler { get; set; }

            public IMobileServiceFilesClient FilesClient { get; set; }

            public static MobileServiceFileSyncContextInput CreateWithout(MobileServiceFileSyncContextArgs args)
            {
                var arguments = MobileServiceFileSyncContextArgs.All & ~args;

                var input = new MobileServiceFileSyncContextInput();

                input.Client = CreateIfEnabled<IMobileServiceClient>(arguments, MobileServiceFileSyncContextArgs.Client);
                input.MetadataStore = CreateIfEnabled<IFileMetadataStore>(arguments, MobileServiceFileSyncContextArgs.MetadataStore);
                input.OperationsQueue = CreateIfEnabled<IFileOperationQueue>(arguments, MobileServiceFileSyncContextArgs.OperationQueue);
                input.TriggerFactory = CreateIfEnabled<IFileSyncTriggerFactory>(arguments, MobileServiceFileSyncContextArgs.TriggerFactory);
                input.SyncHandler = CreateIfEnabled<IFileSyncHandler>(arguments, MobileServiceFileSyncContextArgs.FileSyncHandler);
                input.FilesClient = CreateIfEnabled<IMobileServiceFilesClient>(arguments, MobileServiceFileSyncContextArgs.FilesClient);

                return input;
            }

            private static T CreateIfEnabled<T>(MobileServiceFileSyncContextArgs flags, MobileServiceFileSyncContextArgs checkFlag) where T : class
            {
                if (flags.HasFlag(checkFlag))
                {
                    return new Mock<T>().Object;
                }

                return null;
            }
        }

        private class FileSyncTrigger : IFileSyncTrigger, IDisposable
        {
            public bool Disposed { get; set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }



        [Flags]
        private enum MobileServiceFileSyncContextArgs
        {
            None = 0,
            Client = 1,
            MetadataStore = 2,
            OperationQueue = 4,
            TriggerFactory = 8,
            FileSyncHandler = 16,
            FilesClient = 32,
            All = 0xFFF
        }
    }
}
