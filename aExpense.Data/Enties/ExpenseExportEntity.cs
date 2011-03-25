namespace AExpense.Data.Enties
{
    using AExpense.Data.Storage;

    public sealed class ExpenseExportEntity : KindEntity, IExpenseExportEntity
    {
        public const string RowKeyPrefix = "EX_";

        public ExpenseExportEntity()
            : base(TableKinds.Expense.ToString())
        {
        }

        public ExpenseExportEntity(string partitionKey, string rowKey)
            : base(partitionKey, rowKey, TableKinds.Expense.ToString())
        {
        }

        public string ApproverName { get; set; }
        public string CostCenter { get; set; }
        public string ReimbursementMethod { get; set; }
        public double TotalAmount { get; set; }
        public string UserName { get; set; }
    }
}