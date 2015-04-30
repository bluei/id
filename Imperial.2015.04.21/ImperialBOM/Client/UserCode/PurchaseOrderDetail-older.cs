using System;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using System.Windows.Controls;
using Microsoft.LightSwitch;
using Microsoft.LightSwitch.Framework.Client;
using Microsoft.LightSwitch.Presentation;
using Microsoft.LightSwitch.Presentation.Extensions;
using Microsoft.LightSwitch.Threading;                  // needed for dispatchers in polineschanged
using System.Collections.Specialized;

namespace LightSwitchApplication
{
    public partial class PurchaseOrderDetail
    {
        private PurchaseOrder monitoredPurchaseOrder;

        partial void PurchaseOrder_Loaded(bool succeeded)
        {
            // if PO was deleted from search screen while tab was open, PO will be null.
            if (this.PurchaseOrder == null)
            {
                this.Close(false);
            }
            else
            {
                // make the screen read-only if the PO is closed
                if (this.PurchaseOrder.PurchaseOrderStatus.IsClosed)
                {
                    this.FindControl("ScreenLayout").IsReadOnly = true;
                }
                Microsoft.LightSwitch.Threading.Dispatchers.Main.BeginInvoke(() =>
                {
                    this.Details.Properties.PurchaseOrder.Loader.ExecuteCompleted += this.PurchaseOrderLoadedExecuted;
                });
            }
        }

        private string GetDisplayName()
        {
            if (PurchaseOrder != null)
            {
                if (this.PurchaseOrder.Company.CompanyType == "I")
                {
                    return string.Format("WO# {0}", PurchaseOrder.Number);
                }
                else
                {
                    // if generic po and prefix ends in -0 then change po# to prefix-id
                    if (PurchaseOrder.Number == "x")
                    {
                        //string poPrefix = this.DataWorkspace.ApplicationData.AppSettings_Single(1).GenericPOPrefix;
                        string poPrefix = this.DataWorkspace.ApplicationData.AppSettings.Where(a => a.Id == 1).FirstOrDefault().GenericPOPrefix;

                        this.PurchaseOrder.Number = string.Format("{0}-{1}", poPrefix, this.PurchaseOrder.Id);
                    }
                    return string.Format("PO# {0}", PurchaseOrder.Number);
                }
            }
            else
            {
                return "<new>";
            }
        }

        partial void PurchaseOrderDetail_InitializeDataWorkspace(List<IDataService> saveChangesTo)
        {
            
            this.DisplayName = GetDisplayName();
        }
        private void PurchaseOrderLoadedExecuted(object sender, Microsoft.LightSwitch.ExecuteCompletedEventArgs e)
        {
            if (monitoredPurchaseOrder != this.PurchaseOrder)
            {
                if (monitoredPurchaseOrder != null)
                {
                    (monitoredPurchaseOrder as INotifyPropertyChanged).PropertyChanged -= this.PurchaseOrderChanged;
                }
                monitoredPurchaseOrder = this.PurchaseOrder;
                if (monitoredPurchaseOrder != null)
                {
                    (monitoredPurchaseOrder as INotifyPropertyChanged).PropertyChanged += this.PurchaseOrderChanged;
                }
            }
        }

