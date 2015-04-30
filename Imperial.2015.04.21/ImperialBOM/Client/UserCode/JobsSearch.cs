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
    public partial class JobsSearch
    {

        partial void AddNewJob_Execute()
        {
            Application.ShowJobAdd(null);
        }

        partial void ClearFilters_Execute()
        {
            this.CompanyLookup = null;
            this.JobStatusLookup = null;
            this.filter_text = null;
        }

        partial void JobsSearch_Created()
        {
            // Hide the grid headers
            this.FindControl("grid").ControlAvailable += Application.HideGridHeaderTop;
        }
    }
}
