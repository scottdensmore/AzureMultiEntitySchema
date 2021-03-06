﻿namespace AExpense.Data
{
    using System;
    using AExpense.Data.Enties;
    using AExpense.Data.Storage;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    public static class ApplicationStorageInitializer
    {
        public static void Initialize()
        {
            CloudStorageAccount account = CloudConfiguration.GetStorageAccount(AzureConnectionStrings.DataConnection);

            // Tables
            var cloudTableClient = new CloudTableClient(account.TableEndpoint.ToString(), account.Credentials);
            cloudTableClient.CreateTableIfNotExist<ExpenseExpenseItemEntity>(AzureStorageNames.ExpenseTable);
            cloudTableClient.CreateTableIfNotExist<ExpenseExportEntity>(AzureStorageNames.ExpenseExportTable);

            // Blobs
            CloudBlobClient client = account.CreateCloudBlobClient();
            client.RetryPolicy = RetryPolicies.Retry(3, TimeSpan.FromSeconds(5));
            var container = client.GetContainerReference(AzureStorageNames.ReceiptContainerName);
            container.CreateIfNotExist();
            container = client.GetContainerReference(AzureStorageNames.ExpenseExportContainerName);
            container.CreateIfNotExist();

            // Queues
            CloudQueueClient queueClient = account.CreateCloudQueueClient();
            queueClient.RetryPolicy = RetryPolicies.Retry(3, TimeSpan.FromSeconds(5));
            CloudQueue queueReference = queueClient.GetQueueReference(AzureStorageNames.ApprovedExpenseMessage);
            queueReference.CreateIfNotExist();
            queueReference = queueClient.GetQueueReference(AzureStorageNames.PoisonApprovedExpenseMessage);
            queueReference.CreateIfNotExist();
            queueReference = queueClient.GetQueueReference(AzureStorageNames.NewReceiptMessage);
            queueReference.CreateIfNotExist();
            queueReference = queueClient.GetQueueReference(AzureStorageNames.PoisonNewReceiptMessage);
            queueReference.CreateIfNotExist();
        }
    }
}