// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using IO = System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Mobile.Files.IO;

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
