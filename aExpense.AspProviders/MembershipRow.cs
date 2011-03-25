using System;
using System.Configuration.Provider;
using Microsoft.WindowsAzure.StorageClient;

namespace AExpense.AspProviders
{
    public class MembershipRow : TableServiceEntity, IComparable
    {
        private string applicationName;
        private string comment;

        private DateTime createDate;
        private string email;
        private DateTime failedPasswordAnswerAttemptWindowStart;
        private DateTime failedPasswordAttemptWindowStart;
        private DateTime lastActivityDate;
        private DateTime lastLockoutDate;
        private DateTime lastLoginDate;
        private DateTime lastPasswordChangedDate;
        private string password;
        private string passwordAnswer;
        private string passwordQuestion;
        private string passwordSalt;
        private string profileBlobName;
        private DateTime profileLastUpdated;
        private string userName;

        // partition key is applicationName + userName
        // rowKey is empty
        public MembershipRow(string applicationName, string userName)
            : base(SecUtility.CombineToKey(applicationName, userName), string.Empty)
        {
            if (string.IsNullOrEmpty(applicationName))
            {
                throw new ProviderException("Partition key cannot be empty!");
            }
            ////if (string.IsNullOrEmpty(userName))
            ////{
            ////    throw new ProviderException("RowKey cannot be empty!");
            ////}

            // applicationName + userName is partitionKey
            // the reasoning behind this is that we want to strive for the best scalability possible 
            // chosing applicationName as the partition key and userName as row key would not give us that because 
            // it would mean that a site with millions of users had all users on a single partition
            // having the applicationName and userName inside the partition key is important for queries as queries
            // for users in a single application are the most frequent 
            // these queries are faster because application name and user name are part of the key
            ////PartitionKey = SecUtility.CombineToKey(applicationName, userName);
            ////RowKey = string.Empty;

            ApplicationName = applicationName;
            UserName = userName;

            Password = string.Empty;
            PasswordSalt = string.Empty;
            Email = string.Empty;
            PasswordAnswer = string.Empty;
            PasswordQuestion = string.Empty;
            Comment = string.Empty;
            ProfileBlobName = string.Empty;

            CreateDateUtc = ProviderConfiguration.MinSupportedDateTime;
            LastLoginDateUtc = ProviderConfiguration.MinSupportedDateTime;
            LastActivityDateUtc = ProviderConfiguration.MinSupportedDateTime;
            LastLockoutDateUtc = ProviderConfiguration.MinSupportedDateTime;
            LastPasswordChangedDateUtc = ProviderConfiguration.MinSupportedDateTime;
            FailedPasswordAttemptWindowStartUtc = ProviderConfiguration.MinSupportedDateTime;
            FailedPasswordAnswerAttemptWindowStartUtc = ProviderConfiguration.MinSupportedDateTime;
            ProfileLastUpdatedUtc = ProviderConfiguration.MinSupportedDateTime;

            ProfileIsCreatedByProfileProvider = false;
            ProfileSize = 0;
        }

        public MembershipRow()
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
                PartitionKey = SecUtility.CombineToKey(ApplicationName, UserName);
            }
        }

        public string Comment
        {
            get { return comment; }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException(
                        "To ensure string values are always updated, this implementation does not allow null as a string value.");
                }

                comment = value;
            }
        }

        public DateTime CreateDateUtc
        {
            get { return createDate; }

            set { SecUtility.SetUtcTime(value, out createDate); }
        }

        public string Email
        {
            get { return email; }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException(
                        "To ensure string values are always updated, this implementation does not allow null as a string value.");
                }

                email = value;
            }
        }

        public int FailedPasswordAnswerAttemptCount { get; set; }

        public DateTime FailedPasswordAnswerAttemptWindowStartUtc
        {
            get { return failedPasswordAnswerAttemptWindowStart; }

            set { SecUtility.SetUtcTime(value, out failedPasswordAnswerAttemptWindowStart); }
        }

        public int FailedPasswordAttemptCount { get; set; }

        public DateTime FailedPasswordAttemptWindowStartUtc
        {
            get { return failedPasswordAttemptWindowStart; }

            set { SecUtility.SetUtcTime(value, out failedPasswordAttemptWindowStart); }
        }

        public bool IsAnonymous { get; set; }
        public bool IsApproved { get; set; }

        public bool IsLockedOut { get; set; }

        public DateTime LastActivityDateUtc
        {
            get { return lastActivityDate; }

            set { SecUtility.SetUtcTime(value, out lastActivityDate); }
        }

        public DateTime LastLockoutDateUtc
        {
            get { return lastLockoutDate; }

            set { SecUtility.SetUtcTime(value, out lastLockoutDate); }
        }

        public DateTime LastLoginDateUtc
        {
            get { return lastLoginDate; }

            set { SecUtility.SetUtcTime(value, out lastLoginDate); }
        }

        public DateTime LastPasswordChangedDateUtc
        {
            get { return lastPasswordChangedDate; }

            set { SecUtility.SetUtcTime(value, out lastPasswordChangedDate); }
        }

        public string Password
        {
            get { return password; }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException(
                        "To ensure string values are always updated, this implementation does not allow null as a string value.");
                }

                password = value;
            }
        }

        public string PasswordAnswer
        {
            get { return passwordAnswer; }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException(
                        "To ensure string values are always updated, this implementation does not allow null as a string value.");
                }

                passwordAnswer = value;
            }
        }

        public int PasswordFormat { get; set; }

        public string PasswordQuestion
        {
            get { return passwordQuestion; }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException(
                        "To ensure string values are always updated, this implementation does not allow null as a string value.");
                }

                passwordQuestion = value;
            }
        }

        public string PasswordSalt
        {
            get { return passwordSalt; }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException(
                        "To ensure string values are always updated, this implementation does not allow null as a string value.");
                }

                passwordSalt = value;
            }
        }

        public string ProfileBlobName
        {
            get { return profileBlobName; }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException(
                        "To ensure string values are always updated, this implementation does not allow null as a string value..");
                }

                profileBlobName = value;
            }
        }

        public bool ProfileIsCreatedByProfileProvider { get; set; }

        public DateTime ProfileLastUpdatedUtc
        {
            get { return profileLastUpdated; }

            set { SecUtility.SetUtcTime(value, out profileLastUpdated); }
        }

        public int ProfileSize { get; set; }
        public Guid UserId { get; set; }

        public string UserName
        {
            get { return userName; }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException(
                        "To ensure string values are always updated, this implementation does not allow null as a string value.");
                }

                userName = value;
                PartitionKey = SecUtility.CombineToKey(ApplicationName, UserName);
            }
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            var row = obj as MembershipRow;
            if (row == null)
            {
                throw new ArgumentException("The parameter obj is not of type MembershipRow.");
            }

            return string.Compare(UserName, row.UserName, StringComparison.Ordinal);
        }
    }
}