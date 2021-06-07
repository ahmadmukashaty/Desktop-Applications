using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayoutSystem.Models
{
    class Channel
    {
        public int channel_id;
        public string channel_state = "off";
        public ChannelOptions channelOptions = new ChannelOptions();
        public LogoOptions logoOptions = new LogoOptions();
        public SubtitleOptions subtitleOptions = new SubtitleOptions();
        public ChannelSchedules channelSchedules = new ChannelSchedules();
        public ChannelExtraInfo channelExtraInfo = new ChannelExtraInfo();
        public override string ToString()
        {
            return "channel_id : " + channel_id + "\n" + channelOptions.ToString() + "\n" + logoOptions.ToString() + "\n" + subtitleOptions.ToString() + "\n" + channelSchedules.ToString();
        }
    }
}
