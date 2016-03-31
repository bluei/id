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
    public partial class JobAdd
    {
        partial void JobAdd_InitializeDataWorkspace(global::System.Collections.Generic.List<global::Microsoft.LightSwitch.IDataService> saveChangesTo)
        {
            // Create a new Job and populate company if parameter was provided
            this.JobProperty = new Job();
            if (CompanyId.HasValue)
            {
                //this.JobProperty.Company = this.DataWorkspace.ApplicationData.Companies_SingleOrDefault(CompanyId);
                this.JobProperty.Company = this.DataWorkspace.ApplicationData.Companies.Where(c => c.Id == CompanyId).FirstOrDefault();
            }

            // If a CompanyId parameter was provided to the screen, build the Job#
            if (this.JobProperty.Company != null)
            {
                // If no jobs have been issued yet for this company, set next# to 1
                // Next job number must default to 1 for all companies but not if imported previously
                if (this.JobProperty.Company.NextJobNumber.GetValueOrDefault(0) == 0)
                {
                    this.JobProperty.Company.NextJobNumber = 1;
                }

                this.JobProperty.JobNumber = string.Format("{0}-{1}",
                                                this.JobProperty.Company.Id.ToString(),
                                                this.JobProperty.Company.NextJobNumber.ToString());
                //this.JobProperty.Company.NextJobNumber++;
                //DELETE this.SalesRepCommission = Company.DefaultRepCommission.GetValueOrDefault();
            }

        }

        partial void JobAdd_Saved()
        {
            this.Close(false);
            // close and show the default screen using the current property (data just entered) as the parameter
            Application.Current.ShowDefaultScreen(this.JobProperty);
        }
    }
}