using System;
using System.Linq;
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using Microsoft.LightSwitch;
using Microsoft.LightSwitch.Framework.Client;
using Microsoft.LightSwitch.Presentation;
using Microsoft.LightSwitch.Presentation.Extensions;
using System.Text;

namespace LightSwitchApplication
{
    public partial class EditableProductsGrid
    {


        partial void TestComponentList_Execute()
        {
            string result = "0 - " + this.Products.SelectedItem.ProductDisplay + " contains:\n";
            result += Application.ProductDisplayString(this.Products.SelectedItem, string.Empty, 1);
            this.ShowMessageBox(result);
        }

        partial void BuildJMBI_Execute()
        {
            // Write your code here.

            string result = ""; //"0 - " + this.Products.SelectedItem.ProductDisplay + " contains----------:\n";

            //result += Application.BuildJobItems(this.Products.SelectedItem, string.Empty, 1, 1m, string.Empty);
            
            this.ShowMessageBox(result);
        }
        
    }
}
