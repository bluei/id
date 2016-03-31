using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.LightSwitch;
namespace LightSwitchApplication
{
    public partial class Invoice
    {
        partial void Job_Validate(EntityValidationResultsBuilder results)
        {
            // results.AddPropertyError("<Error-Message>");
            if (this.Job != null
                && (this.Details.EntityState == EntityState.Added || this.Details.EntityState == EntityState.Modified)
                //&& this.Details.Properties.Job.IsChanged)
                )
            {
                this.SearchTerms = this.Job.JobNumber + " " + this.Job.JobName + " " + this.Job.Company.Name;
            }
        }
    }
}
