using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.LightSwitch;

namespace LightSwitchApplication
{
    public partial class JobMasterBOMItem
    {
        partial void JobMasterBOMItem_Created()
        {
            this.PurchaseOrderStatus = this.DataWorkspace.ApplicationData.PurchaseOrderStatuses.OrderBy(p=>p.Seq).FirstOrDefault(); 
            this.CreatedDate = DateTime.Now;
            this.Processed = false;
            this.IsDeletedInBOM = false;
        }

        partial void JMBIDisplay_Compute(ref string result)
        {
            // Set indent padding and show the product display default
            int i = (this.Depth - 1).GetValueOrDefault(0) * 4;
            String pad = new String(' ', i);
            string symbol = (this.Product.ProductType.CanHaveBOM == true ? "└ " : "└ ");
            result = pad + (i == 0 ? "" : symbol) + this.Product.ProductDisplay;
        }

        partial void ExtActCost_Compute(ref decimal? result)
        {
            if (this.Product.ProductType.CanHaveBOM)
            {
                result = null;
            }
            else
            {
                result = ExtendedQty.GetValueOrDefault(0) * UnitCost.GetValueOrDefault(0);
            }
        }

        partial void PurchaseOrderStatus_Changed()
        {
            // prevent this event from inadvertently firing
            if (this.Details.EntityState == EntityState.Unchanged)
                return;
            
            // update the parent PO status to the lowest of the child PO statuses
            if (!string.IsNullOrEmpty(IdPath))
            {
                int lastCommaPos;
                lastCommaPos = IdPath.LastIndexOf(",");

                if (lastCommaPos != -1) // verify that there is a comma  
                {
                    string parentIdPath = string.Empty;
                    string childPathSearch = string.Empty;

                    parentIdPath = this.IdPath.Substring(0, lastCommaPos);  // strips comma
                    childPathSearch = parentIdPath + ",";   // includes the ending comma

                    JobMasterBOMItem parentJMBI = this.JobMasterBOM.JobMasterBOMItemQuery
                        .Where(j => j.IdPath.Equals(parentIdPath))
                        .FirstOrDefault();

                    if (parentJMBI != null)
                    {
                        PurchaseOrderStatus lowestPOStatus = this.JobMasterBOM.JobMasterBOMItemQuery
                            .Where(c => c.IdPath.StartsWith(childPathSearch))
                            .OrderBy(c => c.PurchaseOrderStatus.Seq).FirstOrDefault().PurchaseOrderStatus;
                        if (lowestPOStatus != null && parentJMBI.PurchaseOrderStatus != lowestPOStatus)
                        {
                            parentJMBI.PurchaseOrderStatus = lowestPOStatus;
                        }
                    }
                }
            }
        }

        partial void OnPONumber_Compute(ref string result)
        {
            if (POLineId.HasValue)
            {
                // TODO: increase performance by not having a query in a computed property.  Persist to a field instead.
                PurchaseOrderLine pol = this.DataWorkspace.ApplicationData.PurchaseOrderLines.Where(l => l.Id == POLineId).FirstOrDefault();
                if (pol != null && pol.PurchaseOrder != null)
	            {
                    result = pol.PurchaseOrder.Number;
	            }
            }
        }

        partial void NeedsVendor_Compute(ref bool result)
        {
            if (this.Product != null && !Product.ProductType.CanHaveBOM && Vendor == null)
            {
                result = true;
            }
            else
            {
                result = false;
            }
        }

        partial void JMBIProductGroup_Compute(ref string result)
        {
            // Set result to the desired field value
            if (this.Product!=null)
            {
                if (this.Product.ProductGroup!=null)
                {
                    result = this.Product.ProductGroup.ProductGroupDisplay;
                }
            }
        }

        partial void PurchaseOrderStatus_Validate(EntityValidationResultsBuilder results)
        {
            // prevent this event from inadvertently firing
            if (this.Details.EntityState != EntityState.Modified) return;
            if (this.Details.EntityState == EntityState.Added) return;
            if (this.Details.EntityState == EntityState.Deleted) return;
            if (!this.Details.Properties.PurchaseOrderStatus.IsChanged) return;

            PurchaseOrderStatus oldPOS = Details.Properties.PurchaseOrderStatus.OriginalValue;

            bool newStatusIsComplete = this.PurchaseOrderStatus.IsClosed;
            bool newStatusIsCreated = this.PurchaseOrderStatus == this.DataWorkspace.ApplicationData.PurchaseOrderStatuses.OrderBy(s => s.Seq).FirstOrDefault();
            bool oldStatusIsComplete = oldPOS.IsClosed;
            bool oldStatusIsRequisition = !oldPOS.IsClosed && !oldPOS.IsLocked && !oldPOS.IsOrdered;
            bool oldStatusIsCreated = oldPOS == this.DataWorkspace.ApplicationData.PurchaseOrderStatuses.OrderBy(s => s.Seq).FirstOrDefault();

            // if PO status is changed to anything but a created or completed status without a PO, flag this as an issue.
            if (this.POLineId == null && !this.Product.ProductType.CanHaveBOM && !this.IsDeletedInBOM)
            {
                // can change from created to complete OR complete/requisition to created.  if not one of those then error
                if (!((oldStatusIsCreated && newStatusIsComplete) || (newStatusIsCreated && (oldStatusIsComplete || oldStatusIsRequisition))))
                {
                    results.AddPropertyError("Status can only be changed between Created and Complete if there is no PO");
                }                
            }
            
        }
    }
}
