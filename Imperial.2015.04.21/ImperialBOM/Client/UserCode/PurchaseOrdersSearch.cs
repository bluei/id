using System;
using System.Linq;
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using Microsoft.LightSwitch;
using Microsoft.LightSwitch.Framework.Client;
using Microsoft.LightSwitch.Presentation;
using Microsoft.LightSwitch.Presentation.Extensions;

using System.Windows.Controls;
using System.Windows.Data;

namespace LightSwitchApplication
{
    public partial class PurchaseOrdersSearch
    {
        partial void AddNewPO_Execute()
        {
            Application.ShowPurchaseOrderAdd(null);
        }

        partial void PurchaseOrdersSearch_Created()
        {
            this.FindControl("gridPurchaseOrders").ControlAvailable += Application.HideGridHeaderTop;
            this.FindControl("gridPurchaseOrders").ControlAvailable += new EventHandler<ControlAvailableEventArgs>((s1, e1) =>
            {
                IContentItem contentItem = (e1.Control as Control).DataContext as IContentItem;
                foreach (var item in contentItem.ChildItems.First().ChildItems)
                {
                    IContentItemProxy CtrlBinder = this.FindControl(item.BindingPath);
                    CtrlBinder.SetBinding(TextBlock.ForegroundProperty, "Details.Entity.PurchaseOrderStatus.FgColor", new ChangeFontColorStatus(), BindingMode.TwoWay);
                }
            });
        }

        partial void ClearFilters_Execute()
        {
            this.filterOrderedFromDate = null;
            this.filterOrderedThruDate = null;
            this.SelectedStatus = null;
            this.SelectedCompany = null;
        }

        partial void CancelChanges_Execute()
        {
            // Write your code here.
            foreach (PurchaseOrder po in this.DataWorkspace.ApplicationData.Details.GetChanges().OfType<PurchaseOrder>())
            {
                po.Details.DiscardChanges();
            }
            this.Close(false);
        }
    }
}
