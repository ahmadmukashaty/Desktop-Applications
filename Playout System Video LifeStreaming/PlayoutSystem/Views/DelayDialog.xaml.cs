using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PlayoutSystem.Views
{
    /// <summary>
    /// Interaction logic for Combo.xaml
    /// </summary>
    public partial class DelayDialog : Window
    {
        public DelayDialog(string time)
        {
            InitializeComponent();
            string[] timeArr = time.Split(':');
            int hour = int.Parse(timeArr[0]);
            int minute = int.Parse(timeArr[1]);
            int second = int.Parse(timeArr[2]);
            channel_DelayTime.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, minute, second);
            hourTime.Text = hour.ToString();
            minuteTime.Text = minute.ToString();
            secondTime.Text = second.ToString();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        public string Time = "";
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {

            int hour, minute, second;
            if(hourTime.Text.Length==0)
            {
                hour = 0;
            }
            else
            {
                hour = int.Parse(hourTime.Text);
            }
            if (minuteTime.Text.Length == 0)
            {
                minute = 0;
            }
            else
            {
                minute = int.Parse(minuteTime.Text);
            }
            if (secondTime.Text.Length == 0)
            {
                second = 0;
            }
            else
            {
                second = int.Parse(secondTime.Text);
            }
            try
            {
                channel_DelayTime.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, minute, second);
                if (channel_DelayTime.Text == null)
                {
                    Time = "00:00:00";
                }
                else if (channel_DelayTime.Text.Length == 0)
                {
                    Time = "00:00:00";
                }
                else
                {
                    Time = channel_DelayTime.Text;
                }
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in Time");
            }

        }
    }
}
