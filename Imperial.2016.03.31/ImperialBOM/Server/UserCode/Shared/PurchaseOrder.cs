using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.LightSwitch;
namespace LightSwitchApplication
{
    public partial class PurchaseOrder
    {
        partial void PurchaseOrder_Created()
        {
            POCreatedDate = DateTime.Now;
            DateOrdered = DateTime.Now;
            CreatedBy = this.Application.User.FullName;
            this.PurchaseOrderStatus = this.DataWorkspace.ApplicationData.PurchaseOrderStatuses.OrderBy(ps => ps.Seq).FirstOrDefault();
        }

        partial void DaysUntilDue_Compute(ref int result)
        {
            // Calculate the days until due.  Goes negative if overdue
            TimeSpan span = DateDue.GetValueOrDefault().Subtract(DateTime.Today);
            result = span.Days;
        }

        partial void PurchaseOrderStatus_Changed()
        {
            // prevent this event from inadvertently firing
            if (this.Details.EntityState == EntityState.Deleted ||
                this.Details.EntityState == EntityState.Discarded ||
                this.Details.EntityState == EntityState.Unchanged)
                return;

            if (this.DateOrdered == null && this.PurchaseOrderStatus.IsOrdered)
            {
                this.DateOrdered = DateTime.Today;
                this.OrderedBy = Application.User.FullName;
            }

            if (this.DateClosed == null && this.PurchaseOrderStatus.IsClosed)
            {
                this.DateClosed = DateTime.Today;
            }

        }

        partial void Company_Changed()
        {
            // prevent this event from inadvertently firing
            if (this.Details.EntityState == EntityState.Deleted ||
                this.Details.EntityState == EntityState.Discarded ||
                this.Details.EntityState == EntityState.Unchanged)
                return;

            // find the primary contact and fill it in once a company is selected or changed
            if (this.Company != null)
            {
                this.Contact = Company.Contacts.OrderBy(c => c.IsPrimary).ThenBy(c => c.FullName).FirstOrDefault();
            }
        }
    }
}