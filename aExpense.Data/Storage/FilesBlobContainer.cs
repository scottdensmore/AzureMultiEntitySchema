namespace AExpense.Data.Storage
{
    using System;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    public class FilesBlobContainer : IAzureBlobContainer<byte[]>
    {
        private readonly CloudStorageAccount account;
        private readonly CloudBlobContainer container;
        private readonly string contentType;

        public FilesBlobContainer(CloudStorageAccount account, string containerName, string contentType)
        {
            this.account = account;
            this.contentType = contentType;

            var client = this.account.CreateCloudBlobClient();
            client.RetryPolicy = RetryPolicies.Retry(3, TimeSpan.FromSeconds(5));

            container = client.GetContainerReference(containerName);
        }

        public void Delete(string objId)
        {
            CloudBlob blob = container.GetBlobReference(objId);
            blob.DeleteIfExists();
        }

        public void DeleteContainer()
        {
            container.Delete();
        }

        public void EnsureExist()
        {
            container.CreateIfNotExist();
            container.SetPermissions(new BlobContainerPermissions {PublicAccess = BlobContainerPublicAccessType.Blob});
        }

        public Uri GetUri(string objId)
        {
            CloudBlob blob = container.GetBlobReference(objId);
            return blob.Uri;
        }

        public void Save(string objId, byte[] obj)
        {
            CloudBlob blob = container.GetBlobReference(objId);
            blob.Properties.ContentType = contentType;
            blob.UploadByteArray(obj);
        }

        byte[] IAzureBlobContainer<byte[]>.Get(string objId)
        {
            CloudBlob blob = container.GetBlobReference(objId);
            try
            {
                return blob.DownloadByteArray();
            }
            catch (StorageClientException)
            {
                return null;
            }
        }
    }
}