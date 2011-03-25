namespace AExpense.Data.Enties
{
    using System;
    using AExpense.Data.Storage;

    public interface IExpenseEntity : IEntity
    {
        bool? Approved { get; set; }
        string ApproverName { get; set; }
        string CostCenter { get; set; }
        DateTime? Date { get; set; }
        string ReimbursementMethod { get; set; }
        string Title { get; set; }
    }
}