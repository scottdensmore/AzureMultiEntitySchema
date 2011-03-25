namespace AExpense.Data.Storage
{
    using System.Collections.Generic;

    public interface IAzureQueue<T> where T : AzureQueueMessage
    {
        void AddMessage(T message);
        void Clear();
        void DeleteMessage(T message);
        void EnsureExist();
        T GetMessage();
        IEnumerable<T> GetMessages(int maxMessagesToReturn);
    }
}