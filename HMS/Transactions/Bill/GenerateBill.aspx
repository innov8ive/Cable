<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="GenerateBill.aspx.cs" Inherits="HMS.Transactions.Bill.GenerateBill" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="True">
        </asp:ScriptManager>
        <div>
            <table>
                <tr>
                    <td>Bill Date
                    </td>
                    <td>
                        <sc:DatePicker runat="server" ID="billdateDatePicker" CssClass="CTextBox" Width="200px" />
                    </td>
                    <td>
                        <asp:Button ID="btnGenerate" runat="server" Text="Generate Bill" CssClass="btn1"
                            OnClick="btnGenerate_Click" OnClientClick="return generateBill();" />
                    </td>

                </tr>
                <tr>
                    <td colspan="3" id="tdProcess" runat="server" visible="false">
                        <asp:Button ID="btnProcess" runat="server" Text="" CssClass="btn1"
                            OnClick="btnProcess_Click" />
                    </td>
                </tr>
                <tr>
                    <td colspan="3" id="tdProcessEmail" runat="server" visible="false">
                        <asp:Button ID="btnProcessEmail" runat="server" Text="" CssClass="btn1"
                            OnClick="btnProcessEmail_Click" />
                    </td>
                </tr>
                <tr>
                    <td colspan="3">
                        <asp:Label ID="lbMessage" runat="server" Text="" Font-Bold="true"></asp:Label>
                    </td>
                </tr>
                <tr>
                    <td colspan="3">
                        <h3>
                            <asp:Label ID="lbGenerated" runat="server" Text=""></asp:Label>
                            Generated of 
                            <asp:Label ID="lbTotal" runat="server" Text=""></asp:Label>
                            Bill(s)
                        </h3>
                    </td>
                </tr>
            </table>
        </div>
        <asp:PlaceHolder ID="PlaceHolder1" runat="server">
            <script type="text/javascript">
                var totalBill = 0;
                var generatedBill = 0;
                var customers = [];
                function generateBill() {
                    var billDate = document.getElementById('<%=billdateDatePicker.ClientID%>').value;
                    if (billDate == '') {
                        alert('Please enter Bill Date');
                        return false;
                    }
                    PageMethods.StartGenerate(function (cust) {
                        customers = cust;
                        document.getElementById('<%=billdateDatePicker.ClientID%>').disabled = true;
                        totalBill = customers.length;
                        generatedBill = 0;
                        document.getElementById('<%=lbGenerated.ClientID%>').innerHTML = generatedBill;
                        document.getElementById('<%=lbTotal.ClientID%>').innerHTML = totalBill;
                        beginReq('<b>' + generatedBill + '</b> Generated of <b>' + totalBill + '</b> Bill(s)');
                        generateBillCustomer(customers[generatedBill]);
                    });
                    return false;
                }

                function generateBillCustomer(customerID) {
                    var billDate = document.getElementById('<%=billdateDatePicker.ClientID%>').value;
                    PageMethods.GenerateBillCustomer(customerID, billDate, function (result) {
                        generatedBill++;
                        document.getElementById('<%=lbGenerated.ClientID%>').innerHTML = generatedBill;
                        beginReq('<b>' + generatedBill + '</b> Generated of <b>' + totalBill + '</b> Bill(s)');
                        if (generatedBill >= totalBill) {
                            endReq();
                            document.getElementById('<%=billdateDatePicker.ClientID%>').disabled = false;
                            window.location = window.location;
                        }
                        else {
                            generateBillCustomer(customers[generatedBill]);
                        }
                    });
                }
            </script>
        </asp:PlaceHolder>

    </form>
</body>
</html>
