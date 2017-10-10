using System.Threading.Tasks;
using Microsoft.WindowsAzure.Mobile.Files.IO;
using IO = System.IO;

namespace Microsoft.WindowsAzure.MobileServices.Files.Sync
{
    public class PathMobileServiceFileDataSource : IMobileServiceFileDataSource
    {
        private string filePath;

        public PathMobileServiceFileDataSource(string filePath)
        {
            this.filePath = filePath;
        }

        public Task<IO.Stream> GetStream()
        {
            return File.OpenReadAsync(filePath);
        }
    }
}
