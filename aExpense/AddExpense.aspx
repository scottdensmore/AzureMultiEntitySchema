<%@ Page Title="" Language="C#" MasterPageFile="~/Styling/Site.Master" AutoEventWireup="true"
    CodeBehind="AddExpense.aspx.cs" Inherits="AExpense.AddExpense" Culture="en-US" ValidateRequest="true" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadPlaceholder" runat="server">
    <link type="text/css" href="Styling/styles/redmond/jquery-ui-1.8.custom.css" rel="stylesheet" />
    <script src="https://ajax.microsoft.com/ajax/jquery/jquery-1.4.2.js" type="text/javascript"></script>
    <script src="/Scripts/jquery-ui-1.8.custom.min.js" type="text/javascript"></script>
     <script type="text/javascript">
         $(function() {
            $(<%= "\"#" + this.ExpenseDate.ClientID + "\"" %>).datepicker();
         });
	</script>
	
	<script type="text/javascript" src="/Scripts/lib.multiupload.js"></script>
    <script type="text/javascript">
     var uploadBag = null;
     $(document).ready(function () {
        // multiple file upload bag
        uploadBag = new FileUploadBag(document.getElementById("uploadsContainer"), 1);
     });
    </script>
	
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceholder" runat="server">
    <form method="post" action="AddExpense.aspx">
    <div class="infoBox">
        <h2>
            Create a New Expense</h2>
        <p>
            Use the form below to report a new expense.
        </p>
    </div>
    <div id="expenseform">
        <p>
            <asp:Label ID="ExpenseDateLabel" AssociatedControlID="ExpenseDate" runat="server">
            Date:<br />
            <span>Select the expense date by clicking on the text box below.</span>
            </asp:Label>
            <br />
            <asp:TextBox ID="ExpenseDate" runat="server" MaxLength="10" />
            &nbsp;<asp:RequiredFieldValidator ID="ExpenseDateRequiredValidator" runat="server"
                ErrorMessage="*" ControlToValidate="ExpenseDate" />
            <asp:CompareValidator ID="ExpenseDateFormatValidator" runat="server" ErrorMessage="Enter a valid date to continue."
                ControlToValidate="ExpenseDate" Type="Date" Operator="DataTypeCheck" CultureInvariantValues="True" />
        </p>
        <p>
            <asp:Label ID="ExpenseTitleLabel" AssociatedControlID="ExpenseTitle" Text="Title:"
                runat="server" />
        <br />
            <asp:TextBox ID="ExpenseTitle" runat="server" MaxLength="100" />
            &nbsp;<asp:RequiredFieldValidator ID="ExpenseTitleValidator" runat="server" ErrorMessage="*"
                ControlToValidate="ExpenseTitle" />
        </p>
        <p>
            <asp:Label ID="ExpenseDetailsLabel" AssociatedControlID="ExpenseItemsGridView"
             Text="Details:" runat="server" />
            <br />
            <div style="float: left; display: inline-table; width: 80px">
                <ul id="uploadsContainer"></ul>
                <asp:FileUpload ID="dummy" runat="server" style="display: none" />
            </div>
            <div style="float: left; display: inline-table">
                <asp:Label ID="ExpenseItemDescriptionLabel" AssociatedControlID="ExpenseItemDescription" runat="server">
                    <span>Description:</span>
                </asp:Label>
                <asp:TextBox ID="ExpenseItemDescription" runat="server" MaxLength="100" />
                    &nbsp;<asp:RequiredFieldValidator ID="ExpenseItemDescriptionValidator" runat="server"
                        ErrorMessage="*" ControlToValidate="ExpenseItemDescription" ValidationGroup="AddNewExpenseItem" />
                <br />
                <br />
                <asp:Label ID="ExpenseItemAmountLabel" AssociatedControlID="ExpenseItemAmount" runat="server">
                    <span>Amount:</span>
                </asp:Label>
                &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                <asp:TextBox ID="ExpenseItemAmount" runat="server" CssClass="amountText" MaxLength="20" />
                &nbsp;<asp:RequiredFieldValidator ID="ExpenseAmountRequiredValidator" runat="server"
                    ErrorMessage="*" ControlToValidate="ExpenseItemAmount" ValidationGroup="AddNewExpenseItem" />
                <asp:CompareValidator ID="ExpenseItemAmountValidator" runat="server" ErrorMessage="Enter a valid amount to continue."
                    ControlToValidate="ExpenseItemAmount" Type="Currency" Operator="DataTypeCheck" ValidationGroup="AddNewExpenseItem" />

                <asp:Button ID="AddNewExpenseItem" runat="server" OnClick="OnAddNewExpenseItemClick" Text="˅ Add ˅"  ValidationGroup="AddNewExpenseItem" />
            </div>
            <div style="clear: both"></div>
            <br />
            <asp:GridView Width="85%" ID="ExpenseItemsGridView" runat="server" 
                          AutoGenerateColumns="False" AllowPaging="False" DataKeyNames="Id" EnableModelValidation="false"
                          OnRowDeleting="ExpenseItemsGridViewOnRowDeleting" OnRowDataBound="ExpenseItemsGridViewOnRowDataBound">
                <Columns>        
                    <asp:BoundField DataField="Description" HeaderText="Description" SortExpression="Description" />
                    <asp:TemplateField HeaderText="Amount" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" HeaderStyle-Width="100px">
                        <ItemTemplate><%# string.Format(System.Globalization.CultureInfo.CurrentUICulture, "${0}", Math.Round((double)Eval("Amount"), 2)) %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField ItemStyle-HorizontalAlign="Center" HeaderStyle-Width="25px">
                        <ItemTemplate>
                            <asp:ImageButton id="DeleteButton" CommandName="Delete" ImageUrl="~/Styling/Images/cancel.gif" runat="server" ValidationGroup="DeleteExpenseItem" />
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
                <HeaderStyle BackColor="#e6e6e6" />
                <EmptyDataTemplate>
                    No details added yet.
                </EmptyDataTemplate>
            </asp:GridView>
        </p>
        <p>
            <asp:Label ID="ExpenseReimbursementMethodLabel" AssociatedControlID="ExpenseReimbursementMethod"
                Text="Reimb. Method:" runat="server" />
        <br />
            <asp:DropDownList ID="ExpenseReimbursementMethod" runat="server" />
        </p>
        <p>
            <asp:Label ID="ExpenseCostCenterLabel" AssociatedControlID="ExpenseCostCenter" Text="Cost Center:"
                runat="server" />
        <br />
            <asp:Label ID="ExpenseCostCenter" runat="server" />
        </p>
        <p>
            <asp:Label ID="ApproverLabel" AssociatedControlID="Approver" Text="Approver:"
                runat="server" />
        <br />
            <asp:TextBox ID="Approver" runat="server" MaxLength="50" />
            &nbsp;<asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ErrorMessage="*"
                ControlToValidate="Approver" />
        </p>
        <div id="newexpense-button">
            <asp:Button ID="AddExpenseButton" runat="server" Text="Add »" OnClick="AddExpenseButtonOnClick" />
        </div>
    </div>
    </form>
</asp:Content>
