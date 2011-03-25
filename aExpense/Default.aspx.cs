using System.Web;

namespace AExpense
{
    using System;
    using System.Globalization;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using AExpense.Data;

    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!this.IsPostBack)
            {
                var repository = new ExpenseRepository();
                var expenses = repository.GetExpensesByUser(this.User.Identity.Name);
                this.MyExpensesGridView.DataSource = expenses;
                this.DataBind();
            }
        }

        protected void MyExpensesGridViewOnRowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "Select")
            {
                int selectedRow = Convert.ToInt32(e.CommandArgument);
                string expenseId = this.MyExpensesGridView.DataKeys[selectedRow].Value.ToString();
                string expenseDetailsUrl = string.Format(CultureInfo.InvariantCulture, "ExpenseDetails.aspx?id={0}", expenseId);
                this.Response.Redirect(expenseDetailsUrl);
            }
        }

        protected void MyExpensesGridViewOnRowDataBound(object sender, GridViewRowEventArgs eventArgs)
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
