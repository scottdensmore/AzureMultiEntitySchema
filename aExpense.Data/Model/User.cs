namespace AExpense.Data.Model
{
    using System.Collections.Generic;

    public class User
    {
        public User()
        {
            this.Roles = new List<string>();
        }
        
        public string UserName { get; set; }

        public string FullName { get; set; }

        public string Manager { get; set; }

        public IList<string> Roles { get; set; }

        public string CostCenter { get; set; }

        public ReimbursementMethod PreferredReimbursementMethod { get; set; }

        public override string ToString()
        {
            return UserName;
        }
    }
}