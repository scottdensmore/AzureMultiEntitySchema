namespace AExpense.Data
{
    using Microsoft.WindowsAzure.StorageClient;
    using System;
    using AExpense.Data.Storage;
    using Microsoft.WindowsAzure;

    public class ExpenseExportStorage
    {
        private readonly CloudStorageAccount account;
        private readonly string containerName;
        private readonly CloudBlobContainer container;

        public ExpenseExportStorage()
        {
            this.account = CloudConfiguration.GetStorageAccount(AzureConnectionStrings.DataConnection);

            this.containerName = AzureStorageNames.ExpenseExportContainerName;
            var client = this.account.CreateCloudBlobClient();
            client.RetryPolicy = RetryPolicies.Retry(3, TimeSpan.FromSeconds(5));

            this.container = client.GetContainerReference(this.containerName);          
        }

        public string AddExport(string name, string content, string contentType)
        {
            CloudBlob blob = this.container.GetBlobReference(name);
            blob.Properties.ContentType = contentType;
            blob.UploadText(content);

            return blob.Uri.ToString();
        } 

        public string GetExport(string name)
        {
            CloudBlob blob = this.container.GetBlobReference(name);
            try
            {
                return blob.DownloadText();
            }
            catch (StorageClientException)
            {
                return string.Empty;
            }
        }
    }
}