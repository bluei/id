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
    public partial class Home
    {
        private bool _loadedOnce;

        partial void Home_Created()
        {
            var assem = new System.Reflection.AssemblyName(System.Reflection.Assembly.GetExecutingAssembly().FullName);
            VersionNum = assem.Version.ToString();
            
            // Hide the grid headers
            this.FindControl("ActiveJobs").ControlAvailable += Application.HideGridHeaderBottom;
            this.FindControl("gridOpenPurchaseOrders").ControlAvailable += Application.HideGridHeaderBottom;
            this.FindControl("gridOpenPurchaseOrders").ControlAvailable += new EventHandler<ControlAvailableEventArgs>((s1, e1) =>
            {
                IContentItem contentItem = (e1.Control as Control).DataContext as IContentItem;
                foreach (var item in contentItem.ChildItems.First().ChildItems)
                {
                    IContentItemProxy CtrlBinder = this.FindControl(item.BindingPath);
                    CtrlBinder.SetBinding(TextBlock.ForegroundProperty, "Details.Entity.PurchaseOrderStatus.FgColor", new ChangeFontColorStatus(), BindingMode.TwoWay);
                }
            });

            // Grid Conditional Formatting
            this.FindControl("gridOpenPurchaseOrders").ControlAvailable += gridOpenPurchaseOrders_ControlAvailable;

            // Set visibility by permissions
            this.FindControl("gridOpenPurchaseOrders").IsVisible = Application.User.HasPermission(Permissions.EditPOs);
            this.FindControl("ActiveJobs").IsVisible = Application.User.HasPermission(Permissions.EditJobs);
            this.FindControl("Products").IsVisible = Application.User.HasPermission(Permissions.EditProducts);
            this.FindControl("CompanySearch").IsVisible = Application.User.HasPermission(Permissions.EditCompanies);
            this.FindControl("Jobs").IsVisible = Application.User.HasPermission(Permissions.EditJobs);
            this.FindControl("PurchaseOrders").IsVisible = Application.User.HasPermission(Permissions.EditPOs);
            this.FindControl("Settings").IsVisible = Application.User.HasPermission(Permissions.EditSettings);
            this.FindControl("LogoGroup").IsVisible = !Application.User.HasPermission(Permissions.HideLogoGroup);
        }

        private System.Windows.Controls.DataGrid dg;
        private void gridOpenPurchaseOrders_ControlAvailable(object sender, ControlAvailableEventArgs e)
        {
            var ctrl = e.Control as DataGrid;
            dg = ctrl;
            if (ctrl != null)
                ctrl.LoadingRow += ctrl_LoadingRow;
        }
        void ctrl_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var grid = sender as DataGrid;
            var row = e.Row as DataGridRow;
            DataGridCell changeCell;

            var bindingDaysOverDue = new Binding("DaysUntilDue")
            {
                Mode = BindingMode.OneWay,
                Converter = new LateColorConverter(),
                ValidatesOnExceptions = true
            };
            
            changeCell = dg.Columns[1].GetCellContent(row).Parent as DataGridCell;
            if (changeCell != null)
                changeCell.SetBinding(DataGridCell.BackgroundProperty, bindingDaysOverDue);
        }

        partial void Home_InitializeDataWorkspace(List<IDataService> saveChangesTo)
        {
            // Fill the enumeration in ImperialCode.cs with the PO Statuses for faster speed.
            POStatusInfo.POStatuses = DataWorkspace.ApplicationData.PurchaseOrderStatuses.OrderBy(p => p.Seq).Execute();                                                                                                                       
            _loadedOnce = true;
        }

        partial void Home_Activated()
        {
            if (_loadedOnce)  // prevents this from running on a refresh
            {
                this.OpenPurchaseOrders.Load();
                this.ActiveJobs.Load();
            }

        }

   #region BUTTON EXECUTE
        partial void AddCompany_Execute()
        {
            this.Application.ShowCompanyDetail(null);
        }
        partial void AddJob_Execute()
        {
            this.Application.ShowJobAdd(null);
        }
        partial void AddProduct_Execute()
        {
            this.Application.ShowProductDetail(null);
        }
        partial void Companies_Execute()
        {
            Application.ShowCompanySearch();
        }
        partial void InvoicesSearch_Execute()
        {
            Application.ShowInvoicesSearch();
        }
        partial void Jobs_Execute()
        {
            Application.ShowJobsSearch();
        }
        partial void Messages_Execute()
        {
            Application.ShowMessagesSearch();
        }
        partial void Products_Execute()
        {
            Application.ShowProductsSearch();
        }
        partial void PurchaseOrders_Execute()
        {
            Application.ShowPurchaseOrdersSearch();
        }
        partial void Receiving_Execute()
        {
            Application.ShowPurchaseOrderReceiving();
        }
        partial void Settings_Execute()
        {
            Application.ShowApplicationSettings(1);
        }
   #endregion

   #region CAN EXEUUTE

        partial void AddCompany_CanExecute(ref bool result)
        {
            result = Application.User.HasPermission(Permissions.EditCompanies);
        }
        partial void AddJob_CanExecute(ref bool result)
        {
            result = Application.User.HasPermission(Permissions.EditJobs);
        }
        partial void AddProduct_CanExecute(ref bool result)
        {
            result = Application.User.HasPermission(Permissions.EditProducts);
        }
        partial void Companies_CanExecute(ref bool result)
        {
            result = Application.User.HasPermission(Permissions.EditCompanies);
        }
        partial void InvoicesSearch_CanExecute(ref bool result)
        {
            result = Application.User.HasPermission(Permissions.EditInvoices);
        }
        partial void Jobs_CanExecute(ref bool result)
        {
            result = Application.User.HasPermission(Permissions.EditJobs);
        }
        partial void Messages_CanExecute(ref bool result)
        {
            result = Application.User.HasPermission(Permissions.EditMessages);
        }
        partial void PurchaseOrders_CanExecute(ref bool result)
        {
            result = Application.User.HasPermission(Permissions.EditPOs);
        }
        partial void POStatus_CanExecute(ref bool result)
        {
            result = Application.User.HasPermission(Permissions.EditSettings);
        }
        partial void Products_CanExecute(ref bool result)
        {
            result = Application.User.HasPermission(Permissions.EditProducts);
        }
        partial void ProductUnits_CanExecute(ref bool result)
        {
            result = Application.User.HasPermission(Permissions.EditSettings);
        }        
        partial void Settings_CanExecute(ref bool result)
        {
            result = Application.User.HasPermission(Permissions.EditSettings);
        }

   #endregion

        


        

        
        
    }
}
