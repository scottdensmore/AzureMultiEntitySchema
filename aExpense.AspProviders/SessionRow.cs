using System;
using Microsoft.WindowsAzure.StorageClient;

namespace AExpense.AspProviders
{
    public class SessionRow : TableServiceEntity
    {
        private string applicationName;
        private string blobName;
        private DateTime created;
        private DateTime expires;
        private string id;
        private DateTime lockDate;

        // application name + session id is partitionKey
        public SessionRow(string sessionId, string applicationName)
            : base(SecUtility.CombineToKey(applicationName, sessionId), string.Empty)
        {
            SecUtility.CheckParameter(ref sessionId, true, true, true, ProviderConfiguration.MaxStringPropertySizeInChars, "sessionId");
            SecUtility.CheckParameter(ref applicationName, true, true, true, Constants.MaxTableApplicationNameLength, "applicationName");

            Id = sessionId;
            ApplicationName = applicationName;
            ExpiresUtc = ProviderConfiguration.MinSupportedDateTime;
            LockDateUtc = ProviderConfiguration.MinSupportedDateTime;
            CreatedUtc = ProviderConfiguration.MinSupportedDateTime;
            Timeout = 0;
            BlobName = string.Empty;
        }

        public SessionRow()
        {
        }

        public string ApplicationName
        {
            get { return applicationName; }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException(
                        "To ensure string values are always updated, this implementation does not allow null as a string value.");
                }

                applicationName = value;
                PartitionKey = SecUtility.CombineToKey(ApplicationName, Id);
            }
        }

        public string BlobName
        {
            get { return blobName; }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException(
                        "To ensure string values are always updated, this implementation does not allow null as a string value.");
                }

                blobName = value;
            }
        }

        public DateTime CreatedUtc
        {
            get { return created; }

            set { SecUtility.SetUtcTime(value, out created); }
        }

        public DateTime ExpiresUtc
        {
            get { return expires; }

            set { SecUtility.SetUtcTime(value, out expires); }
        }

        public string Id
        {
            get { return id; }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException(
                        "To ensure string values are always updated, this implementation does not allow null as a string value.");
                }

                id = value;
                PartitionKey = SecUtility.CombineToKey(ApplicationName, Id);
            }
        }

        public bool Initialized { get; set; }
        public int Lock { get; set; }

        public DateTime LockDateUtc
        {
            get { return lockDate; }
            set { SecUtility.SetUtcTime(value, out lockDate); }
        }

        public bool Locked { get; set; }
        public int Timeout { get; set; }
    }
}