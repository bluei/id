using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.Text;

namespace LightSwitchApplication
{
    public partial class xrPurchaseOrder : DevExpress.XtraReports.UI.LightSwitchReport
    {
        public xrPurchaseOrder()
        {
            InitializeComponent();
        }

        private void xrLabelPhone_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            XRLabel l = sender as XRLabel;

            long j;
            bool result = Int64.TryParse(l.Text, out j);
            if (true == result)
                l.Text = String.Format("{0:(###) ###-####}", j);
        }
    }
}
