namespace AExpense.Data.Model
{
    using System;
    using AExpense.Data.Storage;
    using AExpense.Data.Enties;

    public static class ModelExtensions
    {
        public static string ToCsvLine(this ExpenseExport model)
        {
            return string.Format(
                "{0},{1},{2},{3},{4},{5},{6}",
                model.ApproveDate,
                model.Id,
                model.ApproverName,
                model.UserName,
                model.CostCenter,
                Enum.GetName(
                    typeof (ReimbursementMethod),
                    model.ReimbursementMethod),
                model.TotalAmount);
        }

        public static string ToExpenseExportKey(this DateTime model)
        {
            return model.ToString("yyyy-MM-dd");
        }

        public static Expense ToModel(this IExpenseEntity entity)
        {
            var expense = new Expense
                              {
                                  Id = new StorageKey(KeyGenerator.ExpenseEntitySuffix(entity.RowKey)).InvertedTicks,
                                  Approved = entity.Approved.HasValue ? entity.Approved.Value : false,
                                  CostCenter = entity.CostCenter,
                                  Date = entity.Date.HasValue ? entity.Date.Value : DateTime.UtcNow,
                                  ReimbursementMethod = (ReimbursementMethod) Enum.Parse(typeof (ReimbursementMethod), entity.ReimbursementMethod),
                                  Title = entity.Title,
                                  User = new User {UserName = entity.PartitionKey.DecodePartitionAndRowKey()},
                                  ApproverName = entity.ApproverName
                              };

            return expense;
        }

        public static ExpenseExport ToModel(this ExpenseExportEntity entity)
        {
            var expenseReport = new ExpenseExport
                                    {
                                        ApproveDate = Convert.ToDateTime(entity.PartitionKey),
                                        ApproverName = entity.ApproverName,
                                        CostCenter = entity.CostCenter,
                                        Id = entity.RowKey ?? StorageKey.Now.InvertedTicks,
                                        ReimbursementMethod =
                                            entity.ReimbursementMethod == null
                                                ? ReimbursementMethod.NotSet
                                                : (ReimbursementMethod) Enum.Parse(typeof (ReimbursementMethod), entity.ReimbursementMethod),
                                        TotalAmount = entity.TotalAmount,
                                        UserName = entity.UserName
                                    };
            return expenseReport;
        }

        public static ExpenseItem ToModel(this IExpenseItemEntity entity)
        {
           
            var expenseItem = new ExpenseItem
                                  {
                                      Id = new StorageKey(KeyGenerator.ExpenseItemEntitySuffix(entity.RowKey)).InvertedTicks,
                                      Amount = entity.Amount.HasValue ? entity.Amount.Value : 0,
                                      Description = entity.Description,
                                      ReceiptUrl = string.IsNullOrEmpty(entity.ReceiptUrl) ? null : new Uri(entity.ReceiptUrl),
                                      ReceiptThumbnailUrl = string.IsNullOrEmpty(entity.ReceiptThumbnailUrl) ? null : new Uri(entity.ReceiptThumbnailUrl)
                                  };

            return expenseItem;
        }

        public static ExpenseExportEntity ToTableEntity(this ExpenseExport model)
        {
            var expenseExport = new ExpenseExportEntity
                                    {
                                        PartitionKey = model.ApproveDate.ToExpenseExportKey(),
                                        RowKey = model.Id,
                                        ApproverName = model.ApproverName,
                                        CostCenter = model.CostCenter,
                                        ReimbursementMethod = Enum.GetName(typeof (ReimbursementMethod), model.ReimbursementMethod),
                                        UserName = model.UserName,
                                        TotalAmount = model.TotalAmount
                                    };

            return expenseExport;
        }

        public static ExpenseEntity ToTableEntity(this Expense model)
        {
            var expense = new ExpenseEntity
                              {
                                  PartitionKey = model.User.UserName.EncodePartitionAndRowKey(),
                                  RowKey = model.Id == null ? null : KeyGenerator.ExpenseEntityRowKey(model.Id.ToString()),
                                  Approved = model.Approved,
                                  CostCenter = model.CostCenter,
                                  Date = model.Date,
                                  ReimbursementMethod = Enum.GetName(typeof (ReimbursementMethod), model.ReimbursementMethod),
                                  Title = model.Title,
                                  ApproverName = model.ApproverName
                              };

            return expense;
        }

        public static ExpenseItemEntity ToTableEntity(this ExpenseItem model, string expensePartitionKey, string expenseId)
        {
            var expenseItem = new ExpenseItemEntity
                                  {
                                      PartitionKey = expensePartitionKey,
                                      RowKey = KeyGenerator.ExpenseItemEntityRowKey(expenseId, model.Id.ToString()),
                                      Amount = model.Amount,
                                      Description = model.Description,
                                      ReceiptUrl = model.ReceiptUrl == null ? null : model.ReceiptUrl.OriginalString,
                                      ReceiptThumbnailUrl = model.ReceiptThumbnailUrl == null ? null : model.ReceiptThumbnailUrl.OriginalString
                                  };

            return expenseItem;
        }
    }
}