using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.LightSwitch;
using Microsoft.LightSwitch.Threading;


namespace LightSwitchApplication
{
    public partial class PurchaseOrderLine
    {

        partial void JobMasterBOMItemId_Changed()
        {
            // JobMasterBOMItemId is the associated JMBI for this PO Line.
            // If it is changed from nothing to something, or something else, 
            // (1) update the PoLine's Product and quantity.
            // (2) update the new JMBI's associated POLineId to match

            // prevent this event from inadvertently firing
            if (this.Details.EntityState == EntityState.Deleted ||
                this.Details.EntityState == EntityState.Discarded ||
                this.Details.EntityState == EntityState.Unchanged)
                return;

            // reference the jmbi that exists for this PO Line's JMB Item Id
            //JobMasterBOMItem jmbi = this.DataWorkspace.ApplicationData.JobMasterBOMItems_SingleOrDefault(this.JobMasterBOMItemId);
            JobMasterBOMItem jmbi = this.DataWorkspace.ApplicationData.JobMasterBOMItems.Where(i => i.Id == this.JobMasterBOMItemId).FirstOrDefault();

            if (jmbi != null)
            {
                // the jmbi is found, POLine's Product is set to the same product as the jmbi product found 
                this.Product = jmbi.Product;
                this.OrderQty = jmbi.ExtendedQty ?? 0;
                //this.Price = jmbi.Product.StandardCost;  //note: moved to product_changed event

                //may need to move this to the after updated event
                jmbi.POLineId = this.Id;
                jmbi.Vendor = this.PurchaseOrder.Company;
                jmbi.PurchaseOrderStatus = this.PurchaseOrder.PurchaseOrderStatus;
            }
            else
            {
                this.Product = null;
                this.OrderQty = 0;
            }
        }

        partial void ReceivedQty_Compute(ref decimal result)
        {
            // Total of the receipts
            result = this.Receipts.Sum(r => r.ReceivedQty);
        }

        partial void PurchaseOrderLine_Created()
        {
            this.IsComplete = false;
        }

        partial void ExtPrice_Compute(ref decimal? result)
        {
            result = OrderQty * Price;
        }

        partial void PartsPath_Compute(ref string result)
        {
            if (this.JobMasterBOMItemId.HasValue)
            {
                //JobMasterBOMItem jmbi = this.DataWorkspace.ApplicationData.JobMasterBOMItems_SingleOrDefault(this.JobMasterBOMItemId);
                JobMasterBOMItem jmbi = this.DataWorkspace.ApplicationData.JobMasterBOMItems.Where(i => i.Id == JobMasterBOMItemId).FirstOrDefault();
                if (jmbi != null) result = jmbi.PartsPath;
            }
        }

        partial void VendorName_Compute(ref string result)
        {
            if (this.PurchaseOrder != null)
            {
                result = PurchaseOrder.Company.Name;
            }
        }

        partial void Product_Changed()
        {
            // prevent this event from inadvertently firing
            if (this.Details.EntityState == EntityState.Deleted ||
                this.Details.EntityState == EntityState.Discarded ||
                this.Details.EntityState == EntityState.Unchanged)
                return;

            if (this.Product != null)
            {
                this.Price = Product.StandardCost;
            }

        }
    }
}