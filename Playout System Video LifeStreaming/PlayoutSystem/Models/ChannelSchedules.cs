using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayoutSystem.Models
{
    class ChannelSchedules
    {
        public string schedule_day1, schedule_day2, schedule_day3, schedule_day4, schedule_day5, schedule_day6, schedule_day7, directory_name , directoryAdvs_name;

        public override string ToString()
        {
            return "Directory : "+ directory_name + "\n" +"Schedules : \n day1 : " + schedule_day1 + "\n day2 : " + schedule_day2 + "\n day3 : " + schedule_day3 + "\n day4 : " + schedule_day4 + "\n day5 : " + schedule_day5 + "\n day6 : " + schedule_day6 + "\n day7 : " + schedule_day7;
        }
    }
}
