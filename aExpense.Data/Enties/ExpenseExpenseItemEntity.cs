namespace AExpense.Data.Enties
{
    using System;
    using AExpense.Data.Storage;

    public sealed class ExpenseExpenseItemEntity: KindUnion, IExpenseEntity, IExpenseItemEntity
    {
        public string Description { get; set; }
        public double? Amount { get; set; }
        public string ReceiptUrl { get; set; }
        public string ReceiptThumbnailUrl { get; set; }
        public string Title { get; set; }
        public DateTime? Date { get; set; }
        public bool? Approved { get; set; }
        public string CostCenter { get; set; }
        public string ApproverName { get; set; }
        public string ReimbursementMethod { get; set; }
    }
}