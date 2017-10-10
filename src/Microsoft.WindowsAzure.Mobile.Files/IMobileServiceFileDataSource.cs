using System.IO;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.MobileServices.Files
{
    public interface IMobileServiceFileDataSource
    {
        Task<Stream> GetStream();
    }
}
