using nsoftware.IPWorksEncrypt;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PlayoutSystem.Views
{
    /// <summary>
    /// Interaction logic for ImportDialog.xaml
    /// </summary>
    public partial class ImportDialog : Window
    {
        private bool isValid = false;

        public ImportDialog()
        {
            InitializeComponent();
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            txtPath.Text = BrowseFile();
        }

        private string BrowseFile()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Files (*.lic)|*.lic";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                return dlg.FileName;
            }
            return "";
        }

        public string ImportPath
        {
            get { return txtPath.Text; }
            set { txtPath.Text = value; }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (txtPath.Text.Equals(""))
            {
                MessageBox.Show("Please import the path of License");
                isValid = false;
            }
                
            else
            {
                isValid = true;
                DialogResult = true;
            }
               
        }

        private void onClosing(object sender, CancelEventArgs e)
        {
            if(!isValid)
              Application.Current.Shutdown();
        }
    }
}
