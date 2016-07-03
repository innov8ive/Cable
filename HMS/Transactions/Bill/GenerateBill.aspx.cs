using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using HMSOM;
using HMSBL;
using System.Threading.Tasks;
using System.Threading;
using System.Web.Services;

namespace HMS.Transactions.Bill
{
    public partial class GenerateBill : SimpleBasePage
    {
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            this.PageID = "8";
            //objSetting = (AppSettings)Session["AppSettings"];
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                int unProcessedSMS = Common.ToInt(Common.GetDBScalarValue(String.Format(@"select COUNT(*) from NotifyQueue where OperatorID={0} and NotifyBy='SMS'", UserSetting.OperatorID)));

                if (unProcessedSMS > 0)
                {
                    tdProcess.Visible = true;
                    btnProcess.Text = "Process " + unProcessedSMS + " SMS(s)";
                }

                int unProcessedEmail = Common.ToInt(Common.GetDBScalarValue(String.Format(@"select COUNT(*) from NotifyQueue where OperatorID={0} and NotifyBy='Email'", UserSetting.OperatorID)));

                if (unProcessedEmail > 0)
                {
                    tdProcessEmail.Visible = true;
                    btnProcessEmail.Text = "Process " + unProcessedEmail + " Email(s)";
                }

                UserSetting objSetting = (UserSetting)Session["UserSetting"];
                int totalBill = Common.ToInt(Common.GetDBScalarValue("Select count(*) from Customers C where C.IsActive=1 and C.OperatorID=" + objSetting.OperatorID));
                int generatedBill = Common.ToInt(Common.GetDBScalarValue(@"select COUNT(*) from Bills where DATEPART(MM,BillDate)=DATEPART(MM,GETDATE())
AND DATEPART(YYYY,BillDate)=DATEPART(YYYY,GETDATE()) and CustomerID in
(select CustomerID from Customers where IsActive=1 and OperatorID=" + objSetting.OperatorID + ")"));

                lbGenerated.Text = generatedBill.ToString();
                lbTotal.Text = totalBill.ToString();
            }
        }
        protected void btnGenerate_Click(object sender, EventArgs e)
        {
            if (billdateDatePicker.Text.Trim().Length <= 0)
            {
                Common.ShowMessage("Please enter Bill Date.");
                return;
            }
            UserSetting objSetting = (UserSetting)Session["UserSetting"];
            DataTable dtCustomer = Common.GetDBResult(@"Select CustomerID,
            case EmailEnabled when 1 then EmailID else '' end as EmailID from Customers C where C.IsActive=1 and C.OperatorID=" + objSetting.OperatorID);

            /*
             Select CustomerID,
dbo.GetOutstanding(C.CustomerID,0) as PreOutstanding 
from Customers C where C.IsActive=1 and C.OperatorID=23
and C.CustomerID not in (select customerID from Bills where 
BillDate='2016-02-01' and PaymentDate is not null)
            */
            //btnGenerate.Text = "Dont Generate!";
            //            DataTable dtCustomer = Common.GetDBResult(@"Select CustomerID,
            //dbo.GetOutstanding(C.CustomerID,0) as PreOutstanding 
            //from Customers C where C.IsActive=1 and C.OperatorID=23
            //and C.CustomerID not in (
            //select CT.customerID from Bills B inner join Customers CT ON B.CustomerID = CT.CustomerID
            //where B.BillDate='2016-02-01' and CT.OperatorID=23)");
            BillsBL objBillsBL = new BillsBL(Common.GetConString());
            foreach (DataRow dr in dtCustomer.Rows)
            {
                int customerID = Common.ToInt(dr["CustomerID"]);
                DataTable dtPackages = Common.GetDBResult(@"Select dbo.GetOutstanding(C.CustomerID,0) as PreOutstanding,C.CustomerID,C.PackageID,C.Discount as CustDiscount
            ,P.BasicPrice as BasicPrice,P.Discount as PkgDiscount,
            P.EntTax as EntTax,P.AddOnPrice as AddOnPrice,P.ServiceTaxPerc
            from CustomerPackages C
            inner join VW_Packages P ON C.PackageID=P.PackageID 
            where C.CustomerID=" + customerID);

                decimal addOn = 0;
                decimal basic = 0;
                decimal discount = 0;
                decimal entTax = 0;
                decimal serviceTaxPerc = 0;
                decimal total = 0;
                decimal outstanding = 0;
                if (dtPackages.Rows.Count > 0)
                    outstanding = Common.ToDecimal(dtPackages.Rows[0]["PreOutstanding"]);
                foreach (DataRow drPkg in dtPackages.Rows)
                {
                    decimal temp_basic = Common.ToDecimal(drPkg["BasicPrice"]);
                    basic += temp_basic;

                    decimal temp_discount = Common.ToDecimal(drPkg["CustDiscount"]) + Common.ToDecimal(drPkg["PkgDiscount"]);
                    discount += temp_discount;

                    decimal temp_entTax = Common.ToDecimal(drPkg["EntTax"]);
                    entTax += temp_entTax;

                    serviceTaxPerc = Common.ToDecimal(drPkg["ServiceTaxPerc"]);
                    decimal temp_addon = Common.ToDecimal(drPkg["AddOnPrice"]);
                    addOn += temp_addon;

                    decimal temp_total = temp_basic + temp_addon;
                    temp_total = temp_total + (temp_total * serviceTaxPerc) / 100;
                    temp_total = temp_total + temp_entTax - temp_discount;


                    total += Math.Ceiling(temp_total);
                }
                total += outstanding;

                Bills objBills = new Bills();
                objBills.AddOnPrice = addOn;
                objBills.BillDate = Common.ToDate(billdateDatePicker.Text);
                objBills.BasicPrice = basic;
                objBills.CustomerID = customerID;
                objBills.Discount = discount;
                objBills.EntTax = entTax;
                objBills.GeneratedBy = objSetting.UserID;
                objBills.GeneratedDate = Common.GetCurDate();
                objBills.ServiceTaxPerc = serviceTaxPerc;
                objBills.Outstanding = outstanding;
                objBills.NetBillAmount = total;
                objBillsBL.Data = objBills;
                objBillsBL.Update();
                Common.UpdateCustomerOutstanding(customerID);
                if (objSetting.SMSService && Common.ToBool(objBillsBL.Data.BillInsert))
                    Common.InsertNotifyQueue(Common.ToInt(objBillsBL.Data.BillID), customerID, "SMS", "Bill", objSetting.OperatorID);

                string emailID = Common.ToString(dr["EmailID"]);
                if (emailID.Length > 0)
                    Common.InsertNotifyQueue(Common.ToInt(objBillsBL.Data.BillID), customerID, "Email", "Bill", objSetting.OperatorID);

            }
            lbMessage.Text = "Bill generated successfully. To view generated bills, please go to 'Pending Bills'";

            Thread obj = new Thread(ProcessEmailQueue);
            obj.Start();
            Thread obj2 = new Thread(ProcessSMSQueue);
            obj2.Start();
        }
        [WebMethod]
        public static bool GenerateBillCustomer(int customerID,string billDate)
        {
            try
            {
                string emailID = Common.ToString(Common.GetDBScalarValue(@"select case EmailEnabled when 1 then EmailID else '' end as EmailID from Customers C where C.CustomerID=" + customerID));
                UserSetting objSetting = (UserSetting)HttpContext.Current.Session["UserSetting"];
                BillsBL objBillsBL = new BillsBL(Common.GetConString());
                DataTable dtPackages = Common.GetDBResult(@"Select dbo.GetOutstanding(C.CustomerID,0) as PreOutstanding,C.CustomerID,C.PackageID,C.Discount as CustDiscount
            ,P.BasicPrice as BasicPrice,P.Discount as PkgDiscount,
            P.EntTax as EntTax,P.AddOnPrice as AddOnPrice,P.ServiceTaxPerc
            from CustomerPackages C
            inner join VW_Packages P ON C.PackageID=P.PackageID 
            where C.CustomerID=" + customerID);

                decimal addOn = 0;
                decimal basic = 0;
                decimal discount = 0;
                decimal entTax = 0;
                decimal serviceTaxPerc = 0;
                decimal total = 0;
                decimal outstanding = 0;
                if (dtPackages.Rows.Count > 0)
                    outstanding = Common.ToDecimal(dtPackages.Rows[0]["PreOutstanding"]);
                foreach (DataRow drPkg in dtPackages.Rows)
                {
                    decimal temp_basic = Common.ToDecimal(drPkg["BasicPrice"]);
                    basic += temp_basic;

                    decimal temp_discount = Common.ToDecimal(drPkg["CustDiscount"]) + Common.ToDecimal(drPkg["PkgDiscount"]);
                    discount += temp_discount;

                    decimal temp_entTax = Common.ToDecimal(drPkg["EntTax"]);
                    entTax += temp_entTax;

                    serviceTaxPerc = Common.ToDecimal(drPkg["ServiceTaxPerc"]);
                    decimal temp_addon = Common.ToDecimal(drPkg["AddOnPrice"]);
                    addOn += temp_addon;

                    decimal temp_total = temp_basic + temp_addon;
                    temp_total = temp_total + (temp_total * serviceTaxPerc) / 100;
                    temp_total = temp_total + temp_entTax - temp_discount;


                    total += Math.Ceiling(temp_total);
                }
                total += outstanding;

                Bills objBills = new Bills();
                objBills.AddOnPrice = addOn;
                objBills.BillDate = Common.ToDate(billDate);
                objBills.BasicPrice = basic;
                objBills.CustomerID = customerID;
                objBills.Discount = discount;
                objBills.EntTax = entTax;
                objBills.GeneratedBy = objSetting.UserID;
                objBills.GeneratedDate = Common.GetCurDate();
                objBills.ServiceTaxPerc = serviceTaxPerc;
                objBills.Outstanding = outstanding;
                objBills.NetBillAmount = total;
                objBillsBL.Data = objBills;
                objBillsBL.Update();
                Common.UpdateCustomerOutstanding(customerID);
                if (objSetting.SMSService && Common.ToBool(objBillsBL.Data.BillInsert))
                    Common.InsertNotifyQueue(Common.ToInt(objBillsBL.Data.BillID), customerID, "SMS", "Bill", objSetting.OperatorID);

                if (emailID.Length > 0)
                    Common.InsertNotifyQueue(Common.ToInt(objBillsBL.Data.BillID), customerID, "Email", "Bill", objSetting.OperatorID);
            }
            catch
            {
                return false;
            }
            return true;
        }
        [WebMethod]
        public static List<int> StartGenerate()
        {
            UserSetting objSetting = (UserSetting)HttpContext.Current.Session["UserSetting"];
            DataTable dtCustomer = Common.GetDBResult(@"Select CustomerID from Customers C where C.IsActive=1 and C.OperatorID=" + objSetting.OperatorID);
            List<int> list = new List<int>();
            foreach (DataRow dr in dtCustomer.Rows)
            {
                list.Add(Common.ToInt(dr["CustomerID"]));
            }
            return list;
        }
        private void ProcessEmailQueue()
        {
            Common.ProcessEmailQueue(UserSetting.OperatorID);
        }
        private void ProcessSMSQueue()
        {
            Common.SendBillSMS(UserSetting.OperatorID);
        }
        private void ProcessPaymentSMSQueue()
        {
            Common.SendPaymentSMS(UserSetting.OperatorID);
        }
        protected void btnProcess_Click(object sender, EventArgs e)
        {
            Thread obj2 = new Thread(ProcessSMSQueue);
            obj2.Start();
            Thread obj3 = new Thread(ProcessPaymentSMSQueue);
            obj3.Start();
            lbMessage.Text = "SMS Processed Successfully";
            tdProcess.Visible = false;
            tdProcessEmail.Visible = false;
        }
        protected void btnProcessEmail_Click(object sender, EventArgs e)
        {
            Thread obj2 = new Thread(ProcessEmailQueue);
            obj2.Start();
            lbMessage.Text = "Email Processed Successfully";
            tdProcess.Visible = false;
            tdProcessEmail.Visible = false;
        }
    }
}