namespace AExpense.Data.Storage
{
    using System;

    public interface IAzureBlobContainer<T>
    {
        void EnsureExist();
        void Save(string objId, T obj);
        T Get(string objId);
        Uri GetUri(string objId);
        void Delete(string objId);
        void DeleteContainer();
    }
}