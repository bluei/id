using System;
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
    public partial class MessagesSearch
    {
        partial void MessagesSearch_InitializeDataWorkspace(List<IDataService> saveChangesTo)
        {
            // Write your code here.
            this.AcknowledgedParam = false;
        }

        partial void ClearSearchCriteria_Execute()
        {
            // Write your code here.
            this.AcknowledgedParam = true;
        }
    }
}
