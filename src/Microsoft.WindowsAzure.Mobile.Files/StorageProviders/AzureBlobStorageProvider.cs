using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Files.Identity;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.WindowsAzure.MobileServices.Files.StorageProviders
{
    public class AzureBlobStorageProvider : IStorageProvider
    {
        private readonly IMobileServiceClient mobileServiceClient;

        public AzureBlobStorageProvider(IMobileServiceClient client)
        {
            this.mobileServiceClient = client;
        }

        public async Task UploadFileAsync(MobileServiceFileMetadata metadata, IMobileServiceFileDataSource dataSource, StorageToken storageToken)
        {
            CloudBlockBlob blob = GetBlobReference(storageToken, metadata.FileName);

            using (var stream = await dataSource.GetStream())
            {
                await blob.UploadFromStreamAsync(stream);

                metadata.LastModified = blob.Properties.LastModified;
                metadata.FileStoreUri = blob.Uri.LocalPath;

                stream.Position = 0;
                metadata.ContentMD5 = GetMD5Hash(stream);
            }
        }

        public async Task DownloadFileToStreamAsync(MobileServiceFile file, Stream stream, StorageToken storageToken)
        {
            CloudBlockBlob blob = GetBlobReference(storageToken, file.Name);

            await blob.DownloadToStreamAsync(stream);
        }

        public Task<Uri> GetFileUriAsync(StorageToken storageToken, string fileName)
        {
            CloudBlockBlob blob = GetBlobReference(storageToken, fileName);

            return Task.FromResult(new Uri(blob.Uri, storageToken.RawToken));
        }

        private CloudBlockBlob GetBlobReference(StorageToken token, string fileName)
        {
            CloudBlockBlob blob = null;

            if (token.Scope == StorageTokenScope.File)
            {
                blob = new CloudBlockBlob(new Uri(token.ResourceUri, token.RawToken));
            }
            else if (token.Scope == StorageTokenScope.Record)
            {
                var container = new CloudBlobContainer(new Uri(token.ResourceUri, token.RawToken));

                blob = container.GetBlockBlobReference(fileName);
            }

            return blob;
        }

        private string GetMD5Hash(Stream stream)
        {
            //using (MD5 md5 = MD5.Create())
            //{
            //    byte[] hash = md5.ComputeHash(stream);
            //    return Convert.ToBase64String(hash);
            //}

            return string.Empty;
        }
    }
}
