using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayoutSystem.Models
{
    class LogoOptions
    {
        public string logo_state, logo_name, logo_X, logo_Y;

        public override string ToString()
        {
            return "Logo Options : \n name : " + logo_name + " state : " + logo_state + " X : " + logo_X + " Y : " + logo_Y;
        }

        public string logoBrowse_Click()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.Filter = "Files (*.jpg,*.png,*.jpeg)|*.jpg;*.png;*.jpeg";

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
