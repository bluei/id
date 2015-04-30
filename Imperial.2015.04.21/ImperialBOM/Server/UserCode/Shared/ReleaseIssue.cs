using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.LightSwitch;
namespace LightSwitchApplication
{
    public partial class ReleaseIssue
    {
        partial void ReleaseIssue_Created()
        {
            IsFixed = false;
            Severity = 1;
            DateCreated = DateTime.Today;
            CreatedBy = Application.User.FullName;
        }
    }
}
