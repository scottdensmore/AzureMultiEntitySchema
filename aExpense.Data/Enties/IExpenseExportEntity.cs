namespace AExpense.Data.Enties
{
    using AExpense.Data.Storage;

    public interface IExpenseExportEntity : IEntity
    {
        string ApproverName { get; set; }
        string CostCenter { get; set; }
        string ReimbursementMethod { get; set; }
        double TotalAmount { get; set; }
        string UserName { get; set; }
    }
}