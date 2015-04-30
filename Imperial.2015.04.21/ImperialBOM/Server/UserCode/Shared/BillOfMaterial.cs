using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.LightSwitch;
namespace LightSwitchApplication
{
    public partial class BillOfMaterial
    {
        partial void BillOfMaterial_Created()
        {
            this.CreatedDate = DateTime.Now;
            this.CreatedBy = Application.User.FullName;
            this.PerAssemblyQty = 1m;
        }

        partial void BOMDisplay_Compute(ref string result)
        {
            // use the product display as the bom display
            result = (this.BOMComponentID == null) ? "[NEW]" : this.BOMComponentID.ProductDisplay;
        }

        partial void PerAssemblyCost_Compute(ref decimal? result)
        {
            // set to zero if null (new record)
            // Component Std Cost * PerAssyQty
            result = (this.BOMComponentID == null) ? 0 : this.BOMComponentID.StandardCost * this.PerAssemblyQty;
        }

        partial void ComponentCost_Compute(ref decimal? result)
        {
            // set to zero if null (new record)
            result = (this.BOMComponentID==null) ? 0 : this.BOMComponentID.StandardCost;
        }

        private static void UpdateProductStdCost(Product product)
        {
            decimal total = 0m;

            // Add up the standard costs on screen
            foreach (BillOfMaterial bom in product.BOMComponents)
            {
                total += bom.PerAssemblyCost.GetValueOrDefault(0);
            }
            // Change the parent product std cost.  This , when saved, triggers updates to 
            // the BOMsUsedIn collection for that product.
            product.StandardCost = total;
            
        }

        partial void PerAssemblyQty_Validate(EntityValidationResultsBuilder results)
        {
            if (this.PerAssemblyQty == 0)
            {
                results.AddPropertyError("Per Assembly quantity must be greater than zero.");
            }
            else
            {
                // if something has changed, update the produuct standard cost
                if (this.Details.EntityState != EntityState.Unchanged)
                {
                    UpdateProductStdCost(this.BOMProductAssemblyID);
                }
            }
        }
    }
}

