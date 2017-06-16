// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using IO = System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Mobile.Files.IO;
using Microsoft.WindowsAzure.MobileServices.Files.Sync;

namespace Microsoft.WindowsAzure.MobileServices.Files
{
    public static class MobileServiceTableExtensions
    {
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
            using (IO.Stream stream = await File.CreateAsync(targetPath))
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
    }
}
