namespace AExpense.Data
{
    using System.Globalization;
    using AExpense.Data.Enties;

    public static class KeyGenerator
    {
        public static string ExpenseEntityRowKey(string expenseRowKeySuffix)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}_{1}", ExpenseEntity.RowKeyPrefix, expenseRowKeySuffix);
        }

        public static string ExpenseItemEntityRowKey(string expenseEntityRowKeySuffix, string expenseEntityItemRowKeySuffix)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}_{2}", ExpenseItemEntity.RowKeyPrefix, expenseEntityRowKeySuffix, expenseEntityItemRowKeySuffix);
        }

        public static string ExpenseItemEntitySuffix(string entityRowKey)
        {
            return entityRowKey.Substring(entityRowKey.IndexOf('_', ExpenseEntity.RowKeyPrefix.Length + 1) + 1);
        }

        public static string ExpenseEntitySuffix(string entityRowKey)
        {
            return entityRowKey.Substring(ExpenseEntity.RowKeyPrefix.Length + 1);
        }
    }
}