        private void PurchaseOrderChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "PurchaseOrderStatus")
            {
                // If the po Status changed to a closed status, make sure all PO Lines are marked complete.
                if (this.PurchaseOrder.PurchaseOrderStatus.IsClosed == true)
                {
                    foreach (PurchaseOrderLine poLine in this.PurchaseOrderLines)
                    {
                        if (poLine.IsComplete == false)
                        {
                            poLine.IsComplete = true;
                        }
                    }
                }

                // if the status was changed to an "ordered" status, set the ordered date & lead time
                if (PurchaseOrder.PurchaseOrderStatus.IsOrdered && 
                    PurchaseOrder.DateOrdered == null)
                {
                    PurchaseOrder.DateOrdered = DateTime.Today;
                    PurchaseOrder.DateDue = DateTime.Today.AddDays(PurchaseOrder.Company.DefaultLeadDays.GetValueOrDefault(0));
                }

            }
        }

        partial void PurchaseOrder_Changed()
        {
            if (this.monitoredPurchaseOrder.Details.EntityState == EntityState.Deleted || 
                this.monitoredPurchaseOrder.Details.EntityState == EntityState.Discarded || 
                this.monitoredPurchaseOrder.Details.EntityState == EntityState.Unchanged)
            {
                return;
            }

            this.DisplayName = GetDisplayName();
        }

        partial void PrintPO_Execute()
        {
            if (!this.PurchaseOrder.PurchaseOrderStatus.IsOrdered && !this.PurchaseOrder.PurchaseOrderStatus.IsClosed)
            {
                if (this.ShowMessageBox("Would you like to mark this PO as Ordered?","Mark as Ordered?", MessageBoxOption.YesNo) == System.Windows.MessageBoxResult.Yes)
                {
                    // set the po status to the first IsOrdered status in sequence
                    PurchaseOrderStatus poStatus = DataWorkspace.ApplicationData.PurchaseOrderStatuses
                        .OrderBy(pos => pos.Seq)
                        .Where(pos=>pos.IsOrdered == true)
                        .FirstOrDefault();
                    if (poStatus != null)
                    {
                        PurchaseOrder.PurchaseOrderStatus = poStatus;
                        PurchaseOrder.DateOrdered = DateTime.Today;
                    }
                }
            }
            this.Save();
            Application.ShowPurchaseOrderDetailReport(PurchaseOrder.Id);
        }

        partial void ResetPOStatus_CanExecute(ref bool result)
        {
            // Only show this button if closed and the user has the Override PO Status permission
            result = (this.PurchaseOrder.PurchaseOrderStatus.IsClosed && Application.User.HasPermission(Permissions.OverrideClosedStatus));
        }

        partial void ResetPOStatus_Execute()
        {
            // Reset the PO status to the initial status
            string message = string.Format("{0}\n{1}\n\n{2}\n{3}",
                "Note: If all PO Line items are marked 'complete',",
                "the PO will revert to a completed status when saved.",
                "To re-open the PO, uncheck the appropriate completed items",
                "(or delete the reciepts) and change the status.");
            this.ShowMessageBox(message, "Unlocking PO...", MessageBoxOption.Ok);
            this.FindControl("ScreenLayout").IsReadOnly = false;
        }

        partial void OpenSelectedProduct_Execute()
        {
            Application.ShowProductDetail(this.PurchaseOrderLines.SelectedItem.Product.Id);
        }

        partial void PurchaseOrderLines_Validate(ScreenValidationResultsBuilder results)
        {
            // results.AddPropertyError("<Error-Message>");

            //// prevent from firing unless something changed
            //if (this.PurchaseOrderLines.SelectedItem != null) //&& this.PurchaseOrderLines.SelectedItem.Details.EntityState == EntityState.Modified)
            //{
            //    // if the PRICE changed and is not equal to the std cost, ask to update the cost.
            //    if (this.PurchaseOrderLines.SelectedItem.Details.Properties.Price.IsChanged
            //    && this.PurchaseOrderLines.SelectedItem.Price != this.PurchaseOrderLines.SelectedItem.Product.StandardCost)
            //    {
            //        decimal stdCost = this.PurchaseOrderLines.SelectedItem.Product.StandardCost;
            //        decimal price = this.PurchaseOrderLines.SelectedItem.Price;
            //        System.Windows.MessageBoxResult answer = this.ShowMessageBox(string.Format("Would you like to update the Product standard cost from {0} t0 {1}?",
            //            stdCost, price));
            //        if (answer == System.Windows.MessageBoxResult.Yes)
            //        {
            //            Product selectedProduct = this.PurchaseOrderLines.SelectedItem.Product;
            //            selectedProduct.StandardCost = price;
            //            this.DataWorkspace.ApplicationData.SaveChanges();
                        
            //            //// update the standard cost
            //            //using (var tempDataWorkspace = new DataWorkspace())
            //            //{
            //            //    this.PurchaseOrderLines.SelectedItem.Product.StandardCost = price;
            //            //    try
            //            //    {
            //            //        tempDataWorkspace.ApplicationData.SaveChanges();
            //            //    }
            //            //    catch (Exception ex)
            //            //    {
            //            //        this.ShowMessageBox("COULD NOT UPDATE STANDARD COST..." + ex.ToString());
            //            //    }
            //            //}

            //        }
            //    }
            //}
        }

        partial void AddReceipt_Execute()
        {
            decimal receivedQty;
            string textQty = this.ShowInputBox("Quantity Received","Enter Quantity Received");

            if (Decimal.TryParse(textQty, out receivedQty))
            {
                Receipt receipt = new Receipt();
                receipt.PurchaseOrderLine = this.PurchaseOrderLines.SelectedItem;
                receipt.ReceivedQty = receivedQty;
            }else
            {
                if (!string.IsNullOrEmpty(textQty))
                {
                    this.ShowMessageBox("The quantity entered must be a number.");
                }  
            }                
        }

        partial void PurchaseOrderLines_SelectionChanged()
        {
            var selectedItem = this.PurchaseOrderLines.SelectedItem;
            if (selectedItem == null)
            {
                return;
            }

            Dispatchers.Main.BeginInvoke(() =>
            {
                ((INotifyPropertyChanged)selectedItem).PropertyChanged -= SelectedItemPropertyChanged;
                ((INotifyPropertyChanged)selectedItem).PropertyChanged += SelectedItemPropertyChanged;
            });

        }
        private void SelectedItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Price":
                    if (PurchaseOrderLines.SelectedItem.Price != PurchaseOrderLines.SelectedItem.Product.StandardCost)
                    {
                        decimal stdCost = this.PurchaseOrderLines.SelectedItem.Product.StandardCost;
                        decimal price = this.PurchaseOrderLines.SelectedItem.Price;
                        System.Windows.MessageBoxResult answer = this.ShowMessageBox(string.Format("Would you like to update the Product standard cost from {0} t0 {1}?",
                            stdCost, price));
                        if (answer == System.Windows.MessageBoxResult.Yes)
                        {
                            Product selectedProduct = this.PurchaseOrderLines.SelectedItem.Product;
                            selectedProduct.StandardCost = price;
                            this.DataWorkspace.ApplicationData.SaveChanges();

                            //// update the standard cost
                            //using (var tempDataWorkspace = new DataWorkspace())
                            //{
                            //    this.PurchaseOrderLines.SelectedItem.Product.StandardCost = price;
                            //    try
                            //    {
                            //        tempDataWorkspace.ApplicationData.SaveChanges();
                            //    }
                            //    catch (Exception ex)
                            //    {
                            //        this.ShowMessageBox("COULD NOT UPDATE STANDARD COST..." + ex.ToString());
                            //    }
                            //}

                        }
                    }
                    break;
            }
        }


    }
}