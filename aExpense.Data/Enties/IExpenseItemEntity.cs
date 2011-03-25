namespace AExpense.Data.Enties
{
    using AExpense.Data.Storage;

    public interface IExpenseItemEntity : IEntity
    {
        double? Amount { get; set; }
        string Description { get; set; }
        string ReceiptThumbnailUrl { get; set; }
        string ReceiptUrl { get; set; }
    }
}