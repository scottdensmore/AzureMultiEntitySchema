namespace AExpense.Data
{
    using System;
    using System.Collections.Generic;
    using System.Data.Services.Client;
    using System.Globalization;
    using System.Linq;
    using AExpense.Data.Enties;
    using AExpense.Data.Messages;
    using AExpense.Data.Model;
    using AExpense.Data.Storage;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    public class ExpenseRepository
    {
        private readonly CloudStorageAccount account;
        private readonly ExpenseReceiptStorage receiptStorage;
        private readonly TimeSpan sharedSignatureValiditySpan;

        public ExpenseRepository() : this(TimeSpan.FromMinutes(20))
        {
        }

        public ExpenseRepository(TimeSpan sharedSignatureValiditySpan)
        {
            this.account = CloudConfiguration.GetStorageAccount(AzureConnectionStrings.DataConnection);
            this.receiptStorage = new ExpenseReceiptStorage();
            this.sharedSignatureValiditySpan = sharedSignatureValiditySpan;
        }

        public Expense GetExpenseById(string username, string expenseId)
        {
            var context = new ExpenseDataContext(this.account) { MergeOption = MergeOption.NoTracking };

            string expenseRowKey = KeyGenerator.ExpenseEntityRowKey(expenseId);
            char charAfterSeparator = Convert.ToChar((Convert.ToInt32('_') + 1));
            var nextExpenseRowId = expenseRowKey + charAfterSeparator;
            string expenseItemRowKey = string.Format(CultureInfo.InvariantCulture, "{0}{1}", ExpenseItemEntity.RowKeyPrefix, expenseId);
            var nextExpenseItemRowId = expenseItemRowKey + charAfterSeparator;
            // TODO: Update to only have to compare to the expense id key and not the entire row
            var expenseQuery = (from e in context.ExpenseExpenseItem
                                where e.PartitionKey == username.EncodePartitionAndRowKey()
                                      && ((e.RowKey.CompareTo(expenseRowKey) >= 0
                                           && e.RowKey.CompareTo(nextExpenseRowId) < 0)
                                          || (e.RowKey.CompareTo(expenseItemRowKey) >= 0
                                              && e.RowKey.CompareTo(nextExpenseItemRowId) < 0))
                                select e).AsTableServiceQuery();

            Expense expense = null;
            var items = new List<ExpenseItem>();
            try
            {
                foreach (var entity in expenseQuery.Execute())
                {
                    switch (entity.ToEnum<TableKinds>())
                    {
                        case TableKinds.Expense:
                            expense = entity.ToKind<IExpenseEntity>().ToModel();
                            break;
                        case TableKinds.ExpenseItem:
                            items.Add(entity.ToKind<IExpenseItemEntity>().ToModel());
                            break;
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                Log.Write(EventKind.Error, e.Message);
                throw;
            }

            if (expense == null)
            {
                return expense;
            }

            items.ForEach(x => expense.Details.Add(x));

            var policy = new SharedAccessPolicy
                             {
                                 Permissions = SharedAccessPermissions.Read,
                                 SharedAccessExpiryTime = DateTime.UtcNow + this.sharedSignatureValiditySpan
                             };
            var client = this.account.CreateCloudBlobClient();
            var container = client.GetContainerReference(AzureStorageNames.ReceiptContainerName);
            foreach (var item in expense.Details)
            {
                if (item.ReceiptUrl != null)
                {
                    CloudBlob receiptBlob = container.GetBlobReference(item.ReceiptUrl.ToString());
                    item.ReceiptUrl = new Uri(item.ReceiptUrl.AbsoluteUri + receiptBlob.GetSharedAccessSignature(policy));
                }
                else
                {
                    item.ReceiptUrl = new Uri("/Styling/Images/no_receipt.png", UriKind.Relative);
                }

                if (item.ReceiptThumbnailUrl != null)
                {
                    CloudBlob receiptThumbnailBlob = container.GetBlobReference(item.ReceiptThumbnailUrl.ToString());
                    item.ReceiptThumbnailUrl = new Uri(item.ReceiptThumbnailUrl.AbsoluteUri + receiptThumbnailBlob.GetSharedAccessSignature(policy));
                }
                else
                {
                    item.ReceiptThumbnailUrl = new Uri("/Styling/Images/no_receipt.png", UriKind.Relative);
                }
            }

            return expense;
        }

        public IEnumerable<Expense> GetExpensesByUser(string username)
        {
            var context = new ExpenseDataContext(this.account) { MergeOption = MergeOption.NoTracking };

            char charAfterSeparator = Convert.ToChar((Convert.ToInt32('_') + 1));
            var nextId = ExpenseEntity.RowKeyPrefix + charAfterSeparator;

            // The Take(10) is not intended as a paging mechanism.
            // It was added to improve the performance of the application.
            // Using the partition key in the query will improve the performance
            // because the partition key is indexed.
            var query = (from expense in context.ExpenseExpenseItem
                         where expense.PartitionKey.CompareTo(username.EncodePartitionAndRowKey()) == 0
                               && expense.RowKey.CompareTo(ExpenseEntity.RowKeyPrefix) >= 0
                               && expense.RowKey.CompareTo(nextId) < 0
                         select expense).Take(10).AsTableServiceQuery();

            try
            {
                var expenses = new List<Expense>();
                foreach (var entity in query.Execute())
                {
                    switch (entity.ToEnum<TableKinds>())
                    {
                        case TableKinds.Expense:
                            expenses.Add(entity.ToKind<IExpenseEntity>().ToModel());
                            break;
                    }
                }
                return expenses;
            }
            catch (InvalidOperationException e)
            {
                Log.Write(EventKind.Error, e.TraceInformation());
                throw;
            }
        }

        public IEnumerable<Expense> GetExpensesForApproval(string approverName)
        {
            var context = new ExpenseDataContext(this.account) { MergeOption = MergeOption.NoTracking };

            // This query is not effecient at all. Because we are not using PartionKey & RowKey we are 
            // going to get a table scan. Best to create another table and use approver name as the 
            // PartitionKey.
            var query = (from expense in context.ExpenseExpenseItem
                         where expense.ApproverName.CompareTo(approverName) == 0
                         select expense).AsTableServiceQuery();

            try
            {
                return query.Execute().Select(e => e.ToKind<IExpenseEntity>().ToModel()).ToList();
            }
            catch (InvalidOperationException e)
            {
                Log.Write(EventKind.Error, e.TraceInformation());
                throw;
            }
        }

        public void SaveExpense(Expense expense)
        {
            var context = new ExpenseDataContext(this.account);
            ExpenseEntity expenseRow = expense.ToTableEntity();

            foreach (var expenseItem in expense.Details)
            {
                var expenseItemRow = expenseItem.ToTableEntity(expenseRow.PartitionKey, expense.Id);
                context.AddObject(AzureStorageNames.ExpenseTable, expenseItemRow);
            }

            context.AddObject(AzureStorageNames.ExpenseTable, expenseRow);
            context.SaveChanges(SaveChangesOptions.Batch);

            foreach (var expenseItem in expense.Details)
            {
                // save receipt image if any
                if (expenseItem.Receipt != null && expenseItem.Receipt.Length > 0)
                {
                    this.receiptStorage.AddReceipt(expenseItem.Id, expenseItem.Receipt, string.Empty);

                    var queue = new AzureQueue<NewReceiptMessage>(this.account, AzureStorageNames.NewReceiptMessage);
                    queue.AddMessage(new NewReceiptMessage
                                         {
                                             ExpenseItemId = expenseItem.Id,
                                             ExpenseId = expense.Id,
                                             Username = expense.User.UserName
                                         });
                }
            }
        }

        public void UpdateApproved(Expense expense)
        {
            var context = new ExpenseDataContext(this.account);

            IExpenseEntity expenseRow = GetExpenseRowById(context, expense.User.UserName, expense.Id);
            expenseRow.Approved = expense.Approved;

            var queue = new AzureQueue<ApprovedExpenseMessage>(this.account, AzureStorageNames.ApprovedExpenseMessage);
            queue.AddMessage(new ApprovedExpenseMessage { ExpenseId = expense.Id, ApproveDate = DateTime.UtcNow, Username = expense.User.UserName });

            context.UpdateObject(expenseRow);
            context.SaveChanges();
        }

        public void UpdateExpenseItemImages(string username, string expenseId, string expenseItemId, string imageUri, string thumbnailUri)
        {
            var context = new ExpenseDataContext(this.account);

            // this query would work faster if we specify PartitionKey and RowKey
            // For simplicity we'll just do a table scan by expense item id
            var query = (from expenseItemRow in context.ExpenseExpenseItem
                         where expenseItemRow.PartitionKey == username.EncodePartitionAndRowKey()
                               && expenseItemRow.RowKey == KeyGenerator.ExpenseItemEntityRowKey(expenseId, expenseItemId)
                         select expenseItemRow).AsTableServiceQuery();

            var item = query.Execute().SingleOrDefault();

            item.ReceiptUrl = imageUri;
            item.ReceiptThumbnailUrl = thumbnailUri;

            context.UpdateObject(item);
            context.SaveChanges();
        }

        private static IExpenseEntity GetExpenseRowById(ExpenseDataContext context, string username, string expenseRowKey)
        {
            var query = (from expense in context.ExpenseExpenseItem
                         where expense.PartitionKey == username.EncodePartitionAndRowKey()
                               && expense.RowKey == KeyGenerator.ExpenseEntityRowKey(expenseRowKey)
                         select expense).AsTableServiceQuery();

            return query.Execute().SingleOrDefault().ToKind<IExpenseEntity>();
        }
    }
}