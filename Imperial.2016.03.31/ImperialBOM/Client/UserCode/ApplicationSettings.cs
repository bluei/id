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
    public partial class ApplicationSettings
    {
        partial void AppSetting_Loaded(bool succeeded)
        {
            this.DisplayName = "Settings";

            // if this is the first time opening this screen and no app setting record exists, create one.
            if (this.AppSetting == null)
            {
                this.DataWorkspace.ApplicationData.AppSettings.AddNew();
                this.DataWorkspace.ApplicationData.SaveChanges();
                this.Refresh();
            }
        }

        partial void ApplicationSettings_Saved()
        {
            this.Close(false);
        }

    }
}