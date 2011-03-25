namespace AExpense.Data.Enties
{
    using AExpense.Data.Storage;

    public sealed class ExpenseItemEntity : KindEntity, IExpenseItemEntity
    {
        public const string RowKeyPrefix = "EI_";

        public ExpenseItemEntity() : base(TableKinds.ExpenseItem.ToString())
        {
        }

        public ExpenseItemEntity(string partitionKey, string rowKey)
            : base(partitionKey, rowKey, TableKinds.ExpenseItem.ToString())
        {
        }

        public double? Amount { get; set; }
        public string Description { get; set; }
        public string ReceiptThumbnailUrl { get; set; }
        public string ReceiptUrl { get; set; }
    }
}