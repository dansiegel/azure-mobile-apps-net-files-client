namespace Microsoft.WindowsAzure.MobileServices.Files.Identity
{
    public class StorageTokenRequest
    {
        public StoragePermissions Permissions { get; set; }

        public MobileServiceFile TargetFile { get; set; }

        public string ScopedEntityId { get; set; }

        public string ProviderName { get; set; }
    }
}
