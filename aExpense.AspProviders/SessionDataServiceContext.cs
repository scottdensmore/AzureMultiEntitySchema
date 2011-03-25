using Microsoft.WindowsAzure.StorageClient;
using System.Linq;

namespace AExpense.AspProviders
{
    internal class SessionDataServiceContext : TableServiceContext
    {
        public SessionDataServiceContext()
            : base(null, null)
        {
        }

        public IQueryable<SessionRow> Sessions
        {
            get
            {
                return CreateQuery<SessionRow>("Sessions");
            }
        }
    }
}