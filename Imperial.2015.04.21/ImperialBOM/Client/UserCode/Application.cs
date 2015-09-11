using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.LightSwitch;
using Microsoft.LightSwitch.Framework.Client;
using Microsoft.LightSwitch.Presentation;
using Microsoft.LightSwitch.Presentation.Extensions;
using System.Windows; //needed for hide headers
using System.Windows.Controls;

namespace LightSwitchApplication
{
    public partial class Application
    {
        private DataGrid _myGrid;
        //private Timer _timer;
        //partial void Application_LoggedIn()
        //{

        //    if (_timer == null)
        //    {
        //        _timer = new Timer(x =>
        //        {
        //            this.Details.Dispatcher.BeginInvoke(() =>
        //            {
        //                var item = this.CreateDataWorkspace().ApplicationData.Explosions.FirstOrDefault();
        //            });

        //        }, null, 15000, 300000);
        //    }
        //}
        partial void Application_Initialize()
        {
            this.Details.ClientTimeout = 100000;
        }
        public class BOMPreviewItem     // Used to hold list of Exploded BOM for ProductDisplayString 
        {
            public Product BOMPreviewProduct { get; set; }
            public string BOMPreviewLevel { get; set; }
            public decimal BOMQtyPer { get; set; }
            public int Depth { get; set; }
        }   
        public List<BOMPreviewItem> _listOfBOMPreviewItems;
        public StringBuilder DisplayBOMPreviewList(Product masterBOMProduct)
        {
            // Procedure to clear the list variable, generate the list, and return it as a stringbuilder
            // to display in a message box.

            // Clear the variable
            _listOfBOMPreviewItems = new List<BOMPreviewItem>();

            // Generate the BOMPreviewItems list
            GenerateBOMPreviewList(masterBOMProduct, 1, "");

            // Generate a StringBuilder to return the list
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(masterBOMProduct.ProductDisplay);
            sb.AppendLine();

            foreach (BOMPreviewItem item in Application.Current._listOfBOMPreviewItems.OrderBy(l => l.BOMPreviewLevel))
            {
                // Set indent padding and show the product display default

                int i = (item.Depth - 1) * 4;
                String pad = new String(' ', i);
                string symbol = (item.BOMPreviewProduct.ProductType.CanHaveBOM == true ? "└ " : "└ ");
                string display = pad + (i == 0 ? "" : symbol);
                display += string.Format("({0} {1}) ", item.BOMQtyPer, item.BOMPreviewProduct.Unit.UnitCode.ToLower());
                if (item.BOMPreviewProduct.ProductDisplay.Length > 100)
                {
                    // only take the first 100 characters of product display
                    display += item.BOMPreviewProduct.ProductDisplay.Substring(0, 100);
                }
                else
                {
                    display += item.BOMPreviewProduct.ProductDisplay;
                }

                display += (item.BOMPreviewProduct.ProductGroup == null
                        ? string.Empty
                        : string.Format(" (Group {0})", item.BOMPreviewProduct.ProductGroup.Seq.ToString()));

                sb.AppendLine(string.Format("{0}  {1}", item.BOMPreviewLevel, display));

            }

            return sb;


        }
        private void GenerateBOMPreviewList(Product product, int depth, string level = "")
        {
            // This is a recursive function that calls itself.
            // given a product, it calls itself for each subcomponent (and in turn their subcomponents)
            // to build a string "s" and finally return that string

            var sortedBOM = (from BillOfMaterial cb in product.BOMComponents
                             orderby (cb.BOMComponentID.ProductGroup == null ? 0 : cb.BOMComponentID.ProductGroup.Seq),
                                      cb.BOMComponentID.PartNumber
                             select cb);

            int count = 0;                                          // reset the count for this level before the loop
            if (depth > 1) level += ".";                            // add a "." if not zero depth (master item)

            // loop through and add all subs to the list
            foreach (BillOfMaterial component in sortedBOM)         //product.BOMComponents.OrderBy(bom=>bom.BOMComponentID.PartNumber))
            {
                count++;                                            // advance the count which is used to build the level below
                string addon = count.ToString().PadLeft(2, '0');    // create the string
                BOMPreviewItem bpi = new BOMPreviewItem();

                bpi.BOMPreviewLevel = level + addon;
                bpi.BOMPreviewProduct = component.BOMComponentID;
                bpi.BOMQtyPer = component.PerAssemblyQty;
                bpi.Depth = depth;
                _listOfBOMPreviewItems.Add(bpi);

                GenerateBOMPreviewList(component.BOMComponentID, depth + 1, level + addon);
            }
        }

