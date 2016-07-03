using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Services;

namespace HMS.Reports
{
    public partial class Dashboard : SimpleBasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            UserSetting objSetting = (UserSetting)Session["UserSetting"];
            hdnOperatorID.Value = objSetting.OperatorID.ToString();
            Page.ClientScript.RegisterClientScriptInclude("commonJS", ResolveUrl("~/js/jquery-1.7.2.min.js"));
        }

        [WebMethod]
        public static double GetActiveCustomers(int operatorID)
        {
            return
                Common.ToDouble(
                    Common.GetDBScalarValue("Select COUNT(*) from Customers where IsActive=1 and OperatorID=" +
                                            operatorID));
        }

        [WebMethod]
        public static double GetDeActiveCustomers(int operatorID)
        {
            return
                Common.ToDouble(
                    Common.GetDBScalarValue("Select COUNT(*) from Customers where IsActive=0 and OperatorID=" +
                                            operatorID));
        }

        [WebMethod]
        public static double GetTotalPoints(int operatorID)
        {
            return
                Common.ToDouble(
                    Common.GetDBScalarValue("Select COUNT(*) from Customers where OperatorID=" +
                                            operatorID));
        }

        [WebMethod]
        public static double GetTotalTurnOver(int operatorID)
        {
            return
                Common.ToDouble(
                    Common.GetDBScalarValue(@"select ISNULL(SUM(NetBillAmount),0)-ISNULL(SUM(Bills.Outstanding),0) from Bills 
inner join Customers ON Bills.CustomerID=Customers.CustomerID
where Customers.OperatorID=" + operatorID));
        }

        [WebMethod]
        public static double GetCurrentBilling(int operatorID)
        {
            string query =
                @"select ISNULL(SUM(NetBillAmount),0) from Bills 
inner join Customers ON Bills.CustomerID=Customers.CustomerID
where Customers.OperatorID={0} and MONTH(Bills.BillDate)={1} and YEAR(Bills.BillDate)={2}";
            query = String.Format(query, operatorID, Common.GetCurDate().Month, Common.GetCurDate().Year);
            return Common.ToDouble(Common.GetDBScalarValue(query));
        }

        [WebMethod]
        public static double GetTotalEntTax(int operatorID)
        {
            return
                Common.ToDouble(
                    Common.GetDBScalarValue(@"select SUM(EntTax) from Bills 
inner join Customers ON Bills.CustomerID=Customers.CustomerID
where Customers.OperatorID=" + operatorID));
        }

        [WebMethod]
        public static double GetTotalCollection(int operatorID)
        {
            return
                Common.ToDouble(
                    Common.GetDBScalarValue(@"select ISNULL(SUM(CollectedAmount),0) from Bills 
inner join Customers ON Bills.CustomerID=Customers.CustomerID
where Customers.OperatorID=" + operatorID));
        }


        [WebMethod]
        public static double GetCurrentCollection(int operatorID)
        {
            string query =
                @"select ISNULL(SUM(CollectedAmount),0) from Bills 
inner join Customers ON Bills.CustomerID=Customers.CustomerID
where Customers.OperatorID={0} and MONTH(Bills.BillDate)={1} and YEAR(Bills.BillDate)={2}";
            query = String.Format(query, operatorID, Common.GetCurDate().Month, Common.GetCurDate().Year);
            return Common.ToDouble(Common.GetDBScalarValue(query));
        }

        [WebMethod]
        public static double GetTotalServiceTax(int operatorID)
        {
            return
                Common.ToDouble(
                    Common.GetDBScalarValue(@"select SUM(((BasicPrice+AddOnPrice)*ServiceTaxPerc)/100) from Bills 
inner join Customers ON Bills.CustomerID=Customers.CustomerID
where Customers.OperatorID=" + operatorID));
        }

        [WebMethod]
        public static double GetTotalOutstanding(int operatorID)
        {
//            return
//                Common.ToDouble(
//                    Common.GetDBScalarValue(@"select ISNULL(SUM(NetBillAmount),0)-ISNULL(SUM(CollectedAmount),0)
//-ISNULL(SUM(Bills.Outstanding),0)+ISNULL(SUM(StartOutstanding),0) from Bills 
//inner join Customers ON Bills.CustomerID=Customers.CustomerID
//where Customers.OperatorID=" + operatorID));
            return
               Common.ToDouble(
                   Common.GetDBScalarValue(@"select ISNULL(SUM(Customers.Outstanding),0) from Customers
where Customers.OperatorID=" + operatorID));
        }

        [WebMethod]
        public static double GetCurrentOutstanding(int operatorID)
        {
            string query =
                @"select ISNULL(SUM(NetBillAmount),0)-ISNULL(SUM(CollectedAmount),0) from Bills 
inner join Customers ON Bills.CustomerID=Customers.CustomerID
where Customers.OperatorID={0} and MONTH(Bills.BillDate)={1} and YEAR(Bills.BillDate)={2}";
            query = String.Format(query, operatorID, Common.GetCurDate().Month, Common.GetCurDate().Year);
            return Common.ToDouble(Common.GetDBScalarValue(query));
        }
    }
}