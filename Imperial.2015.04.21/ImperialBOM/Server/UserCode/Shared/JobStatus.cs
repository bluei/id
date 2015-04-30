using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.LightSwitch;
namespace LightSwitchApplication
{
    public partial class JobStatus
    {
        partial void JobStatus_Created()
        {
            this.ShowOnHome = false;
        }
    }
}
