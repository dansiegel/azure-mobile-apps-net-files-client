using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices;
using Microsoft.WindowsAzure.MobileServices.Eventing;
using Microsoft.WindowsAzure.MobileServices.Files;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;
using Microsoft.WindowsAzure.MobileServices.Files.Operations;
using Microsoft.WindowsAzure.MobileServices.Files.Sync;
using Microsoft.WindowsAzure.MobileServices.Files.Sync.Triggers;
using Moq;

namespace Microsoft.WindowsAzure.Mobile.Files.Test.Scenarios
{
    public abstract class MobileServiceFileSyncContextScenario
    {
        private readonly Mock<IMobileServiceClient> mobileServiceClientMock;
        
        private readonly Mock<IFileSyncTriggerFactory> triggerFactory;

        public MobileServiceFileSyncContextScenario()
        {
            this.mobileServiceClientMock = new Mock<IMobileServiceClient>();
            this.triggerFactory = new Mock<IFileSyncTriggerFactory>();

            SyncHandlerMock = new Mock<IFileSyncHandler>();
            FileMetadataStoreMock = new Mock<IFileMetadataStore>();
            FileOperationQueueMock = new Mock<IFileOperationQueue>();
            FilesClientMock = new Mock<IMobileServiceFilesClient>();

            EventManagerMock = new Mock<IMobileServiceEventManager>();
            
            this.mobileServiceClientMock.Setup(m => m.EventManager)
                .Returns(EventManagerMock.Object);

            SyncContext = new MobileServiceFileSyncContext(mobileServiceClientMock.Object, FileMetadataStoreMock.Object,
                FileOperationQueueMock.Object, triggerFactory.Object, SyncHandlerMock.Object, FilesClientMock.Object);
        }

        public Mock<IFileOperationQueue> FileOperationQueueMock { get; }

        public Mock<IMobileServiceEventManager> EventManagerMock { get; }

        public Mock<IFileMetadataStore> FileMetadataStoreMock { get; }

        public MobileServiceFileSyncContext SyncContext { get; }

        public Mock<IMobileServiceFilesClient> FilesClientMock { get; }

        public Mock<IFileSyncHandler> SyncHandlerMock { get; }
    }
}
