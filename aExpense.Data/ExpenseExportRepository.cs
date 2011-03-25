namespace AExpense.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AExpense.Data.Enties;
    using AExpense.Data.Model;
    using AExpense.Data.Storage;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    public class ExpenseExportRepository
    {
        private readonly CloudStorageAccount account;

        public ExpenseExportRepository()
        {
            this.account = CloudConfiguration.GetStorageAccount(AzureConnectionStrings.DataConnection);
        }

        public void Delete(ExpenseExport expenseExport)
        {
            var context = new ExpenseDataContext(this.account);
            var query = (from export in context.ExpenseExport
                         where
                             export.PartitionKey.CompareTo(expenseExport.ApproveDate.ToExpenseExportKey()) == 0 &&
                             export.RowKey.CompareTo(expenseExport.Id) == 0
                         select export).AsTableServiceQuery();
            ExpenseExportEntity entity = query.Execute().SingleOrDefault();
            if (entity == null)
            {
                return;
            }

            context.DeleteObject(entity);
            context.SaveChanges();
        }

        public IEnumerable<ExpenseExport> Retreive(DateTime jobDate)
        {
            var context = new ExpenseDataContext(this.account);
            string compareDate = jobDate.ToExpenseExportKey();
            var query = (from export in context.ExpenseExport
                         where export.PartitionKey.CompareTo(compareDate) <= 0
                         select export).AsTableServiceQuery();

            var val = query.Execute();
            return val.Select(e => e.ToModel()).ToList();
        }

        public void Save(ExpenseExport expenseExport)
        {
            var context = new ExpenseDataContext(this.account);
            ExpenseExportEntity entity = expenseExport.ToTableEntity();

            context.AddObject(AzureStorageNames.ExpenseExportTable, entity);
            context.SaveChanges();
        }
    }
}