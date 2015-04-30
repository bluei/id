using System;
using System.Linq;
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using Microsoft.LightSwitch;
using Microsoft.LightSwitch.Framework.Client;
using Microsoft.LightSwitch.Presentation;
using Microsoft.LightSwitch.Presentation.Extensions;

using System.ComponentModel;

namespace LightSwitchApplication
{
    public partial class ProductsSearch
    {
        partial void ProductsSearch_InitializeDataWorkspace(List<IDataService> saveChangesTo)
        {
            this.FilterActive = true;
        }

        private void UpdateFilterLabel()
        {
            switch (FilterActive)
            {
                case true:
                    FilterActiveLabel = "(showing Active)";
                    break;
                case false:
                    FilterActiveLabel = "(showing Inactive)";
                    break;
                default:
                    FilterActiveLabel = "(showing ALL)";
                    break;
            }
        }
        
        partial void AddProduct_Execute()
        {
            // Launch the new product screen with no parameter
            Application.ShowProductDetail(null);
        }

        partial void ClearFilters_Execute()
        {
            this.ProductTypeProperty = null;
            this.CreatedAfter = null;
            this.CreatedBefore = null;
            this.VendorProperty = null;
            this.FilterText = null;
            this.FilterActive = true;
        }
        
        partial void FilterActive_Changed()
        {
            UpdateFilterLabel();        
        }

    }
}
