using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayoutSystem.Models
{
    class ChannelOptions
    {
        public string channel_name,streamming_state, network, port, networkUDP, portUDP,startDate,startTime;

        public override string ToString()
        {
            return "Channnel Options : \n name : " + channel_name + " streamming_state : " + streamming_state + " network : " + network + " port : " + port + " UDPnetwork : " + networkUDP + " UDPport : " + portUDP;
        }
    }
}
