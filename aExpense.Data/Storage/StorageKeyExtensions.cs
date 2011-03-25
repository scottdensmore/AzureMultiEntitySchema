namespace AExpense.Data.Storage
{
    using System;

    public static class StorageKeyExtensions
    {
        public static string EncodePartitionAndRowKey(this string key)
        {
            if (key == null)
            {
                return null;
            }

            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key));
        }

        public static string DecodePartitionAndRowKey(this string encodedKey)
        {
            if (encodedKey == null)
            {
                return null;
            }

            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedKey));
        }
    }
}