using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;

namespace LightSwitchApplication
{
    public partial class xrJobCard : DevExpress.XtraReports.UI.LightSwitchReport
    {
        public xrJobCard()
        {
            InitializeComponent();
        }

        private void xrPhoneField_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            XRLabel l = sender as XRLabel;

            long j;
            bool result = Int64.TryParse(l.Text, out j);
            if (true == result)
                l.Text = String.Format("{0:(###) ###-####}", j);
        }

    }
}
