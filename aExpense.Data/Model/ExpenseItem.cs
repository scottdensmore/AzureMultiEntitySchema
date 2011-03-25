namespace AExpense.Data.Model
{
    using System;

    [Serializable]
    public class ExpenseItem
    {
        public double Amount { get; set; }
        public string Description { get; set; }
        public string Id { get; set; }
        public byte[] Receipt { get; set; }
        public Uri ReceiptThumbnailUrl { get; set; }
        public Uri ReceiptUrl { get; set; }
    }
}