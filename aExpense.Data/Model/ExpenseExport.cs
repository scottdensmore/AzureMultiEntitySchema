namespace AExpense.Data.Model
{
    using System;

    public class ExpenseExport
    {
        public DateTime ApproveDate { get; set; }
        public string ApproverName { get; set; }
        public string CostCenter { get; set; }
        public ReimbursementMethod ReimbursementMethod { get; set; }
        public string Id { get; set; }
        public double TotalAmount { get; set; }
        public string UserName { get; set; }
    }
}