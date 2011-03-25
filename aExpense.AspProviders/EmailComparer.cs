using System;
using System.Collections.Generic;

namespace AExpense.AspProviders
{
    internal class EmailComparer: IComparer<MembershipRow> {
        public int Compare(MembershipRow x, MembershipRow y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                return string.Compare(x.Email, y.Email, StringComparison.Ordinal);
            }
        }
    }
}