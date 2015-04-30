using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;

namespace LightSwitchApplication
{
    public partial class xrJobMasterBOM : DevExpress.XtraReports.UI.LightSwitchReport
    {
        public xrJobMasterBOM()
        {
            InitializeComponent();
        }

        private void xrLabelPgHdrTitle_PrintOnPage(object sender, PrintOnPageEventArgs e)
        {
            if (e.PageCount > 0)
            {
                // Check if the control is printed on the first page.
                if (e.PageIndex == 0)
                {
                    // Cancels the control's printing.
                    e.Cancel = true;
                }
            }
        }

    }
}
