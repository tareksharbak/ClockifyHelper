using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClockifyHelper
{
    public class ApplicationSettings
    {
        public string ApiKey { get; set; }
        public string DefaultProjectName { get; set; }
        public int IdleThresholdMinutes { get; set; }
        public bool ShowInSystemTray { get; set; }
        public bool MinimizeOnClose { get; set; }
        public bool EnableNotifications { get; set; }
    }
}
