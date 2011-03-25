using System.Linq;
using Microsoft.WindowsAzure.StorageClient;

namespace AExpense.AspProviders
{
    /// <summary>
    ///   This class allows DevtableGen to generate the correct table (named 'Roles')
    /// </summary>
    internal class RoleDataServiceContext : TableServiceContext
    {
        public RoleDataServiceContext() : base(null, null)
        {
        }

        public IQueryable<RoleRow> Roles
        {
            get { return CreateQuery<RoleRow>("Roles"); }
        }
    }
}