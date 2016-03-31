using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.LightSwitch;
namespace LightSwitchApplication
{
    public partial class Message
    {
        partial void Message_Created()
        {
            Created = DateTime.Now;
            Acknowledged = false;
            Priority = 1;
        }
    }
}
