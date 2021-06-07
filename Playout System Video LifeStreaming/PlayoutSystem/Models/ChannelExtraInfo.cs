using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PlayoutSystem.Models
{
    class ChannelExtraInfo
    {
        public Process process;
        public DispatcherTimer timer;
        public int time;
        public Boolean processKill = false;
        public string currentMovieName = "";
        public string currentMovieDuration = "";
        public string nextMovieName = "";
    }
}
