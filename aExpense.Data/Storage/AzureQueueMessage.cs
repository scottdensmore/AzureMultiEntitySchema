namespace AExpense.Data.Storage
{
    public abstract class AzureQueueMessage
    {
        public string Id { get; set; }
        public string PopReceipt { get; set; }
        public int DequeueCount { get; set; }
    }
}