// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Query;
using Microsoft.WindowsAzure.MobileServices.Sync;
using Newtonsoft.Json.Linq;

namespace Microsoft.WindowsAzure.MobileServices.Files.Operations
{
    internal sealed class FileOperationQueue : IFileOperationQueue
    {
        public const string FileOperationTableName = "__fileoperationsmetadata";
        private const string CountPropertyName = "count";

        private IMobileServiceLocalStore store;
        private Lazy<OperationsInfo> operationsInfo;

        public FileOperationQueue(IMobileServiceLocalStore store)
        {
            this.store = store;

            this.operationsInfo = new Lazy<OperationsInfo>(LoadOperationsInformation, LazyThreadSafetyMode.ExecutionAndPublication);

            DefineTable(store);
        }

        public int Count
        {
            get { return operationsInfo.Value.Count; }
        }

        private void DefineTable(IMobileServiceLocalStore store)
        {
            var tableLocalStore = store as MobileServiceLocalStore;

            if (tableLocalStore == null)
            {
                return;
            }

            tableLocalStore.DefineTable(FileOperationTableName, new JObject()
            {
                { MobileServiceSystemColumns.Id, string.Empty },
                { "fileId", string.Empty },
                { "kind", 0 },
                { "sequence", 0 }
            });
        }

        private OperationsInfo LoadOperationsInformation()
        {
            var query = new MobileServiceTableQueryDescription(FileOperationTableName);
            query.IncludeTotalCount = true;
            query.Top = 1;

            // Get the last item in the queue
            query.Ordering.Add(new OrderByNode(new MemberAccessNode(null, "sequence"), OrderByDirection.Descending));

            JToken result = this.store.ReadAsync(query).Result;

            return new OperationsInfo(result.Value<int>(CountPropertyName), result.Value<int>("sequence"));
        }

        private async Task<IMobileServiceFileOperation> GetNextOperationItemAsync(bool deleteItem)
        {
            var query = new MobileServiceTableQueryDescription(FileOperationTableName);
            query.Ordering.Add(new OrderByNode(new MemberAccessNode(null, "sequence"), OrderByDirection.Ascending));
            query.Top = 1;

            JToken result = await this.store.ReadAsync(query);
            FileOperationItem operationItem = result.ToObject<List<FileOperationItem>>().FirstOrDefault();

            if (deleteItem)
            {
                await Delete(query);
            }

            return operationItem != null ? operationItem.ToOperation() : null;
        }

        private async Task Delete(MobileServiceTableQueryDescription query)
        {
            await this.store.DeleteAsync(query);

            OperationsInfo operationsInfo = this.operationsInfo.Value;
            Interlocked.Decrement(ref operationsInfo.Count);
        }

        public async Task RemoveAsync(string id)
        {
            var query = new MobileServiceTableQueryDescription(FileOperationTableName);
            query.Filter = new BinaryOperatorNode(BinaryOperatorKind.Equal, new MemberAccessNode(null, MobileServiceSystemColumns.Id), new ConstantNode(id));
            query.Top = 1;

            await Delete(query);
        }

        public async Task<IMobileServiceFileOperation> PeekAsync()
        {
            return await GetNextOperationItemAsync(false);
        }

        public async Task<IMobileServiceFileOperation> DequeueAsync()
        {
            return await GetNextOperationItemAsync(true);
        }

        public async Task<IMobileServiceFileOperation> GetOperationByFileIdAsync(string fileId)
        {
            var query = new MobileServiceTableQueryDescription(FileOperationTableName);
            query.Filter = new BinaryOperatorNode(BinaryOperatorKind.Equal, new MemberAccessNode(null, "fileId"), new ConstantNode(fileId));
            query.Top = 1;

            JToken result = await this.store.ReadAsync(query);

            FileOperationItem operationItem = result.ToObject<List<FileOperationItem>>().FirstOrDefault();

            return operationItem != null ? operationItem.ToOperation() : null;
        }

        public async Task EnqueueAsync(IMobileServiceFileOperation operation)
        {

            OperationsInfo operationsInfo = this.operationsInfo.Value;
            var operationItem = new FileOperationItem
            {
                FileId = operation.FileId,
                Id = operation.Id,
                Kind = operation.Kind,
                Sequence = Interlocked.Increment(ref operationsInfo.Sequence)
            };

            await this.store.UpsertAsync(FileOperationTableName, new[] { operationItem.ToJsonObject() }, ignoreMissingColumns: false);

            Interlocked.Increment(ref operationsInfo.Count);
        }

        private class OperationsInfo
        {
            public OperationsInfo(int count, int sequence)
            {
                this.Count = count;
                this.Sequence = sequence;
            }

            public int Count;

            public int Sequence;
        }

        private class FileOperationItem
        {
            public string Id { get; set; }

            public string FileId { get; set; }

            public FileOperationKind Kind { get; set; }

            public long Sequence { get; set; }

            public MobileServiceFileOperation ToOperation()
            {
                switch (Kind)
                {
                    case FileOperationKind.Create:
                        return new CreateMobileServiceFileOperation(Id, FileId);
                    case FileOperationKind.Update:
                        return new UpdateMobileServiceFileOperation(Id, FileId);
                    case FileOperationKind.Delete:
                        return new DeleteMobileServiceFileOperation(Id, FileId);
                    default:
                        throw new NotSupportedException("Unsupported file operation kind.");
                }
            }

            internal JObject ToJsonObject()
            {
                return new JObject
                {
                    { MobileServiceSystemColumns.Id, this.Id },
                    { "kind", (int)this.Kind },
                    { "fileId", this.FileId },
                    { "sequence", this.Sequence }
                };
            }
        }
    }
}
