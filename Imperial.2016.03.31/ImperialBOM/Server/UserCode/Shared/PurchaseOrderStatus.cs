using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.LightSwitch;
namespace LightSwitchApplication
{
    public partial class PurchaseOrderStatus
    {
        partial void PurchaseOrderStatus_Created()
        {
            IsClosed = false;
            IsLocked = false;
            IsOrdered = false;
            IsRevised = false;
        }
    }
}
