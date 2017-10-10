using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;
using Microsoft.WindowsAzure.MobileServices.Files.Sync;
using Microsoft.WindowsAzure.MobileServices.Sync;

namespace Microsoft.WindowsAzure.MobileServices.Files
{
    public static class MobileServiceSyncTableExtensions
    {
        private static IFileSyncHandler fileSyncHandler;
        public async static Task<IEnumerable<MobileServiceFile>> GetFilesAsync<T>(this IMobileServiceSyncTable<T> table, T dataItem)
        {
            IFileSyncContext context = table.MobileServiceClient.GetFileSyncContext();

            var fileMetadata = await context.MetadataStore.GetMetadataAsync(table.TableName, GetDataItemId(dataItem));

            return fileMetadata.Where(m => !m.PendingDeletion).Select(m => MobileServiceFile.FromMetadata(m));
        }

        internal static void InitializeFileSync(IFileSyncHandler handler)
        {
            fileSyncHandler = handler;
        }

        private static string GetDataItemId(object dataItem)
        {
            // TODO: This needs to use the same logic used by the client SDK
            var objectType = dataItem.GetType().GetTypeInfo();
            var idProperty = objectType.GetDeclaredProperty("Id");

            if (idProperty != null && idProperty.CanRead)
            {
                return idProperty.GetValue(dataItem) as string;
            }

            return null;
        }

        public static MobileServiceFile CreateFile<T>(this IMobileServiceSyncTable<T> table, T dataItem, string fileName)
        {
            return new MobileServiceFile(fileName, table.TableName, GetDataItemId(dataItem));
        }

        public async static Task PurgeFilesAsync<T>(this IMobileServiceSyncTable<T> table)
        {
            IFileSyncContext context = table.MobileServiceClient.GetFileSyncContext();

            await context.MetadataStore.PurgeAsync(table.TableName);
        }

        public async static Task PurgeFilesAsync<T>(this IMobileServiceSyncTable<T> table, T dataItem)
        {
            IFileSyncContext context = table.MobileServiceClient.GetFileSyncContext();

            await context.MetadataStore.PurgeAsync(table.TableName, GetDataItemId(dataItem));
        }

        public async static Task PushFileChangesAsync<T>(this IMobileServiceSyncTable<T> table)
        {
            IFileSyncContext context = table.MobileServiceClient.GetFileSyncContext();

            await context.PushChangesAsync(CancellationToken.None);
        }

        public async static Task PullFilesAsync<T>(this IMobileServiceSyncTable<T> table, T dataItem)
        {
            IFileSyncContext context = table.MobileServiceClient.GetFileSyncContext();

            await context.PullFilesAsync(table.TableName, GetDataItemId(dataItem));
        }

        public async static Task<MobileServiceFile> AddFileAsync<T>(this IMobileServiceSyncTable<T> table, T dataItem, string fileName)
        {
            MobileServiceFile file = CreateFile(table, dataItem, fileName);

            await AddFileAsync(table, file);

            return file;
        }

        public async static Task AddFileAsync<T>(this IMobileServiceSyncTable<T> table, MobileServiceFile file)
        {
            IFileSyncContext context = table.MobileServiceClient.GetFileSyncContext();

            await context.AddFileAsync(file);
        }

     

        public async static Task DeleteFileAsync<T>(this IMobileServiceSyncTable<T> table, MobileServiceFile file)
        {
            IFileSyncContext context = table.MobileServiceClient.GetFileSyncContext();

            await context.DeleteFileAsync(file);

            MobileServiceFileMetadata metadata = await context.MetadataStore.GetFileMetadataAsync(file.Id);
            metadata.PendingDeletion = true;

            await context.MetadataStore.CreateOrUpdateAsync(metadata);
        }
    }
}
