using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.LightSwitch;
namespace LightSwitchApplication
{
    public partial class ProductGroup
    {
        partial void ProductGroupDisplay_Compute(ref string result)
        {
            // used for sorting BOM
            result = string.Format("{0:0000}: {1}", Seq, Name);
        }
    }
}
