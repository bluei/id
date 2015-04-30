using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.LightSwitch;
namespace LightSwitchApplication
{
    public partial class Job
    {
        partial void JobDisplay_Compute(ref string result)
        {
            // Set result to the desired field value
            if (this.JobNumber != null && this.JobName != null)
            {
                result = this.JobNumber + " " + this.JobName;
            }
            else
            {
                result = "<new job>";
            }

        }

        partial void Company_Validate(EntityValidationResultsBuilder results)
        {
            // is it validating the job number first?
            if (this.Company == null)
            {
                results.AddPropertyError("You must specify the Company");
            }
            else
            {
                if (this.Company.NextJobNumber.GetValueOrDefault(0) == 0)
                {
                    this.Company.NextJobNumber = 1;
                }

                if (this.Details.EntityState == EntityState.Added)
                {
                    this.JobNumber = string.Format("{0}-{1}",
                                                    this.Company.Id.ToString(),
                                                    this.Company.NextJobNumber.ToString());
                    //this.Company.NextJobNumber++;
                    //DELETE this.SalesRepCommission = Company.DefaultRepCommission.GetValueOrDefault();
                }
            }
        }

        partial void Job_Created()
        {
            // NOTE: the first status must be "Active"
            //this.JobStatus = this.DataWorkspace.ApplicationData.JobStatuses_SingleOrDefault(1);
            this.JobStatus = this.DataWorkspace.ApplicationData.JobStatuses.Where(s => s.Id == 1).FirstOrDefault();
            this.NextPoNumber = 1;
            this.CreatedDate = DateTime.Now;
            this.CreatedBy = Application.User.FullName;
            this.ModifiedDate = CreatedDate;
            this.ModifiedBy = Application.User.FullName;
            this.DateSold = DateTime.Today;
        }

        partial void TotalActCost_Compute(ref decimal result)
        {
            decimal sumOfMisc = (from pol in this.DataWorkspace.ApplicationData.PurchaseOrderLines
                                 where pol.PurchaseOrder.Job.Id == this.Id
                                 where pol.JobMasterBOMItemId == null
                                 select pol).Execute().Sum(p => p.Price);
                                 
            result = this.JobMasterBOM.Sum(j => j.ActualCost.GetValueOrDefault()) + sumOfMisc;     
        }

        partial void TotalStdCost_Compute(ref decimal result)
        {
            // Sum of Job Master BOM Standard Costs
            result = this.JobMasterBOM.Sum(j => j.StandardCost);
        }

        partial void GrossProfit_Compute(ref decimal result)
        {
            // Set result to the desired field value
            result = SellPrice.GetValueOrDefault() - TotalActCost;
        }

        partial void SellPrice_Compute(ref decimal? result)
        {
            // Total of JMBs sell prices
            result = this.JobMasterBOM.Sum(j => j.Price);
        }

        partial void JobAuditInfo_Compute(ref string result)
        {
            // Used in the header of the product screens
            result = string.Format("Job Id: {0} | Created {1} by {2} | Modified {3} by {4}",
                this.Id, this.CreatedDate.ToString(), this.ModifiedBy, this.ModifiedDate.ToString(), this.ModifiedBy);

        }

        partial void JobGrossMargin_Compute(ref decimal result)
        {           
            if (SellPrice > 0)
            {
                result = Math.Round((GrossProfit / SellPrice.GetValueOrDefault()),4);
            }
            else
            {
                result = 0M;
            }
        }

        partial void SalesRepContact_Changed()
        {
            // prevent this event from inadvertently firing
            if (this.Details.EntityState == EntityState.Deleted ||
                this.Details.EntityState == EntityState.Discarded ||
                this.Details.EntityState == EntityState.Unchanged)
                return;

            if (this.SalesRepContact != null && (!this.SalesRepCommission.HasValue || this.SalesRepCommission == 0))
            {
                this.SalesRepCommission = this.SalesRepContact.Company.DefaultRepCommission.GetValueOrDefault();
            }
        }
    }
}
