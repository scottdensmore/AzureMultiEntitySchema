namespace AExpense.Data.Model
{
    using System;
    using System.Collections.Generic;

    public class Expense
    {
        public Expense()
        {
            Details = new List<ExpenseItem>();
        }

        public bool Approved { get; set; }
        public string ApproverName { get; set; }
        public string CostCenter { get; set; }
        public DateTime Date { get; set; }
        public ICollection<ExpenseItem> Details { get; private set; }
        public string Id { get; set; }
        public ReimbursementMethod ReimbursementMethod { get; set; }
        public string Title { get; set; }
        //public User User { get; set; }
        public string UserName { get; set; }
    }
}