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
    public partial class JobDetailReport
    {
        // Do not rename the CustomizeReportPreviewModel method because it is used to access the ReportPreviewModel that is associated with this Report Preview Screen.
        // You can remove this method if you do not need to access the ReportPreviewModel.
        public void CustomizeReportPreviewModel(DevExpress.Xpf.Printing.ReportPreviewModel model)
        {
            model.Parameters["Id"].Value = JobIdParameter;
        }

        partial void JobDetailReport_Activated()
        {
            // Assign the name of the report, which you want to preview in this screen.
            this.ReportTypeName = "LightSwitchApplication.xrJobMasterBOM";
        }

    }
}