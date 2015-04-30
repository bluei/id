using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.LightSwitch;
namespace LightSwitchApplication
{
    public partial class Receipt
    {
        partial void Receipt_Created()
        {
            this.ReceivedBy = Application.User.FullName;
            this.ReceivedDate = DateTime.Now;
        }

        partial void ReceivedQty_Validate(EntityValidationResultsBuilder results)
        {
            if (this.ReceivedQty < 0)
            {
                results.AddPropertyError("Received quantity must be a positive number.");
            }

            
            if (this.PurchaseOrderLine != null)
            {
                if (this.PurchaseOrderLine.Receipts.Sum(r => r.ReceivedQty) >= PurchaseOrderLine.OrderQty)
                {
                    PurchaseOrderLine.IsComplete = true;
                }
                else
                {
                    PurchaseOrderLine.IsComplete = false;
                }
            }
        }

    }
}

