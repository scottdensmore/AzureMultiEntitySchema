namespace AExpense
{
    using System;
    using System.Globalization;
    using System.Web.UI.WebControls;
    using AExpense.Data;
    using AExpense.Data.Model;
    using AExpense.Data.Storage;

    public partial class ExpenseDetails : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        { 
            if (!IsPostBack)
            {
                string storageId;
                try
                {
                    storageId = this.Request.QueryString["id"];
                }
                catch (ArgumentNullException exception)
                {
                    Log.Write(EventKind.Error, exception.Message);
                    throw;
                }

                var expenseRepository = new ExpenseRepository(TimeSpan.FromMinutes(this.Session.Timeout));
                Expense expense = expenseRepository.GetExpenseById(this.User.Identity.Name, storageId);

                if (expense == null)
                {
                    string errorMessage = string.Format(CultureInfo.CurrentCulture, "There is no expense with the id {0}.", storageId);
                    Log.Write(EventKind.Error, errorMessage);
                    throw new ArgumentException(errorMessage);
                }

                if (expense.User.UserName != this.User.Identity.Name)
                {
                    string errorMessage = string.Format("{0} cannot access the expense with id {1}.", this.User.Identity.Name, expense.Id);
                    throw new UnauthorizedAccessException(errorMessage);
                }

                this.ExpenseDate.Text = expense.Date.ToString("yyyy-MM-dd");
                this.ExpenseTitle.Text = Server.HtmlEncode(expense.Title);
                this.ExpenseItemsGridView.DataSource = expense.Details;
                this.ExpenseItemsGridView.DataBind();
                this.ExpenseReimbursementMethod.Text = Server.HtmlEncode(Enum.GetName(typeof(ReimbursementMethod), expense.ReimbursementMethod));
                this.ExpenseCostCenter.Text = Server.HtmlEncode(expense.CostCenter);
                this.Approver.Text = Server.HtmlEncode(expense.ApproverName);
            }
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
    }
}
