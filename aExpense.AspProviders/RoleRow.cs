using System;
using Microsoft.WindowsAzure.StorageClient;

namespace AExpense.AspProviders
{
    public class RoleRow : TableServiceEntity
    {
        private string applicationName;
        private string roleName;
        private string userName;

        public RoleRow()
        {
        }

        // applicationName + userName is partitionKey
        // roleName is rowKey
        public RoleRow(string applicationName, string roleName, string userName)
            : base(SecUtility.CombineToKey(applicationName, userName), SecUtility.Escape(roleName))
        {
            SecUtility.CheckParameter(ref applicationName, true, true, true, Constants.MaxTableApplicationNameLength, "applicationName");
            SecUtility.CheckParameter(ref roleName, true, true, true, 512, "roleName");
            SecUtility.CheckParameter(ref userName, true, false, true, Constants.MaxTableUsernameLength, "userName");
            ApplicationName = applicationName;
            RoleName = roleName;
            UserName = userName;
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

        public string RoleName
        {
            get { return roleName; }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException(
                        "To ensure string values are always updated, this implementation does not allow null as a string value.");
                }

                roleName = value;
                RowKey = SecUtility.Escape(RoleName);
            }
        }

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
    }
}