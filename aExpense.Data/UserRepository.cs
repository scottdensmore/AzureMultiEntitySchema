namespace AExpense.Data
{
    using System;
    using System.Collections.Generic;
    using System.Web.Profile;
    using System.Web.Security;
    using AExpense.Data.Model;

    public class UserRepository
    {
        public User GetUser(string userName)
        {
            // this is replaced with claims
            string[] roles = Roles.GetRolesForUser();

            // var attributes = SimulatedLdapProfileStore.GetAttributesFor(userName, new[] { "costCenter", "manager", "displayName" });

            // we still use profile for app-specific profile data like preferred reiumbursment method
            var profile = ProfileBase.Create(userName);

            var user = new User
                           {
                               CostCenter = profile.GetProperty<string>("CostCenter"),
                               FullName = profile.GetProperty<string>("FirstName") + " " + profile.GetProperty<string>("LastName"),
                               Manager = profile.GetProperty<string>("Manager"),
                               UserName = Membership.GetUser().UserName,
                               PreferredReimbursementMethod = string.IsNullOrEmpty(profile.GetProperty<string>("PreferredReimbursementMethod"))
                                                                  ? ReimbursementMethod.NotSet
                                                                  : (ReimbursementMethod)
                                                                    Enum.Parse(typeof (ReimbursementMethod), profile.GetProperty<string>("PreferredReimbursementMethod")),
                               Roles = new List<string>(roles)
                           };
            return user;
        }

        public void UpdateUserPreferredReimbursementMethod(User user)
        {
            var profile = ProfileBase.Create(user.UserName);

            profile.SetPropertyValue("PreferredReimbursementMethod", Enum.GetName(typeof (ReimbursementMethod), user.PreferredReimbursementMethod));
            profile.Save();
        }
    }
}