using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Files;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;
using Microsoft.WindowsAzure.MobileServices.Files.Operations;
using Microsoft.WindowsAzure.MobileServices.Files.Sync;
using Moq;
using Xunit;

namespace Microsoft.WindowsAzure.Mobile.Files.Test.Scenarios
{
    [Trait("MobileServiceFileSyncContext: Push changes", "")]
    public sealed class PushChangesScenario : MobileServiceFileSyncContextScenario
    {
        private readonly Mock<IMobileServiceFileOperation> operationMock;
        private readonly Queue<IMobileServiceFileOperation> queue;

        public PushChangesScenario()
        {
            this.operationMock = new Mock<IMobileServiceFileOperation>();
            this.queue = new Queue<IMobileServiceFileOperation>();
            queue.Enqueue(this.operationMock.Object);

            this.FileOperationQueueMock.Setup(m => m.DequeueAsync())
                .Returns(() => Task.FromResult(this.queue.Dequeue()));

            this.FileOperationQueueMock.Setup(m => m.RemoveAsync(It.IsAny<string>()))
                .Callback(() => this.queue.Dequeue())
                .Returns(Task.FromResult(0));

            this.FileOperationQueueMock.Setup(m => m.Count)
                .Returns(()=> this.queue.Count);

            this.FileOperationQueueMock.Setup(m => m.PeekAsync())
                .Returns(()=>Task.FromResult(this.queue.Peek()));


            SyncContext.PushChangesAsync(CancellationToken.None).Wait();
        }

        [Fact(DisplayName = "Queued operation is executed")]
        public void QueuedOperationIsExecuted()
        {
            this.operationMock.Verify(m => m.Execute(It.IsAny<IFileMetadataStore>(), SyncContext));
        }

        [Fact(DisplayName ="The operation queue is emptied")]
        public void OperationQueueIsEmptied()
        {
            Assert.Empty(this.queue);
        }
    }
}
