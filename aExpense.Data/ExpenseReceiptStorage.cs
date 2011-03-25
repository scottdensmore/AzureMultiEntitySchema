namespace AExpense.Data
{
    using Microsoft.WindowsAzure.StorageClient;
    using System;
    using AExpense.Data.Storage;
    using Microsoft.WindowsAzure;

    public class ExpenseReceiptStorage
    {
        private readonly CloudStorageAccount account;
        private readonly string containerName;
        private readonly CloudBlobContainer container;

        public ExpenseReceiptStorage()
        {
            this.account = CloudConfiguration.GetStorageAccount(AzureConnectionStrings.DataConnection);

            this.containerName = AzureStorageNames.ReceiptContainerName;
            var client = this.account.CreateCloudBlobClient();
            client.RetryPolicy = RetryPolicies.Retry(3, TimeSpan.FromSeconds(5));

            this.container = client.GetContainerReference(this.containerName);
        }

        public string AddReceipt(string receiptId, byte[] receipt, string contentType)
        {
            CloudBlob blob = this.container.GetBlobReference(receiptId);
            blob.Properties.ContentType = contentType;
            blob.UploadByteArray(receipt);
                        
            return blob.Uri.ToString();
        }

        public byte[] GetReceipt(string receiptId)
        {
            CloudBlob blob = this.container.GetBlobReference(receiptId);
            try
            {
                return blob.DownloadByteArray();
            }
            catch (StorageClientException)
            {
                return null;
            }
        }

        public void DeleteReceipt(string receiptId)
        {
            CloudBlob blob = this.container.GetBlobReference(receiptId);
            blob.DeleteIfExists();
        }
    }
}
