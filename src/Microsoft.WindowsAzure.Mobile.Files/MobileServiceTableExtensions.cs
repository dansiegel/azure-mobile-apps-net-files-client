// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Files.Identity;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;
using Microsoft.WindowsAzure.MobileServices.Files.StorageProviders;
using Microsoft.WindowsAzure.MobileServices.Files.Sync;

namespace Microsoft.WindowsAzure.MobileServices.Files
{
    public static class MobileServiceTableExtensions
    {
        private readonly static Dictionary<IMobileServiceClient, IMobileServiceFilesClient> filesClients =
            new Dictionary<IMobileServiceClient, IMobileServiceFilesClient>();
        private readonly static object filesClientsSyncRoot = new object();

        private static IMobileServiceFilesClient GetFilesClient(IMobileServiceClient client)
        {
            lock (filesClientsSyncRoot)
            {
                IMobileServiceFilesClient filesClient;
                
                if (!filesClients.TryGetValue(client, out filesClient))
                {
                    filesClient = new MobileServiceFilesClient(client, new AzureBlobStorageProvider(client));
                    filesClients.Add(client, filesClient);
                }

                return filesClient;
            }
        }

        public async static Task<IEnumerable<MobileServiceFile>> GetFilesAsync<T>(this IMobileServiceTable<T> table, T dataItem)
        {
            IFileSyncContext context = table.MobileServiceClient.GetFileSyncContext();

            var fileMetadata = await context.MetadataStore.GetMetadataAsync(table.TableName, GetDataItemId(dataItem));

            return fileMetadata.Where(m => !m.PendingDeletion).Select(m => MobileServiceFile.FromMetadata(m));
        }

        public static MobileServiceFile CreateFile<T>(this IMobileServiceTable<T> table, T dataItem, string fileName)
        {
            return new MobileServiceFile(fileName, table.TableName, GetDataItemId(dataItem));
        }

        public async static Task<Uri> GetFileUri<T>(this IMobileServiceTable<T> table, MobileServiceFile file, StoragePermissions permissions)
        {
            IMobileServiceFilesClient filesClient = GetFilesClient(table.MobileServiceClient);

            return await filesClient.GetFileUriAsync(file, permissions);
        }

        public async static Task<MobileServiceFile> AddFileAsync<T>(this IMobileServiceTable<T> table, T dataItem, string fileName, Stream fileStream)
        {
            MobileServiceFile file = CreateFile(table, dataItem, fileName);

            await AddFileAsync(table, file, fileStream);

            return file;
        }

        public async static Task AddFileAsync<T>(this IMobileServiceTable<T> table, MobileServiceFile file, Stream fileStream)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }

            if (fileStream == null)
            {
                throw new ArgumentNullException("fileStream");
            }

            IMobileServiceFileDataSource dataSource = new StreamMobileServiceFileDataSource(fileStream);

            IMobileServiceFilesClient client = GetFilesClient(table.MobileServiceClient);
            await client.UploadFileAsync(MobileServiceFileMetadata.FromFile(file), dataSource);
        }

        public async static Task DeleteFileAsync<T>(this IMobileServiceTable<T> table, MobileServiceFile file)
        {
            MobileServiceFileMetadata metadata = MobileServiceFileMetadata.FromFile(file);

            IMobileServiceFilesClient client = GetFilesClient(table.MobileServiceClient);
            await client.DeleteFileAsync(metadata);
        }

        public async static Task<Uri> GetFileUriAsync<T>(this IMobileServiceTable<Task> table, MobileServiceFile file, StoragePermissions permissions)
        {
            IMobileServiceFilesClient client = GetFilesClient(table.MobileServiceClient);
            return await client.GetFileUriAsync(file, permissions);
        }

        public async static Task DownloadFileToStreamAsync<T>(this IMobileServiceTable<T> table, MobileServiceFile file, Stream fileStream)
        {
            IMobileServiceFilesClient filesClient = GetFilesClient(table.MobileServiceClient);
            await filesClient.DownloadToStreamAsync(file, fileStream);
        }

        public async static Task UploadFromStreamAsync<T>(this IMobileServiceTable<T> table, MobileServiceFile file, Stream fileStream)
        {
            IMobileServiceFileDataSource dataSource = new StreamMobileServiceFileDataSource(fileStream);
            await UploadAsync(table.MobileServiceClient, file, dataSource);
        }
        
        public async static Task UploadFileAsync<T>(this IMobileServiceTable<T> table, MobileServiceFile file, IMobileServiceFileDataSource dataSource)
        {
            await UploadAsync(table.MobileServiceClient, file, dataSource);
        }

        public async static Task UploadAsync(this IMobileServiceClient client, MobileServiceFile file, IMobileServiceFileDataSource dataSource)
        {
            MobileServiceFileMetadata metadata = MobileServiceFileMetadata.FromFile(file);

            IMobileServiceFilesClient filesClient = GetFilesClient(client);
            await filesClient.UploadFileAsync(metadata, dataSource);
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
    }
}
