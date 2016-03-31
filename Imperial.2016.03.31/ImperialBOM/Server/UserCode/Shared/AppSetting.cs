using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.LightSwitch;
namespace LightSwitchApplication
{
    public partial class AppSetting
    {
        partial void AppSetting_Created()
        {
            // This will only be creaqted once but the first time, set the defaults
            GenericPOPrefix = "800";
            DaysToAverageLastPurchasedCost = 365;
        }
    }
}
