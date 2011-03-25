namespace AExpense.Data.Storage
{
    using System;

    public interface IEntity
    {
        string PartitionKey { get; set; }
        string RowKey { get; set; }
        DateTime Timestamp { get; set; }
    }
}