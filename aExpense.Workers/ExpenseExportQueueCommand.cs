namespace AExpense.Workers
{
    using System;
    using System.Data.Services.Client;
    using System.Linq;
    using System.Net;
    using AExpense.Data;
    using AExpense.Data.Messages;
    using AExpense.Data.Model;
    using AExpense.Data.Process;
    using AExpense.Data.Storage;

    public class ExpenseExportQueueCommand : IQueueCommand<ApprovedExpenseMessage>
    {
        private readonly ExpenseExportRepository expenseExports;
        private readonly ExpenseRepository expenses;

        public ExpenseExportQueueCommand()
        {
            this.expenses = new ExpenseRepository();
            this.expenseExports = new ExpenseExportRepository();
        }

        public void Run(ApprovedExpenseMessage message)
        {
            try
            {
                Expense expense = this.expenses.GetExpenseById(message.Username, message.ExpenseId);

                if (expense == null)
                {
                    return;
                }

                // if the expense was not updated but a message was persisted, we need to delete it
                if (!expense.Approved)
                {
                    return;
                }

                double totalToPay = expense.Details.Sum(x => x.Amount);
                var export = new ExpenseExport
                                           {
                                               ApproveDate = message.ApproveDate,
                                               ApproverName = expense.ApproverName,
                                               CostCenter = expense.CostCenter,
                                               Id = expense.Id,
                                               ReimbursementMethod = expense.ReimbursementMethod,
                                               TotalAmount = totalToPay,
                                               UserName = expense.User.UserName
                                           };
                this.expenseExports.Save(export);
            }
            catch (InvalidOperationException ex)
            {
                var innerEx = ex.InnerException as DataServiceClientException;
                if (innerEx != null && innerEx.StatusCode == (int)HttpStatusCode.Conflict)
                {
                    // the data already exists so we can return true because we have processed this before
                    return;
                }

                Log.Write(EventKind.Error, ex.TraceInformation());
                throw;
            }

            return;
        }
    }
}