        public static void WriteMessage(Job job, string topic, string note)
        {
            Message message = new Message();
            message.Job = job;
            message.Topic = topic;
            message.Note = note;
        }

        public void HideGridHeaderTop(Object sender, ControlAvailableEventArgs args)
        {
            _myGrid = args.Control as DataGrid;
            if (_myGrid != null)
            {
                Grid grid = (Grid)((FrameworkElement)((FrameworkElement)(((FrameworkElement)_myGrid).Parent)).Parent).Parent;
                grid.Children[0].Visibility = System.Windows.Visibility.Collapsed;
                //grid.Children[1].Visibility = System.Windows.Visibility.Collapsed; 
            }
            _myGrid.HeadersVisibility = DataGridHeadersVisibility.All;
        }
        public void HideGridHeaderBottom(Object sender, ControlAvailableEventArgs args)
        {
            _myGrid = args.Control as DataGrid;
            if (_myGrid != null)
            {
                Grid grid = (Grid)((FrameworkElement)((FrameworkElement)(((FrameworkElement)_myGrid).Parent)).Parent).Parent;
                //grid.Children[0].Visibility = System.Windows.Visibility.Collapsed;
                grid.Children[1].Visibility = System.Windows.Visibility.Collapsed; 
            }
            _myGrid.HeadersVisibility = DataGridHeadersVisibility.All;
        }

        #region CAN RUN PERMISSIONS
        partial void ApplicationSettings_CanRun(ref bool result, int AppSettingId)
        {
            result = User.HasPermission(Permissions.EditSettings);
        }

        partial void CompanySearch_CanRun(ref bool result)
        {
            result = User.HasPermission(Permissions.EditCompanies);
        }

        partial void InvoicesSearch_CanRun(ref bool result)
        {
            result = User.HasPermission(Permissions.EditInvoices);
        }

        partial void InvoiceStatuses_CanRun(ref bool result)
        {
            result = User.HasPermission(Permissions.EditSettings);
        }

        partial void JobsSearch_CanRun(ref bool result)
        {
            result = User.HasPermission(Permissions.EditJobs);
        }

        partial void JobStatusMaintenance_CanRun(ref bool result)
        {
            result = User.HasPermission(Permissions.EditSettings);
        }

        partial void MessagesSearch_CanRun(ref bool result)
        {
            result = User.HasPermission(Permissions.EditSettings);
        }

        partial void POStatus_CanRun(ref bool result)
        {
            result = User.HasPermission(Permissions.EditSettings);
        }

        partial void ProductsSearch_CanRun(ref bool result)
        {
            result = User.HasPermission(Permissions.EditSettings);
        }

        partial void ProductTypes_CanRun(ref bool result)
        {
            result = User.HasPermission(Permissions.EditProducts);
        }

        partial void ProductUnits_CanRun(ref bool result)
        {
            result = User.HasPermission(Permissions.EditSettings);
        }

        partial void PurchaseOrdersSearch_CanRun(ref bool result)
        {
            result = User.HasPermission(Permissions.EditPOs);
        }      

        partial void BOMGrid_CanRun(ref bool result)
        {
            result = User.HasPermission(Permissions.EditSettings);
        }

        partial void ProductGroups_CanRun(ref bool result)
        {
            result = User.HasPermission(Permissions.EditSettings);
        }

        partial void Releases_CanRun(ref bool result)
        {
            result = User.HasPermission(Permissions.EditReleases);
        }

        partial void Terms_CanRun(ref bool result)
        {
            result = User.HasPermission(Permissions.EditSettings);
        }

        #endregion

       
    }

}
