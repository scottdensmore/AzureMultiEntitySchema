namespace AExpense.Data.Messages
{
    using System;
    using System.Runtime.Serialization;
    using AExpense.Data.Storage;

    [DataContract]
    public class ApprovedExpenseMessage : AzureQueueMessage
    {
        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public string ExpenseId { get; set; }

        [DataMember]
        public DateTime ApproveDate { get; set; }
    }
}