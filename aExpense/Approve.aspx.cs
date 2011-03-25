namespace AExpense
{
    using System;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using AExpense.Data.Model;

    public partial class Approve : Page
    {
        protected void OnExpensesSelecting(object sender, ObjectDataSourceMethodEventArgs eventArgs)
        {
            eventArgs.InputParameters["approverName"] = User.Identity.Name;
        }

        protected void OnExpenseRowDataBound(object sender, GridViewRowEventArgs eventArgs)
        {
            var row = eventArgs.Row.DataItem as Expense;
            if (row != null)
            {
                if (row.Approved)
                {
                    eventArgs.Row.Enabled = false;
                }
            }

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
