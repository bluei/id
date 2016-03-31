using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.LightSwitch;
namespace LightSwitchApplication
{
    public partial class Contact
    {
        partial void FullName_Compute(ref string result)
        {
            // Set result to the desired field value
            result = (FirstName ?? "") + " " + (LastName ?? "");
        }

        partial void Contact_Created()
        {
            IsPrimary = 1;
        }
    }
}
