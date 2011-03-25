namespace AExpense.Workers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using AExpense.Data;
    using AExpense.Data.Model;
    using AExpense.Data.Process;

    public class ExpenseExportCommand : ICommand
    {
        private readonly ExpenseExportRepository expenseExports;
        private readonly ExpenseExportStorage exportStorage;

        public ExpenseExportCommand()
        {
            this.expenseExports = new ExpenseExportRepository();
            this.exportStorage = new ExpenseExportStorage();
        }

        public void Run()
        {
            DateTime jobDate = DateTime.UtcNow;
            string name = jobDate.ToExpenseExportKey();

            IEnumerable<ExpenseExport> exports = this.expenseExports.Retreive(jobDate);
            if (exports == null || exports.Count() == 0)
            {
                return;
            }

            string text = this.exportStorage.GetExport(name);
            var exportText = new StringBuilder(text);
            foreach (ExpenseExport expenseExport in exports)
            {
                exportText.AppendLine(expenseExport.ToCsvLine());
            }

            this.exportStorage.AddExport(name, exportText.ToString(), "text/plain");

            // delete the exports
            foreach (ExpenseExport exportToDelete in exports)
            {
                try
                {
                    this.expenseExports.Delete(exportToDelete);
                }
                catch (InvalidOperationException ex)
                {
                    Log.Write(EventKind.Error, ex.TraceInformation());
                }
            }
        }
    }
}