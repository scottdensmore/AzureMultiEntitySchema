namespace AExpense.Data.Messages
{
    using System.Runtime.Serialization;
    using AExpense.Data.Storage;

    [DataContract]
    public class NewReceiptMessage : AzureQueueMessage
    {
        [DataMember]
        public string ExpenseId { get; set; }

        [DataMember]
        public string ExpenseItemId { get; set; }

        [DataMember]
        public string Username { get; set; }
    }
}