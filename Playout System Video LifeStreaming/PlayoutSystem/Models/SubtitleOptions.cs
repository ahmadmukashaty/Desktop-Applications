using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayoutSystem.Models
{
    class SubtitleOptions
    {
        public string subtitle_state, subtitle_name, subtitle_X, subtitle_Y, subtitle_appear_H, subtitle_appear_M, subtitle_disappear_M, subtitle_disappear_S, subtitle_appearance_state;

        public override string ToString()
        {
            return "Subtitle Options : \n name : " + subtitle_name + " state : " + subtitle_state + " X : " + subtitle_X + " Y : \n" + subtitle_Y + " appear_H : " + subtitle_appear_H + " appear_M : " + subtitle_appear_M + " disappear_M : " + subtitle_disappear_M + " disappear_S : " + subtitle_disappear_S + " appearance_state : " + subtitle_appearance_state;
        }

        public string subtitleBrowse_Click()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.Filter = "Files (*.gif)|*.gif";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                string path = dlg.FileName;
                return path;
            }
            return "";
        }
    }
}
