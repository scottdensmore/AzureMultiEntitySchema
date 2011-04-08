namespace AExpense.Data
{
    public static class AzureStorageNames
    {
        public const string ExpenseExportTable = "ExpenseExports";
        public const string ExpenseTable = "Expenses";
        public const string ReceiptContainerName = "receipt";
        public const string ExpenseExportContainerName = "expenseout";
        public const string ApprovedExpenseMessage = "approvedexpensemessage";
        public const string PoisonApprovedExpenseMessage = "poisonapprovedexpensemessage";
        public const string NewReceiptMessage = "newreceiptmessage";
        public const string PoisonNewReceiptMessage = "poisonnewreceiptmessage";
    }
}