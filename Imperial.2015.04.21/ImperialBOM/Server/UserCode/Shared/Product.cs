using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.LightSwitch;

namespace LightSwitchApplication
{
    public partial class Product
    {
        public byte[] _cameraIcon;
        
        partial void Product_Created()
        {
            CreatedBy = Application.User.FullName;
            ModifiedBy = Application.User.FullName;
            CreatedDate = DateTime.Now;
            ModifiedDate = CreatedDate;
            StandardCost = 0;
            IsActive = true;
            Unit = DataWorkspace.ApplicationData.Units.FirstOrDefault();  // the first UOM record in the system (EA) is the default
        }

        partial void ProductType_Validate(EntityValidationResultsBuilder results)
        {

            // Create the PartNumber automatically
            if (this.ProductType != null &&
                this.Details.EntityState == EntityState.Added &&
                this.PartNumber == null)
            {
                this.PartNumber = ProductType.Code + ProductType.NextNumber.ToString("0000000");
                this.ProductType.NextNumber += 1;
            }
        }

        partial void ProductDisplay_Compute(ref string result)
        {
            result = PartNumber + " | " + this.Name;
        }

        partial void DefaultVendor_IsReadOnly(ref bool result)
        {
            // If the part can have a BOM then it is not purchased so 
            // default vendor is disabled

            if (this.ProductType != null)
            {
                result = this.ProductType.CanHaveBOM;
            }
            else
            {
                result = false; // not read only
            }
        }

        partial void ProductType_IsReadOnly(ref bool result)
        {
            result = this.PartNumber != null && this.ProductType != null;  //once there is a part number and type, lock it.
        }

        partial void ProductDetailInfo_Compute(ref string result)
        {
            // Used in the header of the product screens
            result = string.Format("Product Id: {0} | Created {1} by {2} | Modified {3} by {4}",
                this.Id, this.CreatedDate.ToString(), this.ModifiedBy, this.ModifiedDate.ToString(), this.ModifiedBy);
        }

        partial void PartNumber_Validate(EntityValidationResultsBuilder results)
        {
            // Ensure no duplicates when adding or editing part number
            if (this.Details.EntityState == EntityState.Added || 
                (this.Details.EntityState == EntityState.Modified && this.Details.Properties.PartNumber.IsChanged))
            {
                Product productDuplicate = this.DataWorkspace.ApplicationData.Products.Where(p => p.PartNumber == this.PartNumber).FirstOrDefault();
                if (productDuplicate != null)
                {
                    results.AddPropertyError("Duplicate Part Number not allowed.");
                }
            }
        }
    }
}
