using System;
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
    public partial class CompanySearch
    {
        partial void AddCompany_Execute()
        {
            this.Application.ShowCompanyDetail(null);
        }

        partial void AddCompany_CanExecute(ref bool result)
        {
            result = this.Application.User.HasPermission(Permissions.EditCompanies);
        }

        partial void ClearFilters_Execute()
        {
            this.CompanyTypeFilter = null;
            this.FilterText = null;
            this.ActiveFilter = true;
        }

        partial void CompanySearch_InitializeDataWorkspace(List<IDataService> saveChangesTo)
        {
            this.ActiveFilter = true;
        }

        partial void CompanySearch_Created()
        {
            // Hide the grid headers
            this.FindControl("grid").ControlAvailable += Application.HideGridHeaderTop;
        }

    }
}
