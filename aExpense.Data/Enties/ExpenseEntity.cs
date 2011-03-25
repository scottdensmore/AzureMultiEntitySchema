namespace AExpense.Data.Enties
{
    using System;
    using AExpense.Data.Storage;

    public sealed class ExpenseEntity : KindEntity, IExpenseEntity
    {
        public const string RowKeyPrefix = "E_";

        public ExpenseEntity() : base(TableKinds.Expense.ToString())
        {
        }

        public ExpenseEntity(string partitionKey, string rowKey)
            : base(partitionKey, rowKey, TableKinds.Expense.ToString())
        {
        }

        public bool? Approved { get; set; }
        public string ApproverName { get; set; }
        public string CostCenter { get; set; }
        public DateTime? Date { get; set; }
        public string ReimbursementMethod { get; set; }
        public string Title { get; set; }
    }
}