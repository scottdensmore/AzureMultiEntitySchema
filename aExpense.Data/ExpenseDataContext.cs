namespace AExpense.Data
{
    using System;
    using System.Globalization;
    using System.Linq;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;
    using AExpense.Data.Enties;

    internal class ExpenseDataContext : TableServiceContext
    {
        public ExpenseDataContext(CloudStorageAccount account)
            : this(account.TableEndpoint.ToString(), account.Credentials)
        {
        }

        public ExpenseDataContext(string baseAddress, StorageCredentials credentials)
            : base(baseAddress, credentials)
        {
            ResolveType = ResolveEntityType;
        }

        public IQueryable<ExpenseExportEntity> ExpenseExport
        {
            get { return CreateQuery<ExpenseExportEntity>(AzureStorageNames.ExpenseExportTable); }
        }

        public IQueryable<ExpenseExpenseItemEntity> ExpenseExpenseItem
        {
            get { return CreateQuery<ExpenseExpenseItemEntity>(AzureStorageNames.ExpenseTable); }
        }

        private static Type ResolveEntityType(string name)
        {
            var tableName = name.Split(new[] {'.'}).Last();
            switch (tableName)
            {
                case AzureStorageNames.ExpenseTable:
                    return typeof(ExpenseExpenseItemEntity);
                case AzureStorageNames.ExpenseExportTable:
                    return typeof (ExpenseExportEntity);
            }

            throw new ArgumentException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Could not resolve the table name '{0}' to a known entity type.",
                    name));
        }
    }
}