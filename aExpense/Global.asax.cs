using System;
using System.Web;
using System.Web.Profile;
using System.Web.Security;

namespace AExpense
{
    using AExpense.Data;
    using AExpense.Data.Model;

    public class Global : HttpApplication
    {
        private void Application_EndRequest(object sender, EventArgs e)
        {
            if (Response.StatusCode == 401)
            {
                Response.ClearContent();
                Server.Transfer("~/401.htm");
            }
        }

        private void Application_Error(object sender, EventArgs e)
        {
            // Get reference to the source of the exception chain
            Exception ex = Server.GetLastError();
            Log.Write(EventKind.Error, ex.TraceInformation());
        }

        private void Application_Start(object sender, EventArgs e)
        {
            SetupUsersAndRoles();


            ProfileInitializer.Initialize();
        }

        private static void SetupUsersAndRoles()
        {
            if (!Roles.RoleExists("Employee"))
            {
                Roles.CreateRole("Employee");
            }

            if (!Roles.RoleExists("Manager"))
            {
                Roles.CreateRole("Manager");
            }

            if (Membership.GetUser(@"ADATUM\johndoe") == null)
            {
                MembershipUser membershipUser = Membership.CreateUser(@"ADATUM\johndoe", "password");
                Roles.AddUserToRole(membershipUser.UserName, "Employee");
            }

            if (Membership.GetUser(@"ADATUM\mary") == null)
            {
                MembershipUser membershipUser = Membership.CreateUser(@"ADATUM\mary", "password");
                Roles.AddUserToRole(membershipUser.UserName, "Manager");
            }

            if (Membership.GetUser(@"ADATUM\bob") == null)
            {
                MembershipUser membershipUser = Membership.CreateUser(@"ADATUM\bob", "password");
                Roles.AddUserToRole(membershipUser.UserName, "Employee");
            }
        }

        public static class ProfileInitializer
        {
            //private const string ApplicationName = "aExpense";

            public static void Initialize()
            {
                CreateProfile();
            }

            private static void CreateProfile()
            {
                //var provider = new TableStorageProfileProvider();
                //provider.Initialize("TableStorageProfileProvider", new NameValueCollection { { "applicationName", ApplicationName } });

                string username = @"ADATUM\johndoe";
                ReimbursementMethod reimbursementMethod = ReimbursementMethod.Cash;
                CreateUserInProfile(username, reimbursementMethod, "John", "Doe", @"ADATUM\mary", CostCenters.CustomerService);

                username = @"ADATUM\mary";
                reimbursementMethod = ReimbursementMethod.Check;
                CreateUserInProfile(username, reimbursementMethod, "Mary", "May", @"ADATUM\bob", CostCenters.CustomerService);

                username = @"ADATUM\bob";
                reimbursementMethod = ReimbursementMethod.Check;
                CreateUserInProfile(username, reimbursementMethod, "Bob", "Smith", "", CostCenters.CustomerService);
            }

            private static void CreateUserInProfile(string username, ReimbursementMethod reimbursementMethod, string firstName, string lastName, string manager, string costCenter)
            {
                var profile = ProfileBase.Create(username);
                profile.SetPropertyValue("PreferredReimbursementMethod", Enum.GetName(typeof (ReimbursementMethod), reimbursementMethod));
                profile.SetPropertyValue("FirstName", firstName);
                profile.SetPropertyValue("LastName", lastName);
                profile.SetPropertyValue("CostCenter", costCenter);
                profile.SetPropertyValue("Manager", manager);
                profile.Save();
            }
        }
    }
}