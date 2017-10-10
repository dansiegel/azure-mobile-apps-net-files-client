using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.MobileServices.Files
{
    public class StreamMobileServiceFileDataSource : IMobileServiceFileDataSource
    {
        private Stream stream;

        public StreamMobileServiceFileDataSource(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            this.stream = stream;
        }

        public Task<System.IO.Stream> GetStream()
        {
            return Task.FromResult(this.stream);
        }
    }
}
