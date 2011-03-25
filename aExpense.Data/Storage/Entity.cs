namespace AExpense.Data.Storage
{
    using System;
    using System.Data.Services.Common;
    using Microsoft.WindowsAzure.StorageClient;

    [DataServiceKey(new[] {"PartitionKey", "RowKey"})]
    public abstract class Entity : TableServiceEntity, IEntity
    {
        protected Entity()
        {
        }

        protected Entity(string partitionKey, string rowKey) : base(partitionKey, rowKey)
        {
        }
    }
}