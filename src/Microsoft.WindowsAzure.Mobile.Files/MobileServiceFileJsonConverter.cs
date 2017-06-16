// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using Microsoft.WindowsAzure.MobileServices;
using Microsoft.WindowsAzure.MobileServices.Sync;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Microsoft.WindowsAzure.MobileServices.Files
{
    public class MobileServiceFileJsonConverter : JsonConverter
    {
        private Type mobileServiceFileType;
        private IMobileServiceClient mobileServiceClient;

        public MobileServiceFileJsonConverter(IMobileServiceClient client)
        {
            this.mobileServiceFileType = typeof(MobileServiceFile);
            this.mobileServiceClient = client;
        }

        public override bool CanConvert(Type objectType)
        {
            return mobileServiceFileType.GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var mobileServiceFile = new MobileServiceFile();
            serializer.Populate(reader, mobileServiceFile);
            
            return mobileServiceFile;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException("MobileServiceFileJsonConverter does not support serialization");
        }

        public override bool CanWrite { get { return false; } }
    }
}
