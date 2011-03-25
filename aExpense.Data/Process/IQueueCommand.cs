namespace AExpense.Data.Process
{
    using AExpense.Data.Storage;

    public interface IQueueCommand<in T> where T : AzureQueueMessage
    {
        void Run(T message);
    }
}