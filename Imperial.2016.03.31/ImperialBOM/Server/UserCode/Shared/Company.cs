using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.LightSwitch;
namespace LightSwitchApplication
{
    public partial class Company
    {
        partial void Company_Created()
        {
            this.IsActive = true;
            this.NextJobNumber = 1;
            this.CreatedBy = Application.User.FullName;
            this.CreatedDate = DateTime.Now;

        }

        partial void NextJobNumber_IsReadOnly(ref bool result)
        {
            // result = !Application.User.HasPermission(Permissions.CompaniesEditNextJob);

            // the above line was not working when adding a new job for a customer if no perms
            // had to rem out that line.
        }

        partial void AuditInfo_Compute(ref string result)
        {
            // Used in the header of the company screens
            result = string.Format("Company Id: {0} | Created {1} by {2}",
                this.Id, this.CreatedDate.ToString(), this.CreatedBy);
        }
    }
}
