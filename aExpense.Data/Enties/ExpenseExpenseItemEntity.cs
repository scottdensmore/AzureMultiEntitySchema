namespace AExpense.Data.Enties
{
    using System;
    using AExpense.Data.Storage;

    public sealed class ExpenseExpenseItemEntity : KindUnion, IExpenseEntity, IExpenseItemEntity
    {
        public double? Amount { get; set; }
        public bool? Approved { get; set; }
        public string ApproverName { get; set; }
        public string CostCenter { get; set; }
        public DateTime? Date { get; set; }
        public string Description { get; set; }
        public bool? HasReceipt { get; set; }
        public string ReceiptThumbnailUrl { get; set; }
        public string ReceiptUrl { get; set; }
        public string ReimbursementMethod { get; set; }
        public string Title { get; set; }
    }
}