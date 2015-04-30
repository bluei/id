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

namespace LightSwitchApplication
{
    public partial class CompanyDetail
    {

        partial void CompanyQuery_Loaded(bool succeeded)
        {
            if (this.CompanyId.HasValue)
            {
                this.Company = this.CompanyQuery;
                EnableDetailControls();
            }
            else
            {
                this.Company = new Company();
            }

            // Add event handler for property changes
            Microsoft.LightSwitch.Threading.Dispatchers.Main.BeginInvoke(() =>
            {
                ((INotifyPropertyChanged)this.Company).PropertyChanged += CompanyFieldChanged;
            });

            this.SetDisplayNameFromEntity(this.CompanyQuery);
        }

        private void EnableDetailControls()
        {
            this.FindControl("DefaultRepCommission").IsVisible = this.Company.CompanyType == "R";
            this.FindControl("gridJobs").IsVisible = this.Company.CompanyType == "C";
            this.FindControl("gridPurchaseOrders").IsVisible = this.Company.CompanyType == "V";
            this.FindControl("NextJobNumber").IsEnabled = this.Application.User.HasPermission(Permissions.EditNextJobNo);
        }

        private void CompanyFieldChanged(object sender, PropertyChangedEventArgs e)
        {
            // if the field being changed is Product Type, enable/disable the standard cost.  Also set to zero.
            if (e.PropertyName == "CompanyType")
            {
                this.EnableDetailControls();
            }
        }

        partial void AddNewJob_Execute()
        {
            Application.ShowJobAdd(this.Company.Id);

        }

        partial void CompanyDetail_Created()
        {
            // Hide the grid headers
            this.FindControl("gridJobs").ControlAvailable += Application.HideGridHeaderTop;
            this.FindControl("gridPurchaseOrders").ControlAvailable += Application.HideGridHeaderTop;
        }
    }
}