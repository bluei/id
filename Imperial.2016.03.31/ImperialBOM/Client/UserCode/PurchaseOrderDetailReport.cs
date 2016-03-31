using System;
using System.Linq;
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using Microsoft.LightSwitch;
using Microsoft.LightSwitch.Framework.Client;
using Microsoft.LightSwitch.Presentation;
using Microsoft.LightSwitch.Presentation.Extensions;

namespace LightSwitchApplication
{
    public partial class PurchaseOrderDetailReport
    {
        // Do not rename the CustomizeReportPreviewModel method because it is used to access the ReportPreviewModel that is associated with this Report Preview Screen.
        // You can remove this method if you do not need to access the ReportPreviewModel.
        public void CustomizeReportPreviewModel(DevExpress.Xpf.Printing.ReportPreviewModel model)
        {
            model.Parameters["Id"].Value = PurchaseOrderIdParameter;
        }

        partial void PurchaseOrderDetailReport_Activated()
        {
            // Assign the name of the report, which you want to preview in this screen.
            this.ReportTypeName = "LightSwitchApplication.xrPurchaseOrder";
        }

        partial void PurchaseOrderDetailReport_InitializeDataWorkspace(List<IDataService> saveChangesTo)
        {
            //string poNumber = this.DataWorkspace.ApplicationData.PurchaseOrders_SingleOrDefault(PurchaseOrderIdParameter).Number;
            string poNumber = this.DataWorkspace.ApplicationData.PurchaseOrders.Where(p => p.Id == PurchaseOrderIdParameter).FirstOrDefault().Number;
            this.DisplayName = string.Format("Preview {0}", poNumber);
        }

    }
}