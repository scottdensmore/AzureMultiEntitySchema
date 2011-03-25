<%@ Page Title="" Language="C#" MasterPageFile="~/Styling/Site.Master" AutoEventWireup="true" CodeBehind="Approve.aspx.cs" 
    Inherits="AExpense.Approve" ValidateRequest="true" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadPlaceholder" runat="server" />

<asp:Content ID="Content2" ContentPlaceholderID="ContentPlaceholder" runat="server">

 <div id="expenselist">    
    <asp:GridView Width="100%" ID="MyExpensesGridView" runat="server" AutoGenerateColumns="False" DataSourceID="ExpensesDataSource" DataKeyNames="Id" OnRowDataBound="OnExpenseRowDataBound">        
        <Columns>        
            <asp:BoundField DataField="Date" HeaderText="Date" SortExpression="Date" DataFormatString="{0:MM/dd/yyyy}" ReadOnly="True" />
            <asp:BoundField DataField="Title" HeaderText="Title" SortExpression="Title" ReadOnly="True" />
            <asp:TemplateField HeaderText="Status" SortExpression="Approved"  ItemStyle-HorizontalAlign="Center">
                    <ItemTemplate>
                        <%# (Boolean.Parse(Eval("Approved").ToString())) ? "Ready for Processing" : "Pending for Approval"%>
                    </ItemTemplate>
                    <EditItemTemplate>
                         <asp:CheckBox runat="server" ID="Approved" Checked='<%# Bind("Approved") %>' Text=" Approved?" />                                                
                    </EditItemTemplate>
                </asp:TemplateField>
            <asp:CommandField ButtonType="Image" ShowEditButton="True" 
                CancelImageUrl="~/Styling/Images/cancel.gif" CancelText="Cancel"
                EditImageUrl="~/Styling/Images/edit.png" HeaderText="Edit" 
                UpdateImageUrl="~/Styling/Images/update.png" UpdateText="Update" 
                ItemStyle-HorizontalAlign="Center" />
        </Columns>
        <HeaderStyle BackColor="#e6e6e6" />       
        <EmptyDataTemplate>
            There are no expenses for approval.
        </EmptyDataTemplate>
    </asp:GridView>
 </div>
    
    <asp:ObjectDataSource ID="ExpensesDataSource" runat="server"
        DataObjectTypeName="AExpense.Data.Model.Expense" SelectMethod="GetExpensesForApproval" 
        TypeName="AExpense.Data.ExpenseRepository" 
        UpdateMethod="UpdateApproved" OnSelecting="OnExpensesSelecting">    
    </asp:ObjectDataSource>

</asp:Content>
