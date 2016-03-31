using System;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using Microsoft.LightSwitch;
using Microsoft.LightSwitch.Framework.Client;
using Microsoft.LightSwitch.Presentation;
using Microsoft.LightSwitch.Presentation.Extensions;
using System.Collections.Specialized;
using System.Windows; //needed for hide headers
using System.Windows.Controls;

namespace LightSwitchApplication
{
    /// <summary>
    /// ////  NOTE THAT THIS SCREEN IS BASED ON A PROPERTY AND NOT A DETAIL SCREEN  ////
    /// </summary>

    public partial class ProductDetail
    {
        partial void ProductDetail_Created()
        {
            Microsoft.LightSwitch.Threading.Dispatchers.Main.BeginInvoke(() =>
            {
                ((INotifyPropertyChanged)this.Product).PropertyChanged += ProductFieldChanged;
            });
            // Hide the grid headers
            this.FindControl("BOMComponents").ControlAvailable += Application.HideGridHeaderTop;
            this.FindControl("PurchaseOrderLines").ControlAvailable += Application.HideGridHeaderTop;
            //this.FindControl("BOMsUsedIn").ControlAvailable += Application.HideGridHeaderTop; 
            if (this.Product.Details.EntityState == EntityState.Added)
                this.FindControl("ProductType").Focus();
        }

        partial void ProductDetail_InitializeDataWorkspace(List<IDataService> saveChangesTo)
        {
            // Determine if creating a new entity or editing an existing, based on 
            //  whether ProductId is passed as a parameter
            if (!this.ProductId.HasValue)
            {
                this.Product = new Product();
            }
            else
            {
                this.Product = this.ProductQuery;
                EnableDetailControls();

                if (!Product.ProductType.CanHaveBOM)
                {
                    //int daysToAverage = this.DataWorkspace.ApplicationData.AppSettings_Single(1).DaysToAverageLastPurchasedCost;
                    int daysToAverage = this.DataWorkspace.ApplicationData.AppSettings.Where(a => a.Id == 1).FirstOrDefault().DaysToAverageLastPurchasedCost;
                    var poLinesInRange = this.PurchaseOrderLines
                        .Where(pol => pol.PurchaseOrder.POCreatedDate > DateTime.Today.AddDays(-1 * daysToAverage));
                    DaysToAverage = daysToAverage;
                    CountOfPoLines = poLinesInRange.Count();
                    TotalPurchasedCost = poLinesInRange.Sum(p => p.ExtPrice);
                    TotalPurchasedQuantity = poLinesInRange.Sum(p => p.OrderQty);
                    if (TotalPurchasedQuantity.GetValueOrDefault() > 0)
                        AveragePurchasedCost = TotalPurchasedCost / TotalPurchasedQuantity;
                    else
                        AveragePurchasedCost = 0m;
                }
            }

            this.DisplayName = this.Product.PartNumber;

        }

        partial void ProductQuery_Changed()
        {
            // prevent this event from inadvertently firing
            if (this.Product.Details.EntityState == EntityState.Deleted ||
                this.Product.Details.EntityState == EntityState.Discarded ||
                this.Product.Details.EntityState == EntityState.Unchanged)
                return;

            this.DisplayName = this.Product.PartNumber;
        }

        partial void ProductDetail_Saved()
        {
            this.DisplayName = this.Product.PartNumber;
        }

        private void EnableDetailControls()
        {
            if (this.Product != null)
            {

                this.FindControl("PartNumber").IsEnabled = Application.User.HasPermission(Permissions.OverridePartNumber);
                if (this.Product.ProductType.CanHaveBOM)
                {
                    this.FindControl("StandardCost").IsReadOnly = true;
                    this.FindControl("POLinesTab").IsVisible = false;
                    this.FindControl("ComponentsTab").IsVisible = true;
                    try
                    {
                         this.FindControl("ComponentsTab").Focus();
                        // THIS LINE ERRORS OUT WHEN THE Detail screen is called a second time - not sure why
                    }
                    catch (Exception ex)
                    {
                        // var err = ex.ToString();
                    }
                }
                else
                {
                    this.FindControl("StandardCost").IsReadOnly = false;
                    this.FindControl("ComponentsTab").IsVisible = false;
                    this.FindControl("POLinesTab").IsVisible = true;
                }

                // make read only checkboxes in PO Lines
                foreach (PurchaseOrderLine pol in PurchaseOrderLines)
                {
                    this.FindControlInCollection("IsComplete", pol).IsReadOnly = true;
                }
            }
            
        }

        private void ProductFieldChanged(object sender, PropertyChangedEventArgs e)
        {
            // if the field being changed is Product Type, enable/disable the standard cost.  Also set to zero.
            if (e.PropertyName == "ProductType")
            {
                EnableDetailControls();
            }

            if (e.PropertyName == "PartNumber")
            {
                this.DisplayName = Product.PartNumber;
                this.FindControl("Name").Focus();
            }
        }

        partial void PreviewBOM_Execute()
        {
            string result = Application.DisplayBOMPreviewList(this.Product).ToString();
            this.ShowMessageBox(result, "BOM Preview  (use mouse wheel to scroll)", MessageBoxOption.Ok);
        }

        partial void AddProduct_Execute()
        {
            this.Save();
            Application.ShowProductDetail(null);
            // if you turn off multiple instances, use the code below...

            //this.Product = new Product();
            //this.DisplayName = "<New>";
        }

        partial void BOMComponents_Changed(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                decimal total = 0m;

                // Add up the standard costs on screen
                foreach (BillOfMaterial bom in this.BOMComponents)
                {
                    total += bom.PerAssemblyCost.GetValueOrDefault(0);
                }
                // Change the parent product std cost.  This in turn triggers updates to 
                // the BOMsUsedIn collection for that product.
                this.Product.StandardCost = total;
            }
        }

        partial void OpenComponentPart_Execute()
        {
            Application.ShowProductDetail(this.BOMComponents.SelectedItem.BOMComponentID.Id);
        }

        partial void QuickAddPN_Changed()
        {
            // prevent this event from inadvertently firing
            if (this.Product != null &&
                this.Product.Details.EntityState == EntityState.Deleted ||
                this.Product.Details.EntityState == EntityState.Discarded)
                return;
            
            if (!string.IsNullOrEmpty(QuickAddPN))
            {
                Product product = this.DataWorkspace.ApplicationData.Products
                                      .Where(p => p.PartNumber == QuickAddPN.ToUpper())
                                      .FirstOrDefault();
                if (product != null)
                {
                    // make sure it is not the Parent
                    if (product == this.Product)
                    {
                        this.ShowMessageBox("Parent part cannot be added as a component of itself.");
                        QuickAddPN = null;
                        return;
                    }
                    
                    // add the product to the BOM  
                    var bom = this.DataWorkspace.ApplicationData.BillOfMaterials.AddNew();
                    bom.BOMProductAssemblyID = this.Product;
                    bom.BOMComponentID = product;
                    bom.PerAssemblyQty = 1;

                    // scroll to end
                    this.BOMComponents.SelectedItem = BOMComponents.LastOrDefault();

                    QuickAddPN = null;  // clear the quick-add text box
                }
                else
                {
                    this.ShowMessageBox(string.Format("Part number {0} was not found", QuickAddPN));
                }
            }
        }

        partial void PreviewBOM_CanExecute(ref bool result)
        {
            // only enable button if item can have a BOM
            if (this.Product.ProductType == null) //Details.EntityState == EntityState.Added)
            {
                result = false;
            }
            else
            {
                result = this.Product.ProductType.CanHaveBOM;
            }
        }
  
    }
}