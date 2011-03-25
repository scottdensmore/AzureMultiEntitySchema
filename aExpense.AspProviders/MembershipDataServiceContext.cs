using System.Linq;
using Microsoft.WindowsAzure.StorageClient;

namespace AExpense.AspProviders
{
    /// <summary>
    ///   This class allows DevtableGen to generate the correct table (named 'Membership')
    /// </summary>
    internal class MembershipDataServiceContext : TableServiceContext
    {
        public MembershipDataServiceContext()
            : base(null, null)
        {
        }

        public IQueryable<MembershipRow> Membership
        {
            get { return CreateQuery<MembershipRow>("Membership"); }
        }
    }
}