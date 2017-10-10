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

        /// <summary>
        /// Uploads a <paramref name="file"/> from a local file specified in the <paramref name="filePath"/>
        /// </summary>
        /// <typeparam name="T">The type of the instances in the table.</typeparam>
        /// <param name="table">The table instance that contains the record associated with the <see cref="MobileServiceFile"/>.</param>
        /// <param name="file">The <see cref="MobileServiceFile"/> instance.</param>
        /// <param name="filePath">The path of the file to be uploaded.</param>
        /// <returns>A <see cref="Task"/> that completes when the upload has finished.</returns>
        public async static Task UploadFileAsync<T>(this IMobileServiceTable<T> table, MobileServiceFile file, string filePath)
        {
            IMobileServiceFileDataSource dataSource = new PathMobileServiceFileDataSource(filePath);

            await table.UploadFileAsync(file, dataSource);
        }

        /// <summary>
        /// Downloads a <paramref name="file"/> and saves it to the local device using the provided <paramref name="targetPath"/>.
        /// </summary>
        /// <typeparam name="T">The type of the instances in the table.</typeparam>
        /// <param name="table">The table instance that contains the record associated with the <see cref="MobileServiceFile"/>.</param>
        /// <param name="file">The <see cref="MobileServiceFile"/> instance representing the file to be downloaded.</param>
        /// <param name="targetPath">The path that will be used to save the downloaded file.</param>
        /// <returns>A <see cref="Task"/> that completes when the download has finished.</returns>
        public async static Task DownloadFileAsync<T>(this IMobileServiceTable<T> table, MobileServiceFile file, string targetPath)
        {
            using (Stream stream = await Mobile.Files.IO.File.CreateAsync(targetPath))
            {
                await table.DownloadFileToStreamAsync(file, stream);
            }
        }

        internal static string GetDataItemId(object dataItem)
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
    }
}
