using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HMSOM;
using HMSDL;

namespace HMSBL
{
    public class ReportBL
    {
        #region Declaration
        private string connectionString;
        #endregion

        #region Constructor
        public ReportBL(string conString)
        {
            connectionString = conString;
        }
        #endregion

        private ReportDL CreateDL()
        {
            return new ReportDL(connectionString);
        }

        public int CollectionReport_GetCustomers(int operatorID, int serviceproviderID, int collectedBy, DateTime date1, DateTime date2)
        {
            var BillsDLObj = CreateDL();
            return BillsDLObj.CollectionReport_GetCustomers(operatorID,serviceproviderID,collectedBy, date1, date2);
        }
        public double CollectionReport_GetCollection(int operatorID, int serviceproviderID, int collectedBy, DateTime date1, DateTime date2)
        {
            var BillsDLObj = CreateDL();
            return BillsDLObj.CollectionReport_GetCollection(operatorID, serviceproviderID, collectedBy, date1, date2);
        }
        public int DateReport_GetCustomers(int operatorID, int serviceproviderID, int active)
        {
            var BillsDLObj = CreateDL();
            return BillsDLObj.DateReport_GetCustomers(operatorID, serviceproviderID, active);
        }
        public int OutstandingReport_GetCustomers(int operatorID, int serviceproviderID, DateTime date1, DateTime date2,double min,double max)
        {
            var BillsDLObj = CreateDL();
            return BillsDLObj.OutstandingReport_GetCustomers(operatorID, serviceproviderID, date1, date2, min, max);
        }
        public double OutstandingReport_GetCollection(int operatorID, int serviceproviderID, DateTime date1, DateTime date2, double min, double max)
        {
            var BillsDLObj = CreateDL();
            return BillsDLObj.OutstandingReport_GetCollection(operatorID, serviceproviderID, date1, date2, min, max);
        }
    }
}
