using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Files.Identity;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;
using Microsoft.WindowsAzure.MobileServices.Files.StorageProviders;

namespace Microsoft.WindowsAzure.MobileServices.Files
{
    public sealed class MobileServiceFilesClient : IMobileServiceFilesClient
    {
        private IMobileServiceClient client;
        private IStorageProvider storageProvider;

        public MobileServiceFilesClient(IMobileServiceClient client, IStorageProvider storageProvider)
        {
            this.client = client;
            this.storageProvider = storageProvider;
        }

        public async Task<IEnumerable<MobileServiceFile>> GetFilesAsync(string tableName, string itemId)
        {
            string route = string.Format("/tables/{0}/{1}/MobileServiceFiles", tableName, itemId);

            if (!this.client.SerializerSettings.Converters.Any(p => p is MobileServiceFileJsonConverter))
            {
                this.client.SerializerSettings.Converters.Add(new MobileServiceFileJsonConverter(this.client));
            }

            return await this.client.InvokeApiAsync<IEnumerable<MobileServiceFile>>(route, HttpMethod.Get, null);
        }

        public async Task UploadFileAsync(MobileServiceFileMetadata metadata, IMobileServiceFileDataSource dataSource)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException("metadata");
            }

            if (dataSource == null)
            {
                throw new ArgumentNullException("dataSource");
            }

            StorageToken token = await GetStorageToken(this.client, MobileServiceFile.FromMetadata(metadata), StoragePermissions.Write);

            await this.storageProvider.UploadFileAsync(metadata, dataSource, token);
        }

        public async Task DownloadToStreamAsync(MobileServiceFile file, Stream stream)
        {
            StorageToken token = await GetStorageToken(this.client, file, StoragePermissions.Read);

            await this.storageProvider.DownloadFileToStreamAsync(file, stream, token);
        }

        public async Task DeleteFileAsync(MobileServiceFileMetadata metadata)
        {
            string route = string.Format("/tables/{0}/{1}/MobileServiceFiles/{2}/", metadata.ParentDataItemType, metadata.ParentDataItemId, metadata.FileName);

            var parameters = new Dictionary<string, string>();
            if (metadata.FileStoreUri != null)
            {
                parameters.Add("x-zumo-filestoreuri", metadata.FileStoreUri);
            }

            await this.client.InvokeApiAsync(route, HttpMethod.Delete, parameters);
        }

        public async Task<Uri> GetFileUriAsync(MobileServiceFile file, StoragePermissions permissions)
        {
            StorageToken token = await GetStorageToken(this.client, file, permissions);

            return await this.storageProvider.GetFileUriAsync(token, file.Name);
        }

        private async Task<StorageToken> GetStorageToken(IMobileServiceClient client, MobileServiceFileMetadata metadata, StoragePermissions permissions)
        {
            return await GetStorageToken(client, MobileServiceFile.FromMetadata(metadata), permissions);
        }

        private async Task<StorageToken> GetStorageToken(IMobileServiceClient client, MobileServiceFile file, StoragePermissions permissions)
        {

            var tokenRequest = new StorageTokenRequest();
            tokenRequest.Permissions = permissions;
            tokenRequest.TargetFile = file;

            string route = string.Format("/tables/{0}/{1}/StorageToken", file.TableName, file.ParentId);

            return await this.client.InvokeApiAsync<StorageTokenRequest, StorageToken>(route, tokenRequest);
        }
    }
}
