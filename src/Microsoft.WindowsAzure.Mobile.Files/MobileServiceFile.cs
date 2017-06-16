// ---------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.MobileServices.Files.Metadata;

namespace Microsoft.WindowsAzure.MobileServices.Files
{
    public sealed class MobileServiceFile
    {
        private string id;
        private string name;
        private string parentId;
        private string tableName;
        private IDictionary<string, string> metadata;

        internal MobileServiceFile() { }

        public MobileServiceFile(string name, string tableName, string parentId)
            : this(name, name, tableName, parentId) { }

        public MobileServiceFile(string id, string name, string tableName, string parentId)
        {
            this.id = id;
            this.name = name;
            this.tableName = tableName;
            this.parentId = parentId;
        }

        public string Id
        {
            get { return this.id; }
            set { this.id = value; }
        }

        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        public string TableName
        {
            get { return this.tableName; }
            set { this.tableName = value; }
        }

        public string ParentId
        {
            get { return parentId; }
            set { parentId = value; }
        }

        public long Length { get; set; }

        public string ContentMD5 { get; set; }

        public DateTimeOffset? LastModified { get; set; }

        public string StoreUri { get; set; }

        public IDictionary<string, string> Metadata
        {
            get { return this.metadata; }
            set { this.metadata = value; }
        }

        internal static MobileServiceFile FromMetadata(MobileServiceFileMetadata metadata)
        {
            var file = new MobileServiceFile(metadata.FileId, metadata.ParentDataItemType, metadata.ParentDataItemId);

            file.ContentMD5 = metadata.ContentMD5;
            file.LastModified = metadata.LastModified;
            file.Length = metadata.Length;
            file.Metadata = metadata.ToDictionary();
            return file;
        }
    }
}
