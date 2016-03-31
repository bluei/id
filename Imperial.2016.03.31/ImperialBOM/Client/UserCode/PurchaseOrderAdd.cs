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
    public partial class PurchaseOrderAdd
    {
        // In this screen we add a new PO
        // the user selects a Vendor and can begin the line items? - actuallly save that for the details screen.

        partial void PurchaseOrderAdd_Created()
        {
            // Build custom event for whenever a screen property has changed
            Microsoft.LightSwitch.Threading.Dispatchers.Main.BeginInvoke(() =>
            {
                ((INotifyPropertyChanged)this.PurchaseOrderProperty).PropertyChanged += AddPOFieldChanged;
            });
        }

        private void AddPOFieldChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Job")
            {
                // A job was selected so calculate the job#
                this.PurchaseOrderProperty.Number = string.Format("{0}-{1}", 
                    this.PurchaseOrderProperty.Job.JobNumber, 
                    this.PurchaseOrderProperty.Job.NextPoNumber);
            }
        }

        partial void PurchaseOrderAdd_InitializeDataWorkspace(global::System.Collections.Generic.List<global::Microsoft.LightSwitch.IDataService> saveChangesTo)
        {
            // Create a new PO and populate Job if known.
            this.PurchaseOrderProperty = new PurchaseOrder();
            if (JobId.HasValue)
            {
                //this.PurchaseOrderProperty.Job = this.DataWorkspace.ApplicationData.Jobs_SingleOrDefault(JobId);
                this.PurchaseOrderProperty.Job = this.DataWorkspace.ApplicationData.Jobs.Where(j => j.Id == JobId).FirstOrDefault();
            }

            // If a JobId parameter was provided to the screen, build the Job#
            if (this.PurchaseOrderProperty.Job != null)
            {
                // If no POs have been issued yet for this job, set nextpo# to 1
                // Next PO# defaults to 1 for all new jobs but not if imported previously
                this.PurchaseOrderProperty.Number = string.Format("{0}-{1}", 
                    this.PurchaseOrderProperty.Job.JobNumber, 
                    this.PurchaseOrderProperty.Job.NextPoNumber);
                // note: the next job numer is incremented when the Add screen is closed ant the PO saved, not here
            }
            else
            {
                // create generic PO#
                //string poPrefix = this.DataWorkspace.ApplicationData.AppSettings_Single(1).GenericPOPrefix;
                //string userInitials = Application.User.Name;
                //this.PurchaseOrderProperty.Number = string.Format("{0}-{1:MMddyy.hhmm}", poPrefix, DateTime.Now);

                // need to create a po and save it.  then open it, and give it a new number in the po details screen
                this.PurchaseOrderProperty.Number = "x";
            }
        }

        partial void PurchaseOrderAdd_Saved()
        {           
            this.Close(false);
            Application.Current.ShowDefaultScreen(this.PurchaseOrderProperty);
        }

        partial void PurchaseOrderAdd_Saving(ref bool handled)
        {
            // if it is created for a job, increment the next number when saving.
            // This is not incremented on the server side due to performance when
            // adding multiple POs during the automated create requisition method.
            if (this.PurchaseOrderProperty.Job != null)
            {
                this.PurchaseOrderProperty.Job.NextPoNumber++;
            }
        }
    }
}