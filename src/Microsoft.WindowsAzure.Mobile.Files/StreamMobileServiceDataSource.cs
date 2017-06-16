// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Files.Sync;

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
