namespace AExpense.Data.Process
{
    using AExpense.Data.Storage;

    public interface IBatchQueueCommand<in T> : IQueueCommand<T> where T : AzureQueueMessage
    {
        void PreRun();
        void PostRun();
    }
}