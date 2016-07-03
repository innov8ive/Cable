using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using System.Web.Services;
using System.Data;
using System.Text;
using System.Data.SqlClient;
using HMSOM;

namespace HMS
{
    //Changes 
    //Sanjay 14 Jun 2013 (Made "DataUploader" class as Generic Uploader)
    //Sanjay 14 Jun 2013 (in "ProductDataUploader", added "ProductMaster" instead of "WH.Commodity"
    public partial class UploadOutstanding : System.Web.UI.Page
    {
        #region Declaration
        static DataTable dtProduct = new DataTable();
        static string dataFileName = String.Empty;
        static CustomerDataUploader2 _objCustomerDataUploader;
        #endregion

        #region Page Events
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindOperator(ddlOperator);
            }
        }
        #endregion

        #region Control Events
        protected void btnDownload_Click(object sender, EventArgs e)
        {
            string attachment = "attachment; filename=Format_" + DateTime.Now.ToString("dd_MM_yyyy_hh_ss") + ".csv";
            HttpContext.Current.Response.Clear();
            HttpContext.Current.Response.ClearContent();
            HttpContext.Current.Response.ClearHeaders();
            HttpContext.Current.Response.Write("FirstName,MiddleName,LastName,Address,Area,City,State,Pincode,Mobile,Email,CANNo,STBNo,SMCNo,ConnectionType,CustomerStatus,ServiceProvider,Outstanding,Package");
            HttpContext.Current.Response.ContentType = "text/csv";
            HttpContext.Current.Response.AppendHeader("Content-Disposition", attachment);
            HttpContext.Current.Response.End();
        }
        protected void btnUpdoad_Click(object sender, EventArgs e)
        {
            UserSetting objSetting = (UserSetting)Session["UserSetting"];
            if (csvFileUpload.HasFile == false)
            {
                Common.ShowMessage("Please select data file.");
            }
            string fileName = Server.MapPath("~/NoteAttachments/") + DateTime.Now.ToString("yyyy_MMM_dd_hh_mm_tt");
            hdnFileName.Value = fileName;
            uxStatuslb.Text = "Uploading data file...";
            //First save the file
            csvFileUpload.SaveAs(fileName);
            dataFileName = fileName;

            _objCustomerDataUploader = new CustomerDataUploader2("dbo.Customers", "CustomerID", Common.GetConString(), objSetting.OperatorID);
            string message = String.Empty;

            //Check data is valid 
            bool isValid = _objCustomerDataUploader.IsDataValidForInsert(ref message, dataFileName, objSetting.OperatorID);
            if (isValid)
            {
                dtProduct = _objCustomerDataUploader.Data;
            }
            if (dtProduct != null && isValid)//Sanjay 04 May 2013 (if data is valid)
            {
                //UpdateCustomerOutstanding2
                Common.ExecuteNonQuery(String.Format("exec UpdateCustomerOutstanding2 {0}", Common.ToInt(ddlOperator.SelectedValue)));
                //Updating Data
                foreach (DataRow dr in dtProduct.Rows)
                {
                    Common.ExecuteNonQuery(String.Format("exec UpdateCustomerOutstanding3 '{0}','{1}','{2}',{3}",
Common.ToString(dr["FirstName"]), Common.ToString(dr["MiddleName"]), Common.ToString(dr["LastName"]),
Common.ToDecimal(dr["Outstanding"])));
                   
                }

                try
                {
                    uxStatuslb.Text = "Success Rows Inserted: " + _objCustomerDataUploader.TotalSuccessInsert
                        + ", Failed Rows : " + _objCustomerDataUploader.TotalFailedInsert;
                }
                catch (Exception Ex)
                {
                    uxStatuslb.Text = Ex.Message;
                    message = Ex.Message;
                }
            }
            uxMessagetxt.InnerHtml = message;

            //Delete file from temp storage
            DeleteFile();

            //ScriptManagerHelper.RegisterStartupScript(this, "validateandsave", "ValidatingData();", true);
        }
        private void BindOperator(DropDownList ddl)
        {
            DataTable dt = Common.GetDBResult("select operatorID,OperatorName from operators Order By OperatorName");
            ddl.DataSource = dt;
            ddl.DataTextField = "OperatorName";
            ddl.DataValueField = "operatorID";
            ddl.DataBind();

            ddl.Items.Insert(0, new ListItem("(Select)", "0"));
        }
        #endregion

        #region Page Methods
        [WebMethod]
        public static string ValidatingData(int companyID)
        {
            _objCustomerDataUploader = new CustomerDataUploader2("dbo.ProductMaster", "ProdID", Common.GetConString(), 1);
            string message = String.Empty;
            //Check data is valid 
            bool isValid = _objCustomerDataUploader.IsDataValidForInsert(ref message, dataFileName, companyID);
            if (isValid)
            {
                dtProduct = _objCustomerDataUploader.Data;
            }
            return message;
        }
        [WebMethod]
        public static string SavingInDatabase(int companyID)
        {
            string message = String.Empty;
            if (dtProduct != null)
            {
                //Adding CompanyID and IsActive Columns
                dtProduct.Columns.Add(new DataColumn() { ColumnName = "CompanyID", DataType = typeof(int) });
                dtProduct.Columns.Add(new DataColumn() { ColumnName = "IsActive", DataType = typeof(bool) });

                //Updating Data
                foreach (DataRow dr in dtProduct.Rows)
                {
                    dr["CompanyID"] = companyID;
                    dr["IsActive"] = true;
                }


                //System.Threading.Thread.Sleep(2000);
                _objCustomerDataUploader.InsertInDatabase(dtProduct);
                return "Success Rows Inserted: " + _objCustomerDataUploader.TotalSuccessInsert
                    + ", Failed Rows : " + _objCustomerDataUploader.TotalFailedInsert;
            }
            return String.Empty;
        }
        [WebMethod]
        public static void DeleteFile()
        {
            //Delete the temporary uploaded file
            try
            {
                FileInfo file = new FileInfo(dataFileName);
                if (file.Exists) file.Delete();
            }
            catch { }
        }

        #endregion
    }
    public class CustomerDataUploader2 : DataUploader
    {
        private int OperatorID;
        #region Constructor
        private CustomerDataUploader2()
        {
        }
        public CustomerDataUploader2(string tableName, string identityColumn, string conString, int oprID)
        {
            TableName = tableName;
            ConnectionString = conString;
            IdentityColumn = identityColumn;
            OperatorID = oprID;
            InitCustomerDataUploader();
            this.OnAfterRowInsert += new AfterRowInsert(ProductDataUploader_OnAfterRowInsert);
        }
        #endregion

        #region Events
        private bool InsertIntoCustomerPackages(CustomerPackages objCustomerPackages, SqlTransaction trn)
        {
            SqlCommand cmd = new SqlCommand("Insert_Into_CustomerPackages", trn.Connection);
            try
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Transaction = trn;
                cmd.Parameters.Add("@CANNo", SqlDbType.VarChar, 25).Value = objCustomerPackages.CANNo;
                cmd.Parameters.Add("@ConnectionType", SqlDbType.VarChar, 10).Value = objCustomerPackages.ConnectionType;
                cmd.Parameters.Add("@CustomerID", SqlDbType.Int).Value = objCustomerPackages.CustomerID;
                cmd.Parameters.Add("@Discount", SqlDbType.Decimal).Value = objCustomerPackages.Discount;
                cmd.Parameters.Add("@PackageID", SqlDbType.Int).Value = objCustomerPackages.PackageID;
                cmd.Parameters.Add("@ServiceProviderID", SqlDbType.Int).Value = objCustomerPackages.ServiceProviderID;
                cmd.Parameters.Add("@SmartCardNo", SqlDbType.VarChar, 25).Value = objCustomerPackages.SmartCardNo;
                cmd.Parameters.Add("@STBMakeID", SqlDbType.Int).Value = objCustomerPackages.STBMakeID;
                cmd.Parameters.Add("@STBNo", SqlDbType.VarChar, 50).Value = objCustomerPackages.STBNo;
                cmd.Parameters.Add("@TotalPrice", SqlDbType.Decimal).Value = objCustomerPackages.TotalPrice;
                cmd.Parameters.Add("@SrNo", SqlDbType.Int).Value = objCustomerPackages.SrNo;

                cmd.ExecuteNonQuery();

                return true;
            }
            catch
            {
                trn.Rollback();
                return false;
            }
            finally
            {
                cmd.Dispose();
            }
        }
        void ProductDataUploader_OnAfterRowInsert(DataRow dr, SqlTransaction trn, int lastID)
        {
            //Update UniqueID
            SqlCommand cmd = new SqlCommand("UpdateUniqueID");
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Connection = trn.Connection;
            cmd.Transaction = trn;
            cmd.Parameters.Add("@CustomerID", SqlDbType.Int).Value = lastID;
            cmd.Parameters.Add("@OperatorID", SqlDbType.Int).Value = Common.ToInt(dr["OperatorID"]);
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch
            {
                trn.Rollback();
            }

            //(Set QtyUnit=UOM)
            CustomerPackages objCustomerPackages = new CustomerPackages();
            objCustomerPackages.CANNo = Common.ToString(dr["CANNo"]);
            objCustomerPackages.ConnectionType = Common.ToString(dr["ConnectionType"]);
            objCustomerPackages.CustomerID = lastID;
            objCustomerPackages.Discount = 0;
            objCustomerPackages.PackageID = Common.ToInt(dr["Package"]);
            objCustomerPackages.STBNo = Common.ToString(dr["STBNo"]);
            objCustomerPackages.ServiceProviderID = Common.ToInt(dr["ServiceProvider"]);
            objCustomerPackages.SmartCardNo = Common.ToString(dr["SmartCardNo"]);
            objCustomerPackages.SrNo = 1;
            objCustomerPackages.TotalPrice = 0;
            InsertIntoCustomerPackages(objCustomerPackages, trn);
        }
        #endregion

        #region Override Methods
        private List<UploadColumn> _Columns;
        public override List<UploadColumn> Columns
        {
            get
            {
                return _Columns;
            }
            set
            {
                _Columns = value;
            }
        }
        #endregion

        #region Private Methods
        private void InitCustomerDataUploader()
        {
            List<ReferenceColumn> objSPList = GetServiceProviderList();
            List<ReferenceColumn> objPkgList = GetPackageList();
            List<ReferenceColumn> objConTypeList = GetConnectionTypeList();
            List<ReferenceColumn> objStatusList = GetStatusList();
            this.Columns = new List<UploadColumn>();

            //Add Manadatory columns
            this.Columns.Add(new UploadColumn() { ColumnName = "FirstName", ColumnType = "System.String", IsManadatory = true, DataColumnName = "FirstName", MaxLength = 100 });
            this.Columns.Add(new UploadColumn() { ColumnName = "MiddleName", ColumnType = "System.String", IsManadatory = false, DataColumnName = "MiddleName", MaxLength = 100 });
            this.Columns.Add(new UploadColumn() { ColumnName = "LastName", ColumnType = "System.String", IsManadatory = false, DataColumnName = "LastName", MaxLength = 100 });
            this.Columns.Add(new UploadColumn() { ColumnName = "Address", ColumnType = "System.String", IsManadatory = false, DataColumnName = "Address1", MaxLength = 500 });
            this.Columns.Add(new UploadColumn() { ColumnName = "Area", ColumnType = "System.String", IsManadatory = false, DataColumnName = "Area", MaxLength = 100 });
            this.Columns.Add(new UploadColumn() { ColumnName = "City", ColumnType = "System.String", IsManadatory = true, DataColumnName = "City", MaxLength = 100 });
            this.Columns.Add(new UploadColumn() { ColumnName = "State", ColumnType = "System.String", IsManadatory = true, DataColumnName = "State", MaxLength = 100 });
            this.Columns.Add(new UploadColumn() { ColumnName = "PinCode", ColumnType = "System.String", IsManadatory = true, DataColumnName = "PinCode", MaxLength = 100 });
            this.Columns.Add(new UploadColumn() { ColumnName = "Mobile", ColumnType = "System.String", IsManadatory = false, DataColumnName = "MobileNo", MaxLength = 12 });
            this.Columns.Add(new UploadColumn() { ColumnName = "Email", ColumnType = "System.String", IsManadatory = false, DataColumnName = "EmailID", MaxLength = 100 });
            this.Columns.Add(new UploadColumn() { ColumnName = "CANNo", ColumnType = "System.String", IsManadatory = false, DataColumnName = "CANNo", MaxLength = 25 });
            this.Columns.Add(new UploadColumn() { ColumnName = "STBNo", ColumnType = "System.String", IsManadatory = false, DataColumnName = "STBNo", MaxLength = 50 });
            this.Columns.Add(new UploadColumn() { ColumnName = "SMCNo", ColumnType = "System.String", IsManadatory = false, DataColumnName = "SmartCardNo", MaxLength = 25 });
            this.Columns.Add(new UploadColumn() { ColumnName = "ConnectionType", ColumnType = "System.String", IsManadatory = true, DataColumnName = "ConnectionType", MaxLength = 10, RefList = objConTypeList });
            this.Columns.Add(new UploadColumn() { ColumnName = "CustomerStatus", ColumnType = "System.String", IsManadatory = true, DataColumnName = "IsActive", MaxLength = 10, RefList = objStatusList });
            this.Columns.Add(new UploadColumn() { ColumnName = "ServiceProvider", ColumnType = "System.String", IsManadatory = true, DataColumnName = "ServiceProvider", MaxLength = 50, RefList = objSPList });
            this.Columns.Add(new UploadColumn() { ColumnName = "Outstanding", ColumnType = "System.Int32", IsManadatory = true, DataColumnName = "StartOutstanding" });
            this.Columns.Add(new UploadColumn() { ColumnName = "Package", ColumnType = "System.String", IsManadatory = true, DataColumnName = "Package", MaxLength = 50, RefList = objPkgList });
        }
        private List<ReferenceColumn> GetServiceProviderList()
        {
            List<ReferenceColumn> objUnitList = new List<ReferenceColumn>();
            DataTable dt = Common.GetDBResult("select ServiceProviderID,Name from dbo.ServiceProviders");
            foreach (DataRow dr in dt.Rows)
                objUnitList.Add(new ReferenceColumn() { Text = Common.ToString(dr["Name"]), Value = Common.ToString(dr["ServiceProviderID"]) });

            return objUnitList;
        }
        private List<ReferenceColumn> GetPackageList()
        {
            List<ReferenceColumn> objSysPkgUnitList = new List<ReferenceColumn>();
            DataTable dt = Common.GetDBResult("select PackageID,PackageName from dbo.Packages where OperatorID=" + OperatorID);
            foreach (DataRow dr in dt.Rows)
                objSysPkgUnitList.Add(new ReferenceColumn() { Text = Common.ToString(dr["PackageName"]), Value = Common.ToString(dr["PackageID"]) });

            return objSysPkgUnitList;
        }
        private List<ReferenceColumn> GetConnectionTypeList()
        {
            List<ReferenceColumn> list = new List<ReferenceColumn>();
            list.Add(new ReferenceColumn() { Text = "Normal", Value = "Normal" });
            list.Add(new ReferenceColumn() { Text = "HD", Value = "HD" });
            return list;
        }
        private List<ReferenceColumn> GetStatusList()
        {
            List<ReferenceColumn> list = new List<ReferenceColumn>();
            list.Add(new ReferenceColumn() { Text = "Active", Value = "1" });
            list.Add(new ReferenceColumn() { Text = "Deactive", Value = "0" });
            return list;
        }
        #endregion
    }
}
