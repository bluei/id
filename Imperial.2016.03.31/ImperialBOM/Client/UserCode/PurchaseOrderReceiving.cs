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
    public partial class PurchaseOrderReceiving
    {
        partial void EnterReceiptQty_Execute()
        {
            decimal receivedQty;
            string textQty = this.ShowInputBox("Quantity Received", "Enter Quantity Received", PurchaseOrderLines.SelectedItem.OrderQty.ToString());

            if (Decimal.TryParse(textQty, out receivedQty))
            {
                Receipt receipt = new Receipt();
                receipt.PurchaseOrderLine = this.PurchaseOrderLines.SelectedItem;
                receipt.ReceivedQty = receivedQty;
            }
            else
            {
                if (!string.IsNullOrEmpty(textQty))
                {
                    this.ShowMessageBox("The quantity entered must be a number.");
                }
            }

        }

        partial void DeleteLastReceipt_Execute()
        {
            // point lastReceipt to the most recent receipt
            Receipt lastReceipt = this.PurchaseOrderLines.SelectedItem.Receipts.OrderByDescending(r => r.Id).FirstOrDefault();

            if (lastReceipt != null)
            {
                if (this.PurchaseOrderLines.SelectedItem.ReceivedQty-lastReceipt.ReceivedQty < PurchaseOrderLines.SelectedItem.OrderQty)
                {
                    if (this.PurchaseOrderLines.SelectedItem.IsComplete == true)
                    {
                        this.PurchaseOrderLines.SelectedItem.IsComplete = false;
                    }
                }
                lastReceipt.Delete();
            }

        }

        partial void CloseReceiptsGroup_Execute()
        {
            this.CloseModalWindow("ReceiptsGroup");
        }

        partial void ShowReceipts_Execute()
        {
            this.OpenModalWindow("ReceiptsGroup");
        }

    }
}
