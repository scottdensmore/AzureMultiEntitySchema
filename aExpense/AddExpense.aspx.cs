namespace AExpense
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using AExpense.Data;
    using AExpense.Data.Model;
    using AExpense.Data.Storage;


    public partial class AddExpense : Page
    {
        private List<ExpenseItem> ExpenseItems
        {
            get
            {
                if (this.Session["ExpenseItems"] == null)
                {
                   this.Session["ExpenseItems"] = new List<ExpenseItem>();
                }

                return (List<ExpenseItem>) this.Session["ExpenseItems"];
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                this.Session["ExpenseItems"] = null;
                this.InitializeControls();
            }
        }

        protected void AddExpenseButtonOnClick(object sender, EventArgs e)
        {
            if (this.IsValid)
            {
                this.SaveExpense();
                Response.Redirect("~/Default.aspx", true);
            }
        }

        protected void OnAddNewExpenseItemClick(object sender, EventArgs e)
        {
            this.Validate("AddNewExpenseItem");
            // Here must be placed all the extra validations for the inputs 
            // (like length, potentially dangerous characters, etc.)
            if (this.IsValid)
            {
                this.ExpenseItems.Add(
                    new ExpenseItem
                        {
                            Id = StorageKey.Now.InvertedTicks,
                            Description = this.ExpenseItemDescription.Text,
                            Amount = double.Parse(this.ExpenseItemAmount.Text, CultureInfo.CurrentUICulture),
                            Receipt = ReadBytes(this.Request.Files["upload_1"].InputStream)
                        });

                this.ExpenseItemDescription.Text = string.Empty;
                this.ExpenseItemAmount.Text = string.Empty;
            }

            this.ExpenseItemsGridView.DataSource = this.ExpenseItems;
            this.ExpenseItemsGridView.DataBind();
        }

        protected void ExpenseItemsGridViewOnRowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            this.ExpenseItems.RemoveAt(e.RowIndex);
            this.ExpenseItemsGridView.DataSource = this.ExpenseItems;
            this.ExpenseItemsGridView.DataBind();
        }

        protected void ExpenseItemsGridViewOnRowDataBound(object sender, GridViewRowEventArgs eventArgs)
        {
            foreach (TableCell cell in eventArgs.Row.Cells)
            {
                if (!string.IsNullOrEmpty(cell.Text) && !cell.Text.Equals("&nbsp;"))
                {
                    cell.Text = Server.HtmlEncode(cell.Text);
                }
            }
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            this.ViewStateUserKey = this.User.Identity.Name;
        }

        private static byte[] ReadBytes(Stream input)
        {
            if (input == null)
            {
                return null;
            }
        
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }

                return ms.ToArray();
            }
        }

        private void SaveExpense()
        {
            var userRepository = new UserRepository();
            var user = userRepository.GetUser(this.User.Identity.Name);
            
            var approverName = this.Approver.Text;

            if (string.IsNullOrEmpty(approverName))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "The approver {0} does not exists", this.Approver.Text));
            }

            var newExpense = new Expense
            {
                Id = StorageKey.Now.InvertedTicks,
                Title = this.ExpenseTitle.Text,
                CostCenter = user.CostCenter,
                Approved = false,
                ReimbursementMethod = (ReimbursementMethod)Enum.Parse(typeof(ReimbursementMethod), this.ExpenseReimbursementMethod.SelectedItem.Value),
                UserName = user.UserName,
                Date = DateTime.Parse(this.ExpenseDate.Text, CultureInfo.CurrentUICulture),
                ApproverName = approverName
            };
            this.ExpenseItems.ForEach(ei => newExpense.Details.Add(ei));

            var expenseRepository = new ExpenseRepository();
            expenseRepository.SaveExpense(newExpense);

            user.PreferredReimbursementMethod = (ReimbursementMethod)Enum.Parse(typeof(ReimbursementMethod), this.ExpenseReimbursementMethod.SelectedValue);
            userRepository.UpdateUserPreferredReimbursementMethod(user);
        }

        private void InitializeControls()
        {
            var userRepository = new UserRepository();
            var user = userRepository.GetUser(this.User.Identity.Name);
            if (user == null)
            {
                throw new InvalidOperationException("User not exists");
            }

            this.ExpenseReimbursementMethod.Items.Add(new ListItem("Check", ReimbursementMethod.Check.ToString()));
            this.ExpenseReimbursementMethod.Items.Add(new ListItem("Cash", ReimbursementMethod.Cash.ToString()));
            this.ExpenseReimbursementMethod.Items.Add(new ListItem("Direct Deposit", ReimbursementMethod.DirectDeposit.ToString()));
            if (user.PreferredReimbursementMethod != ReimbursementMethod.NotSet)
            {
                this.ExpenseReimbursementMethod.SelectedValue = user.PreferredReimbursementMethod.ToString();
            }

            this.ExpenseCostCenter.Text = Server.HtmlEncode(user.CostCenter);
            this.Approver.Text = user.Manager;

            this.ExpenseItemsGridView.DataSource = this.ExpenseItems;
            this.ExpenseItemsGridView.DataBind();
        }
    }
}
