using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.IO;
using PlayoutSystem.Models;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Linq;
using PlayoutSystem.Views;
using System.Windows.Media;
using System.Windows.Controls;
using Hardcodet.Wpf.Util;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;
using Parago.Windows;
using System.Data.SQLite;
using Microsoft.WindowsAPICodePack.Dialogs;
using NReco.VideoInfo;
using System.Windows.Controls.Primitives;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using nsoftware.IPWorksEncrypt;
using System.Reflection;

namespace PlayoutSystem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region variables

        #region SQLite variables
        SQLiteConnection sqlite_conn;
        SQLiteCommand sqlite_cmd;
        SQLiteDataReader sqlite_datareader;
        #endregion

        #region channels variables
        //this list has all channels info
        List<Channel> channels = new List<Channel>();
        List<List<Movie>> moviesDay1 = new List<List<Movie>>();
        List<List<Movie>> moviesDay2 = new List<List<Movie>>();
        List<List<Movie>> moviesDay3 = new List<List<Movie>>();
        List<List<Movie>> moviesDay4 = new List<List<Movie>>();
        List<List<Movie>> moviesDay5 = new List<List<Movie>>();
        List<List<Movie>> moviesDay6 = new List<List<Movie>>();
        List<List<Movie>> moviesDay7 = new List<List<Movie>>();
        int channelIndex = 0;
        #endregion

        #region constants
        const string TWELVEHOURS = "12:00:00";
        const string FORTEENHOURS = "24:00:00";
        const string ZEROHOURS = "00:00:00";
        #endregion

        #region boolean variables for streamming
        Boolean windwoClose = false;
        #endregion

        #region DraggedItem

        int sourceIndex;
        public static readonly DependencyProperty DraggedItemProperty =
            DependencyProperty.Register("DraggedItem", typeof(Movie), typeof(MainWindow));

        public Movie DraggedItem
        {
            get { return (Movie)GetValue(DraggedItemProperty); }
            set { SetValue(DraggedItemProperty, value); }
        }

        #endregion

        #region IPWork Encrypt

        private IContainer components;
        private Ezcrypt ezcrypt1;

        #endregion

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            
           // Properties.Settings.Default.Reset();
           // Properties.Settings.Default.Save();
            
            
            this.components = new Container();
            this.ezcrypt1 = new Ezcrypt(this.components);
            //if (Properties.Settings.Default.NoChannel.Equals(""))
            //{
            //    FillSettings();
            //    getHardWareInfo();
            //    ShowImportDialog();
            //}
            
            //else
            {
                InitializeDigitalClock();
                CreateDatabaseForChannels();
                GetChannelsInfo();
                InsertChannelsInChannelsGrid();
                InsertItemsInChannelInterface(channelIndex);
            }  
        }

        #region what is done when open the project

        #region  Initialize digital clock

        public void InitializeDigitalClock()
        {
            DispatcherTimer DigitalClockTimer = new DispatcherTimer();
            DigitalClockTimer.Interval = new TimeSpan(0, 0, 0, 0, 100); // 100 Milliseconds 
            DigitalClockTimer.Tick += DigitalClock_Tick;
            DigitalClockTimer.Start();
        }

        void DigitalClock_Tick(object sender, EventArgs e)
        {
            tbk_clock.Text = DateTime.Now.ToString("HH:mm:ss");
            dataOfSystem.Content = DateTime.Now.ToString("M/d/yyyy");
            string date = DateTime.Now.ToString("M/d/yyyy");
            string time = DateTime.Now.ToString("HH:mm:ss");
            for (int i = 0; i < channels.Count; i++)
            {
                string channelDate = channels[i].channelOptions.startDate;
                string channelTime = channels[i].channelOptions.startTime;
                if (date.Equals(channelDate))
                {
                    if (time.Equals(channelTime))
                    {
                        if (channels[i].channel_state.Equals("off"))
                        {
                            RunChannelStreamming(i);
                        }
                    }
                }
            }
        }

        public void RunChannelStreamming(int streammingIndex)
        {
            if (channelIndex == streammingIndex)
            {
                channel_state_btn.IsChecked = true;
                channel_state_btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
            else
            {
                if (channels[streammingIndex].channel_state.Equals("off"))
                {
                    string errorMsg = "Error In Channel : " + channels[streammingIndex].channelOptions.channel_name + "\n";
                    Boolean error = false;
                    if (channels[streammingIndex].channelOptions.network.Length == 0)
                    {
                        error = true;
                        errorMsg += "Network is required \n";
                    }
                    if (channels[streammingIndex].channelOptions.port.Length == 0)
                    {
                        error = true;
                        errorMsg += "Port is required \n";
                    }
                    if (channels[streammingIndex].channelOptions.streamming_state.Equals("UDP"))
                    {
                        if (channels[streammingIndex].channelOptions.networkUDP.Length == 0)
                        {
                            error = true;
                            errorMsg += "UDP Network is required \n";
                        }
                        if (channels[streammingIndex].channelOptions.portUDP.Length == 0)
                        {
                            error = true;
                            errorMsg += "UDP Port is required \n";
                        }
                    }
                    if (moviesDay1[streammingIndex].Count == 0 && channels[streammingIndex].channelOptions.streamming_state.Equals("files"))
                    {
                        error = true;
                        errorMsg += "You have to fill schedule of Day1 and save it \n";
                    }
                    if (error)
                    {
                        errorMsg += "Check that you save information \n";
                        MessageBox.Show(errorMsg);
                    }
                    else
                    {
                        channels[streammingIndex].channel_state = "on";

                        if (channels[streammingIndex].channelOptions.streamming_state.Equals("UDP"))
                        {
                            Thread thread;
                            channels[streammingIndex].channelExtraInfo.processKill = false;
                            thread = new Thread(() => StartStreammingUDP(streammingIndex));
                            thread.Start();
                        }
                        else
                        {
                            Thread thread;
                            thread = new Thread(() => StartStreammingFiles(streammingIndex));
                            thread.Start();
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Channel " + channels[streammingIndex].channelOptions.channel_name + " is already running");
                }
            }
        }

        #endregion


        //this function just create database for channels if it doesn't exist
        public void CreateDatabaseForChannels()
        {
            if (File.Exists("database.db"))
            {
                return;
            }
            else
            {
                //Create Database For All Channels
                sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=True;Compress=True;");

                sqlite_conn.Open();

                sqlite_cmd = sqlite_conn.CreateCommand();

                sqlite_cmd.CommandText = "CREATE TABLE Channels (channel_id INTEGER PRIMARY KEY , channel_name VARCHAR(100), streamming_state VARCHAR(100), network VARCHAR(100), port VARCHAR(100), UDPnetwork VARCHAR(100), UDPport VARCHAR(100), startDate VARCHAR(100), startTime VARCHAR(100), logo_state VARCHAR(100), logo_name VARCHAR(100), logo_X VARCHAR(100), logo_Y VARCHAR(100), subtitle_state VARCHAR(100), subtitle_name VARCHAR(100), subtitle_X VARCHAR(100), subtitle_Y VARCHAR(100), subtitle_appear_H VARCHAR(100), subtitle_appear_M VARCHAR(100), subtitle_disappear_M VARCHAR(100), subtitle_disappear_S VARCHAR(100), subtitle_appearance_state VARCHAR(100), directory_name VARCHAR(100), directoryAdvs_name VARCHAR(100), schedule_day1 VARCHAR(100), schedule_day2 VARCHAR(100), schedule_day3 VARCHAR(100), schedule_day4 VARCHAR(100), schedule_day5 VARCHAR(100), schedule_day6 VARCHAR(100), schedule_day7 VARCHAR(100));";

                sqlite_cmd.ExecuteNonQuery();

                sqlite_cmd.CommandText = "INSERT INTO Channels (channel_name, streamming_state, network, port, UDPnetwork, UDPport, startDate, startTime, logo_state, logo_name, logo_X, logo_Y, subtitle_state, subtitle_X, subtitle_Y, subtitle_appear_H, subtitle_appear_M, subtitle_disappear_M, subtitle_disappear_S, subtitle_appearance_state, subtitle_name, directory_name, directoryAdvs_name, schedule_day1, schedule_day2, schedule_day3, schedule_day4, schedule_day5, schedule_day6, schedule_day7) VALUES ('channel1','files','','','','','','','off','','','','off','','','','','','','always','','','','(12)','(12)','(12)','(12)','(12)','(12)','(12)');";

                sqlite_cmd.ExecuteNonQuery();

                sqlite_conn.Close();

                string fileName = "Playout.exe";
                string exeDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string sourcePath = @exeDir;
                string targetPath = @exeDir + "\\libraries";

                // Use Path class to manipulate file and directory paths.
                string sourceFile = System.IO.Path.Combine(sourcePath, fileName);
                string destFile = System.IO.Path.Combine(targetPath,"channel1.exe");

                // To copy a folder's contents to a new location:
                // Create a new target folder, if necessary.
                if (Directory.Exists(targetPath))
                {
                    Directory.Delete(targetPath, true);
                }
                Directory.CreateDirectory(targetPath);

                // To copy a file to another location and 
                // overwrite the destination file if it already exists.
                File.Copy(sourceFile, destFile, true);

            }
        }


        //this function get all channels information and insert it into (List<Channel> channels)
        public void GetChannelsInfo()
        {
            sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

            sqlite_conn.Open();

            sqlite_cmd = sqlite_conn.CreateCommand();

            sqlite_cmd.CommandText = "SELECT * FROM Channels";

            sqlite_datareader = sqlite_cmd.ExecuteReader();


            while (sqlite_datareader.Read())
            {

                Channel channel = new Channel();
                channel.channel_id = int.Parse(sqlite_datareader["channel_id"].ToString());
                channel.channelOptions.channel_name = sqlite_datareader["channel_name"].ToString();
                channel.channelOptions.streamming_state = sqlite_datareader["streamming_state"].ToString();
                channel.channelOptions.network = sqlite_datareader["network"].ToString();
                channel.channelOptions.port = sqlite_datareader["port"].ToString();
                channel.channelOptions.networkUDP = sqlite_datareader["UDPnetwork"].ToString();
                channel.channelOptions.portUDP = sqlite_datareader["UDPport"].ToString();
                channel.channelOptions.startDate = sqlite_datareader["startDate"].ToString();
                channel.channelOptions.startTime = sqlite_datareader["startTime"].ToString();

                channel.logoOptions.logo_state = sqlite_datareader["logo_state"].ToString();
                channel.logoOptions.logo_name = sqlite_datareader["logo_name"].ToString();
                channel.logoOptions.logo_X = sqlite_datareader["logo_X"].ToString();
                channel.logoOptions.logo_Y = sqlite_datareader["logo_Y"].ToString();

                channel.subtitleOptions.subtitle_name = sqlite_datareader["subtitle_name"].ToString();
                channel.subtitleOptions.subtitle_state = sqlite_datareader["subtitle_state"].ToString();
                channel.subtitleOptions.subtitle_X = sqlite_datareader["subtitle_X"].ToString();
                channel.subtitleOptions.subtitle_Y = sqlite_datareader["subtitle_Y"].ToString();
                channel.subtitleOptions.subtitle_appearance_state = sqlite_datareader["subtitle_appearance_state"].ToString();
                channel.subtitleOptions.subtitle_appear_H = sqlite_datareader["subtitle_appear_H"].ToString();
                channel.subtitleOptions.subtitle_appear_M = sqlite_datareader["subtitle_appear_M"].ToString();
                channel.subtitleOptions.subtitle_disappear_S = sqlite_datareader["subtitle_disappear_S"].ToString();
                channel.subtitleOptions.subtitle_disappear_M = sqlite_datareader["subtitle_disappear_M"].ToString();

                channel.channelSchedules.directory_name = sqlite_datareader["directory_name"].ToString();
                channel.channelSchedules.directoryAdvs_name = sqlite_datareader["directoryAdvs_name"].ToString();
                channel.channelSchedules.schedule_day1 = sqlite_datareader["schedule_day1"].ToString();
                channel.channelSchedules.schedule_day2 = sqlite_datareader["schedule_day2"].ToString();
                channel.channelSchedules.schedule_day3 = sqlite_datareader["schedule_day3"].ToString();
                channel.channelSchedules.schedule_day4 = sqlite_datareader["schedule_day4"].ToString();
                channel.channelSchedules.schedule_day5 = sqlite_datareader["schedule_day5"].ToString();
                channel.channelSchedules.schedule_day6 = sqlite_datareader["schedule_day6"].ToString();
                channel.channelSchedules.schedule_day7 = sqlite_datareader["schedule_day7"].ToString();

                channels.Add(channel);

                List<Movie> movieDay1 = new List<Movie>();
                string scheduleDay1 = channel.channelSchedules.schedule_day1.Substring(4);
                if (scheduleDay1.Length > 0)
                {
                    string[] tokens = scheduleDay1.Split('%');
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        string[] movieInfo = tokens[i].Split('&');
                        Movie movie = new Movie();
                        movie.Name = movieInfo[0];
                        movie.Duration = movieInfo[1];
                        movie.Delay = movieInfo[2];
                        movie.Directory = movieInfo[3];
                        movieDay1.Add(movie);
                    }
                }
                moviesDay1.Add(movieDay1);

                List<Movie> movieDay2 = new List<Movie>();
                string scheduleDay2 = channel.channelSchedules.schedule_day2.Substring(4);
                if (scheduleDay2.Length > 0)
                {
                    string[] tokens = scheduleDay2.Split('%');
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        string[] movieInfo = tokens[i].Split('&');
                        Movie movie = new Movie();
                        movie.Name = movieInfo[0];
                        movie.Duration = movieInfo[1];
                        movie.Delay = movieInfo[2];
                        movie.Directory = movieInfo[3];
                        movieDay2.Add(movie);
                    }
                }
                moviesDay2.Add(movieDay2);

                List<Movie> movieDay3 = new List<Movie>();
                string scheduleDay3 = channel.channelSchedules.schedule_day3.Substring(4);
                if (scheduleDay3.Length > 0)
                {
                    string[] tokens = scheduleDay3.Split('%');
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        string[] movieInfo = tokens[i].Split('&');
                        Movie movie = new Movie();
                        movie.Name = movieInfo[0];
                        movie.Duration = movieInfo[1];
                        movie.Delay = movieInfo[2];
                        movie.Directory = movieInfo[3];
                        movieDay3.Add(movie);
                    }
                }
                moviesDay3.Add(movieDay3);

                List<Movie> movieDay4 = new List<Movie>();
                string scheduleDay4 = channel.channelSchedules.schedule_day4.Substring(4);
                if (scheduleDay4.Length > 0)
                {
                    string[] tokens = scheduleDay4.Split('%');
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        string[] movieInfo = tokens[i].Split('&');
                        Movie movie = new Movie();
                        movie.Name = movieInfo[0];
                        movie.Duration = movieInfo[1];
                        movie.Delay = movieInfo[2];
                        movie.Directory = movieInfo[3];
                        movieDay4.Add(movie);
                    }
                }
                moviesDay4.Add(movieDay4);

                List<Movie> movieDay5 = new List<Movie>();
                string scheduleDay5 = channel.channelSchedules.schedule_day5.Substring(4);
                if (scheduleDay5.Length > 0)
                {
                    string[] tokens = scheduleDay5.Split('%');
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        string[] movieInfo = tokens[i].Split('&');
                        Movie movie = new Movie();
                        movie.Name = movieInfo[0];
                        movie.Duration = movieInfo[1];
                        movie.Delay = movieInfo[2];
                        movie.Directory = movieInfo[3];
                        movieDay5.Add(movie);
                    }
                }
                moviesDay5.Add(movieDay5);

                List<Movie> movieDay6 = new List<Movie>();
                string scheduleDay6 = channel.channelSchedules.schedule_day6.Substring(4);
                if (scheduleDay6.Length > 0)
                {
                    string[] tokens = scheduleDay6.Split('%');
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        string[] movieInfo = tokens[i].Split('&');
                        Movie movie = new Movie();
                        movie.Name = movieInfo[0];
                        movie.Duration = movieInfo[1];
                        movie.Delay = movieInfo[2];
                        movie.Directory = movieInfo[3];
                        movieDay6.Add(movie);
                    }
                }
                moviesDay6.Add(movieDay6);

                List<Movie> movieDay7 = new List<Movie>();
                string scheduleDay7 = channel.channelSchedules.schedule_day7.Substring(4);
                if (scheduleDay7.Length > 0)
                {
                    string[] tokens = scheduleDay7.Split('%');
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        string[] movieInfo = tokens[i].Split('&');
                        Movie movie = new Movie();
                        movie.Name = movieInfo[0];
                        movie.Duration = movieInfo[1];
                        movie.Delay = movieInfo[2];
                        movie.Directory = movieInfo[3];
                        movieDay7.Add(movie);
                    }
                }
                moviesDay7.Add(movieDay7);
            }

            sqlite_conn.Close();
        }


        public void InsertChannelsInChannelsGrid()
        {
            for (int i = 0; i < channels.Count; i++)
            {
                ChannelsGrid channelGrid = new ChannelsGrid();
                channelGrid.ChannelName = channels[i].channelOptions.channel_name;
                channelsGrid.Items.Add(channelGrid);
            }
        }


        //this function fill all Interface from information in channels  list and also recall : GetAllFilesFromDirectory to add all movies in datagrid on right fillDataGridSchedule to add all saved movies in datagrid on left
        public void InsertItemsInChannelInterface(int channelIndex)
        {
            Channel channel = channels[channelIndex];
            channelsGrid.SelectedIndex = channelIndex;
            //channel options
            if (channel.channelOptions.streamming_state.Equals("files"))
            {
                streamming_state_files.IsChecked = true;
            }
            else
            {
                streamming_state_UDP.IsChecked = true;
            }
            networkBox.Text = channel.channelOptions.network;
            portBox.Text = channel.channelOptions.port;
            networkUDPBox.Text = channel.channelOptions.networkUDP;
            portUDPBox.Text = channel.channelOptions.portUDP;
            channel_StartDate.SelectedDate = null;
            channel_StartTime.Value = null;
            if (channel.channelOptions.startDate.Length > 0)
            {
                string[] date = channel.channelOptions.startDate.Split('/');
                int day = int.Parse(date[1]);
                int month = int.Parse(date[0]);
                int year = int.Parse(date[2]);
                channel_StartDate.SelectedDate = new DateTime(year, month, day);
            }
            if (channel.channelOptions.startTime.Length > 0)
            {
                string[] time = channel.channelOptions.startTime.Split(':');
                int hour = int.Parse(time[0]);
                int minute = int.Parse(time[1]);
                int second = int.Parse(time[2]);
                channel_StartTime.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, minute, second);
            }
            //logo options
            if (channel.logoOptions.logo_state.Equals("off"))
            {
                logo_state_btn.IsChecked = false;
            }
            else
            {
                logo_state_btn.IsChecked = true;
            }
            logo_nameBox.Text = channel.logoOptions.logo_name;
            logoX_Box.Text = channel.logoOptions.logo_X;
            logoY_Box.Text = channel.logoOptions.logo_Y;
            //subtitle option
            if (channel.subtitleOptions.subtitle_state.Equals("off"))
            {
                subtitle_state_btn.IsChecked = false;
            }
            else
            {
                subtitle_state_btn.IsChecked = true;
            }
            subtitle_nameBox.Text = channel.subtitleOptions.subtitle_name;
            subtitle_X_Box.Text = channel.subtitleOptions.subtitle_X;
            subtitle_Y_Box.Text = channel.subtitleOptions.subtitle_Y;
            subtitle_appear_H.Text = channel.subtitleOptions.subtitle_appear_H;
            subtitle_appear_M.Text = channel.subtitleOptions.subtitle_appear_M;
            subtitle_disappear_M.Text = channel.subtitleOptions.subtitle_disappear_M;
            subtitle_disappear_S.Text = channel.subtitleOptions.subtitle_disappear_S;
            if (channel.subtitleOptions.subtitle_appearance_state.Equals("always"))
            {
                subtitle_appearance_state_always.IsChecked = true;
            }
            else
            {
                subtitle_appearance_state_choosen.IsChecked = true;
            }
            //////////////////////////////////////////////////////////////
            //schedule options
            directoryNameBox.Text = channel.channelSchedules.directory_name;
            directoryAdvsNameBox.Text = channel.channelSchedules.directoryAdvs_name;
            string directory = directoryNameBox.Text;
            allMovieGrid.Items.Clear();
            if (Directory.Exists(directory))
            {
                GetAllFilesFromDirectory(directory,true);
            }
            string directoryAdvs = directoryAdvsNameBox.Text;
            allAdvsGrid.Items.Clear();
            if (Directory.Exists(directoryAdvs))
            {
                GetAllFilesFromDirectory(directoryAdvs,false);
            }
            //schedule options (day1)
            string scheduleFormatDay1 = channel.channelSchedules.schedule_day1;
            string totalHoursDay1 = scheduleFormatDay1.Substring(1, 2);
            string scheduleDay1 = scheduleFormatDay1.Substring(4);
            if (totalHoursDay1.Equals("12"))
            {
                scheduleDay1Hour12.IsChecked = true;
                if (scheduleDay1.Length == 0)
                {
                    scheduleDay1ElapsedTime.Content = ZEROHOURS;
                    scheduleDay1RestTime.Content = TWELVEHOURS;
                }
                else
                {
                    scheduleDay1ElapsedTime.Content = TWELVEHOURS;
                    scheduleDay1RestTime.Content = ZEROHOURS;
                }
            }
            else
            {
                scheduleDay1Hour24.IsChecked = true;
                if (scheduleDay1.Length == 0)
                {
                    scheduleDay1ElapsedTime.Content = ZEROHOURS;
                    scheduleDay1RestTime.Content = FORTEENHOURS;
                }
                else
                {
                    scheduleDay1ElapsedTime.Content = FORTEENHOURS;
                    scheduleDay1RestTime.Content = ZEROHOURS;
                }
            }
            Day1Grid.Items.Clear();
            for (int i = 0; i < moviesDay1[channelIndex].Count; i++)
            {
                Day1Grid.Items.Add(moviesDay1[channelIndex][i]);
            }
            //schedule options (day2)
            string scheduleFormatDay2 = channel.channelSchedules.schedule_day2;
            string totalHoursDay2 = scheduleFormatDay2.Substring(1, 2);
            string scheduleDay2 = scheduleFormatDay2.Substring(4);
            if (totalHoursDay2.Equals("12"))
            {
                scheduleDay2Hour12.IsChecked = true;
                if (scheduleDay2.Length == 0)
                {
                    scheduleDay2ElapsedTime.Content = ZEROHOURS;
                    scheduleDay2RestTime.Content = TWELVEHOURS;
                }
                else
                {
                    scheduleDay2ElapsedTime.Content = TWELVEHOURS;
                    scheduleDay2RestTime.Content = ZEROHOURS;
                }
            }
            else
            {
                scheduleDay2Hour24.IsChecked = true;
                if (scheduleDay2.Length == 0)
                {
                    scheduleDay2ElapsedTime.Content = ZEROHOURS;
                    scheduleDay2RestTime.Content = FORTEENHOURS;
                }
                else
                {
                    scheduleDay2ElapsedTime.Content = FORTEENHOURS;
                    scheduleDay2RestTime.Content = ZEROHOURS;
                }
            }
            Day2Grid.Items.Clear();
            for (int i = 0; i < moviesDay2[channelIndex].Count; i++)
            {
                Day2Grid.Items.Add(moviesDay2[channelIndex][i]);
            }
            //schedule options (day3)
            string scheduleFormatDay3 = channel.channelSchedules.schedule_day3;
            string totalHoursDay3 = scheduleFormatDay3.Substring(1, 2);
            string scheduleDay3 = scheduleFormatDay3.Substring(4);
            if (totalHoursDay3.Equals("12"))
            {
                scheduleDay3Hour12.IsChecked = true;
                if (scheduleDay3.Length == 0)
                {
                    scheduleDay3ElapsedTime.Content = ZEROHOURS;
                    scheduleDay3RestTime.Content = TWELVEHOURS;
                }
                else
                {
                    scheduleDay3ElapsedTime.Content = TWELVEHOURS;
                    scheduleDay3RestTime.Content = ZEROHOURS;
                }
            }
            else
            {
                scheduleDay3Hour24.IsChecked = true;
                if (scheduleDay3.Length == 0)
                {
                    scheduleDay3ElapsedTime.Content = ZEROHOURS;
                    scheduleDay3RestTime.Content = FORTEENHOURS;
                }
                else
                {
                    scheduleDay3ElapsedTime.Content = FORTEENHOURS;
                    scheduleDay3RestTime.Content = ZEROHOURS;
                }
            }
            Day3Grid.Items.Clear();
            for (int i = 0; i < moviesDay3[channelIndex].Count; i++)
            {
                Day3Grid.Items.Add(moviesDay3[channelIndex][i]);
            }
            //schedule options (day4)
            string scheduleFormatDay4 = channel.channelSchedules.schedule_day4;
            string totalHoursDay4 = scheduleFormatDay4.Substring(1, 2);
            string scheduleDay4 = scheduleFormatDay4.Substring(4);
            if (totalHoursDay4.Equals("12"))
            {
                scheduleDay4Hour12.IsChecked = true;
                if (scheduleDay4.Length == 0)
                {
                    scheduleDay4ElapsedTime.Content = ZEROHOURS;
                    scheduleDay4RestTime.Content = TWELVEHOURS;
                }
                else
                {
                    scheduleDay4ElapsedTime.Content = TWELVEHOURS;
                    scheduleDay4RestTime.Content = ZEROHOURS;
                }
            }
            else
            {
                scheduleDay4Hour24.IsChecked = true;
                if (scheduleDay4.Length == 0)
                {
                    scheduleDay4ElapsedTime.Content = ZEROHOURS;
                    scheduleDay4RestTime.Content = FORTEENHOURS;
                }
                else
                {
                    scheduleDay4ElapsedTime.Content = FORTEENHOURS;
                    scheduleDay4RestTime.Content = ZEROHOURS;
                }
            }
            Day4Grid.Items.Clear();
            for (int i = 0; i < moviesDay4[channelIndex].Count; i++)
            {
                Day4Grid.Items.Add(moviesDay4[channelIndex][i]);
            }
            //schedule options (day5)
            string scheduleFormatDay5 = channel.channelSchedules.schedule_day5;
            string totalHoursDay5 = scheduleFormatDay5.Substring(1, 2);
            string scheduleDay5 = scheduleFormatDay5.Substring(4);
            if (totalHoursDay5.Equals("12"))
            {
                scheduleDay5Hour12.IsChecked = true;
                if (scheduleDay5.Length == 0)
                {
                    scheduleDay5ElapsedTime.Content = ZEROHOURS;
                    scheduleDay5RestTime.Content = TWELVEHOURS;
                }
                else
                {
                    scheduleDay5ElapsedTime.Content = TWELVEHOURS;
                    scheduleDay5RestTime.Content = ZEROHOURS;
                }
            }
            else
            {
                scheduleDay5Hour24.IsChecked = true;
                if (scheduleDay5.Length == 0)
                {
                    scheduleDay5ElapsedTime.Content = ZEROHOURS;
                    scheduleDay5RestTime.Content = FORTEENHOURS;
                }
                else
                {
                    scheduleDay5ElapsedTime.Content = FORTEENHOURS;
                    scheduleDay5RestTime.Content = ZEROHOURS;
                }
            }
            Day5Grid.Items.Clear();
            for (int i = 0; i < moviesDay5[channelIndex].Count; i++)
            {
                Day5Grid.Items.Add(moviesDay5[channelIndex][i]);
            }
            //schedule options (day6)
            string scheduleFormatDay6 = channel.channelSchedules.schedule_day6;
            string totalHoursDay6 = scheduleFormatDay6.Substring(1, 2);
            string scheduleDay6 = scheduleFormatDay6.Substring(4);
            if (totalHoursDay6.Equals("12"))
            {
                scheduleDay6Hour12.IsChecked = true;
                if (scheduleDay6.Length == 0)
                {
                    scheduleDay6ElapsedTime.Content = ZEROHOURS;
                    scheduleDay6RestTime.Content = TWELVEHOURS;
                }
                else
                {
                    scheduleDay6ElapsedTime.Content = TWELVEHOURS;
                    scheduleDay6RestTime.Content = ZEROHOURS;
                }
            }
            else
            {
                scheduleDay6Hour24.IsChecked = true;
                if (scheduleDay6.Length == 0)
                {
                    scheduleDay6ElapsedTime.Content = ZEROHOURS;
                    scheduleDay6RestTime.Content = FORTEENHOURS;
                }
                else
                {
                    scheduleDay6ElapsedTime.Content = FORTEENHOURS;
                    scheduleDay6RestTime.Content = ZEROHOURS;
                }
            }
            Day6Grid.Items.Clear();
            for (int i = 0; i < moviesDay6[channelIndex].Count; i++)
            {
                Day6Grid.Items.Add(moviesDay6[channelIndex][i]);
            }
            //schedule options (day7)
            string scheduleFormatDay7 = channel.channelSchedules.schedule_day7;
            string totalHoursDay7 = scheduleFormatDay7.Substring(1, 2);
            string scheduleDay7 = scheduleFormatDay7.Substring(4);
            if (totalHoursDay7.Equals("12"))
            {
                scheduleDay7Hour12.IsChecked = true;
                if (scheduleDay7.Length == 0)
                {
                    scheduleDay7ElapsedTime.Content = ZEROHOURS;
                    scheduleDay7RestTime.Content = TWELVEHOURS;
                }
                else
                {
                    scheduleDay7ElapsedTime.Content = TWELVEHOURS;
                    scheduleDay7RestTime.Content = ZEROHOURS;
                }
            }
            else
            {
                scheduleDay7Hour24.IsChecked = true;
                if (scheduleDay7.Length == 0)
                {
                    scheduleDay7ElapsedTime.Content = ZEROHOURS;
                    scheduleDay7RestTime.Content = FORTEENHOURS;
                }
                else
                {
                    scheduleDay7ElapsedTime.Content = FORTEENHOURS;
                    scheduleDay7RestTime.Content = ZEROHOURS;
                }
            }
            Day7Grid.Items.Clear();
            for (int i = 0; i < moviesDay7[channelIndex].Count; i++)
            {
                Day7Grid.Items.Add(moviesDay7[channelIndex][i]);
            }

            if (channel.channel_state.Equals("off"))
            {
                channel_state_btn.IsChecked = false;
                streamming_state_files.IsEnabled = true;
                streamming_state_UDP.IsEnabled = true;
                if (streamming_state_UDP.IsChecked == true)
                {
                    networkBox.IsEnabled = true;
                    portBox.IsEnabled = true;
                    networkUDPBox.IsEnabled = true;
                    portUDPBox.IsEnabled = true;
                }
                else
                {
                    scheduleDay1Hour12.IsEnabled = true;
                    scheduleDay1Hour24.IsEnabled = true;
                    scheduleDay1Save_btn.IsEnabled = true;
                    networkUDPBox.IsEnabled = false;
                    portUDPBox.IsEnabled = false;
                    networkBox.IsEnabled = true;
                    portBox.IsEnabled = true;
                }
                nextMovieDuration.Content = "";
            }
            else
            {
                channel_state_btn.IsChecked = true;
                streamming_state_files.IsEnabled = false;
                streamming_state_UDP.IsEnabled = false;
                if (streamming_state_UDP.IsChecked == true)
                {
                    networkBox.IsEnabled = false;
                    portBox.IsEnabled = false;
                    networkUDPBox.IsEnabled = false;
                    portUDPBox.IsEnabled = false;
                    scheduleDay1Hour12.IsEnabled = true;
                    scheduleDay1Hour24.IsEnabled = true;
                    scheduleDay1Save_btn.IsEnabled = true;
                }
                else
                {
                    scheduleDay1Hour12.IsEnabled = false;
                    scheduleDay1Hour24.IsEnabled = false;
                    scheduleDay1Save_btn.IsEnabled = false;
                    networkBox.IsEnabled = true;
                    portBox.IsEnabled = true;
                    networkUDPBox.IsEnabled = false;
                    portUDPBox.IsEnabled = false;
                }
            }
            currentMovieName.Content = channel.channelExtraInfo.currentMovieName;
            currentMovieDuration.Content = channel.channelExtraInfo.currentMovieDuration;
            nextMovieName.Content = channel.channelExtraInfo.nextMovieName;
            nextMovieDuration.Content = "";
            channel_save_btn.IsEnabled = false;
            logo_save_btn.IsEnabled = false;
            subtitle_save_btn.IsEnabled = false;
            directorySave_btn.IsEnabled = false;
            scheduleDay1Save_btn.IsEnabled = false;
            scheduleDay2Save_btn.IsEnabled = false;
            scheduleDay3Save_btn.IsEnabled = false;
            scheduleDay4Save_btn.IsEnabled = false;
            scheduleDay5Save_btn.IsEnabled = false;
            scheduleDay6Save_btn.IsEnabled = false;
            scheduleDay7Save_btn.IsEnabled = false;
        }


        #endregion


        #region rename & add & delete & change channels events

        private void RenameChannel_Click(object sender, RoutedEventArgs e)
        {
            string oldName;
            int index = channelsGrid.SelectedIndex;

            ChannelsGrid channel = (ChannelsGrid)channelsGrid.Items[index];
            string channelName = channel.ChannelName;
            if (channels[index].channel_state.Equals("on"))
            {
                MessageBox.Show("You Can't Rename " + channel.ChannelName + " Because It's Running");
            }
            else
            {
                RenameChannelInterface renameChannelInterface = new RenameChannelInterface();
                renameChannelInterface.channelName.Text = channel.ChannelName;
                oldName = channel.ChannelName;
                if (renameChannelInterface.ShowDialog() == true)
                {
                    string newChannelName = renameChannelInterface.channelName.Text;
                    Boolean error = false;
                    if (newChannelName.Length > 0)
                    {
                        for (int i = 0; i < channels.Count && i != index; i++)
                        {
                            if (newChannelName.Equals(channels[i].channelOptions.channel_name))
                            {
                                error = true;
                            }
                        }
                        if (error)
                        {
                            MessageBox.Show("Another Channel Has The Same Name");
                        }
                        else
                        {
                            int indexofChannel = channelsGrid.Items.IndexOf(channel);
                            channel.ChannelName = newChannelName;
                            channels[index].channelOptions.channel_name = newChannelName;
                            channelsGrid.Items[indexofChannel] = channel;
                            // channelsGrid.Items.Remove(channel);
                            // channelsGrid.Items.Insert(index, channel);

                            sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

                            sqlite_conn.Open();

                            sqlite_cmd = sqlite_conn.CreateCommand();

                            sqlite_cmd.CommandText = "UPDATE Channels SET channel_name=\"" + newChannelName + "\" WHERE channel_id=" + channels[index].channel_id + ";";

                            sqlite_cmd.ExecuteNonQuery();

                            sqlite_conn.Close();

                            string exeDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                            string sourcePath = @exeDir;
                            string targetPath = @exeDir + "\\libraries";

                            File.Move(targetPath + "\\" + oldName + ".exe", targetPath + "\\" + newChannelName + ".exe");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Channel Name Is Empty");
                    }
                }
            }

        }

        private void DeleteChannel_Click(object sender, RoutedEventArgs e)
        {
            if (channels.Count > 1)
            {
                int index = channelsGrid.SelectedIndex;
                ChannelsGrid channel = (ChannelsGrid)channelsGrid.Items[index];
                string channelName = channel.ChannelName;
                if (channels[index].channel_state.Equals("on"))
                {
                    MessageBox.Show("You Can't Remove " + channel.ChannelName + " Because It's Running");
                }
                else
                {
                    MessageBoxResult m = MessageBox.Show("Do you want to remove " + channel.ChannelName + " channel?", "Delete Channel", MessageBoxButton.OKCancel);
                    if (m.ToString().Equals("OK"))
                    {

                        int id = channels[index].channel_id;

                        sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

                        sqlite_conn.Open();

                        sqlite_cmd = sqlite_conn.CreateCommand();

                        sqlite_cmd.CommandText = "DELETE FROM Channels WHERE channel_id=" + id + ";";
                        
                        sqlite_cmd.ExecuteNonQuery();

                        sqlite_conn.Close();

                        channels.Remove(channels[index]);
                        moviesDay1.Remove(moviesDay1[index]);
                        moviesDay2.Remove(moviesDay2[index]);
                        moviesDay3.Remove(moviesDay3[index]);
                        moviesDay4.Remove(moviesDay4[index]);
                        moviesDay5.Remove(moviesDay5[index]);
                        moviesDay6.Remove(moviesDay6[index]);
                        moviesDay7.Remove(moviesDay7[index]);

                        channelsGrid.Items.Remove(channel);


                        string exeDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        string targetPath = @exeDir + "\\libraries\\"+channelName+".exe";

                        // Delete a file by using File class static method...
                        if (File.Exists(targetPath))
                        {
                            // Use a try block to catch IOExceptions, to
                            // handle the case of the file already being
                            // opened by another process.
                            try
                            {
                                File.Delete(targetPath);
                            }
                            catch (IOException ex)
                            {
                            }
                        }

                    }
                }
            }
            else
            {
                MessageBox.Show("must be kept at least one channel");
            }
        }

        private void NewChannel_Click(object sender, RoutedEventArgs e)
        {
            int noChannel = int.Parse(Properties.Settings.Default.NoChannel);
          //  int noChannel = 15;
            if (channelsGrid.Items.Count < noChannel)
            {
                //if(Properties.Settings.Default.NoChannel.Equals(""))
                RenameChannelInterface renameChannelInterface = new RenameChannelInterface();
                renameChannelInterface.Title = "Add Channel ";
                if (renameChannelInterface.ShowDialog() == true)
                {
                    string newChannelName = renameChannelInterface.channelName.Text;
                    Boolean error = false;
                    if (newChannelName.Length > 0)
                    {
                        for (int i = 0; i < channels.Count; i++)
                        {
                            if (newChannelName.Equals(channels[i].channelOptions.channel_name))
                            {
                                error = true;
                            }
                        }
                        if (error)
                        {
                            MessageBox.Show("Another Channel Has The Same Name");
                        }
                        else
                        {
                            ChannelsGrid newChannel = new ChannelsGrid();
                            newChannel.ChannelName = newChannelName;
                            channelsGrid.Items.Add(newChannel);

                            sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

                            sqlite_conn.Open();

                            sqlite_cmd = sqlite_conn.CreateCommand();

                            sqlite_cmd.CommandText = "INSERT INTO Channels (channel_name, streamming_state, network, port, UDPnetwork, UDPport, startDate, startTime, logo_state, logo_name, logo_X, logo_Y, subtitle_state, subtitle_X, subtitle_Y, subtitle_appear_H, subtitle_appear_M, subtitle_disappear_M, subtitle_disappear_S, subtitle_appearance_state, subtitle_name, directory_name, directoryAdvs_name, schedule_day1, schedule_day2, schedule_day3, schedule_day4, schedule_day5, schedule_day6, schedule_day7) VALUES (\"" + newChannelName + "\",'files','','','','','','','off','','','','off','','','','','','','always','','','','(12)','(12)','(12)','(12)','(12)','(12)','(12)');";

                            sqlite_cmd.ExecuteNonQuery();

                            sqlite_cmd.CommandText = "SELECT * FROM Channels WHERE channel_name=\"" + newChannelName + "\";";

                            sqlite_datareader = sqlite_cmd.ExecuteReader();


                            while (sqlite_datareader.Read())
                            {

                                Channel channel = new Channel();
                                channel.channel_id = int.Parse(sqlite_datareader["channel_id"].ToString());
                                channel.channelOptions.channel_name = sqlite_datareader["channel_name"].ToString();
                                channel.channelOptions.streamming_state = sqlite_datareader["streamming_state"].ToString();
                                channel.channelOptions.network = sqlite_datareader["network"].ToString();
                                channel.channelOptions.port = sqlite_datareader["port"].ToString();
                                channel.channelOptions.networkUDP = sqlite_datareader["UDPnetwork"].ToString();
                                channel.channelOptions.portUDP = sqlite_datareader["UDPport"].ToString();
                                channel.channelOptions.startDate = sqlite_datareader["startDate"].ToString();
                                channel.channelOptions.startTime = sqlite_datareader["startTime"].ToString();

                                channel.logoOptions.logo_state = sqlite_datareader["logo_state"].ToString();
                                channel.logoOptions.logo_name = sqlite_datareader["logo_name"].ToString();
                                channel.logoOptions.logo_X = sqlite_datareader["logo_X"].ToString();
                                channel.logoOptions.logo_Y = sqlite_datareader["logo_Y"].ToString();

                                channel.subtitleOptions.subtitle_name = sqlite_datareader["subtitle_name"].ToString();
                                channel.subtitleOptions.subtitle_state = sqlite_datareader["subtitle_state"].ToString();
                                channel.subtitleOptions.subtitle_X = sqlite_datareader["subtitle_X"].ToString();
                                channel.subtitleOptions.subtitle_Y = sqlite_datareader["subtitle_Y"].ToString();
                                channel.subtitleOptions.subtitle_appearance_state = sqlite_datareader["subtitle_appearance_state"].ToString();
                                channel.subtitleOptions.subtitle_appear_H = sqlite_datareader["subtitle_appear_H"].ToString();
                                channel.subtitleOptions.subtitle_appear_M = sqlite_datareader["subtitle_appear_M"].ToString();
                                channel.subtitleOptions.subtitle_disappear_S = sqlite_datareader["subtitle_disappear_S"].ToString();
                                channel.subtitleOptions.subtitle_disappear_M = sqlite_datareader["subtitle_disappear_M"].ToString();

                                channel.channelSchedules.directory_name = sqlite_datareader["directory_name"].ToString();
                                channel.channelSchedules.directoryAdvs_name = sqlite_datareader["directoryAdvs_name"].ToString();
                                channel.channelSchedules.schedule_day1 = sqlite_datareader["schedule_day1"].ToString();
                                channel.channelSchedules.schedule_day2 = sqlite_datareader["schedule_day2"].ToString();
                                channel.channelSchedules.schedule_day3 = sqlite_datareader["schedule_day3"].ToString();
                                channel.channelSchedules.schedule_day4 = sqlite_datareader["schedule_day4"].ToString();
                                channel.channelSchedules.schedule_day5 = sqlite_datareader["schedule_day5"].ToString();
                                channel.channelSchedules.schedule_day6 = sqlite_datareader["schedule_day6"].ToString();
                                channel.channelSchedules.schedule_day7 = sqlite_datareader["schedule_day7"].ToString();

                                channels.Add(channel);

                                List<Movie> movieDay1 = new List<Movie>();
                                moviesDay1.Add(movieDay1);
                                List<Movie> movieDay2 = new List<Movie>();
                                moviesDay2.Add(movieDay2);
                                List<Movie> movieDay3 = new List<Movie>();
                                moviesDay3.Add(movieDay3);
                                List<Movie> movieDay4 = new List<Movie>();
                                moviesDay4.Add(movieDay4);
                                List<Movie> movieDay5 = new List<Movie>();
                                moviesDay5.Add(movieDay5);
                                List<Movie> movieDay6 = new List<Movie>();
                                moviesDay6.Add(movieDay6);
                                List<Movie> movieDay7 = new List<Movie>();
                                moviesDay7.Add(movieDay7);
                            }

                            sqlite_conn.Close();
                        }

                        string fileName = "Playout.exe";
                        string exeDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        string sourcePath = @exeDir;
                        string targetPath = @exeDir+ "\\libraries";

                        // Use Path class to manipulate file and directory paths.
                        string sourceFile = System.IO.Path.Combine(sourcePath, fileName);
                        string destFile = System.IO.Path.Combine(targetPath, newChannelName+".exe");

                        // To copy a folder's contents to a new location:
                        // Create a new target folder, if necessary.
                        if (!Directory.Exists(targetPath))
                        {
                            Directory.CreateDirectory(targetPath);
                        }

                        // To copy a file to another location and 
                        // overwrite the destination file if it already exists.
                        File.Copy(sourceFile, destFile, true);
                    }
                    else
                    {
                        MessageBox.Show("Channel Name Is Empty");
                    }
                }
            } else
            {
                MessageBox.Show("Maximum number of channels allowed is " + noChannel);
            }
        }

        private void ChannelGrid_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (channelsGrid.SelectedIndex != -1)
            {
                int index = channelsGrid.SelectedIndex;
                channelIndex = index;

                if (index != 0)
                {
                    ProgressDialogResult result = ProgressDialog.Execute(this, "Loading...", (bw, we) =>
                    {
                        Thread.Sleep(1000);
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            InsertItemsInChannelInterface(index);
                        }));
                    });
                }
                else
                {
                    if (this.IsLoaded)
                    {
                        ProgressDialogResult result = ProgressDialog.Execute(this, "Loading...", (bw, we) =>
                        {
                            Thread.Sleep(1000);
                            Application.Current.Dispatcher.Invoke((Action)(() =>
                            {
                                InsertItemsInChannelInterface(index);
                            }));
                        });
                    } else
                        InsertItemsInChannelInterface(index);
                }
            }
            else
            {
                if (channelIndex < channelsGrid.Items.Count)
                {
                    ProgressDialogResult result = ProgressDialog.Execute(this, "Loading...", (bw, we) =>
                    {
                        Thread.Sleep(1000);
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            InsertItemsInChannelInterface(channelIndex);
                           
                        }));
                    }); 
                }
                else
                {
                    channelIndex = channelsGrid.Items.Count - 1;
                    ProgressDialogResult result = ProgressDialog.Execute(this, "Loading...", (bw, we) =>
                    {
                        Thread.Sleep(1000);
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            InsertItemsInChannelInterface(channelIndex);
                        }));
                    });
                }
            }
        }

        #endregion


        #region clicked & hover events for options


        //this function recall when browse logo btn clicked 
        private void logoBrowse_Click(object sender, RoutedEventArgs e)
        {
            string path = channels[channelIndex].logoOptions.logoBrowse_Click();
            if (path.Length > 0)
            {
                logo_nameBox.Text = path;
                logo_save_btn.IsEnabled = true;
            }
        }


        //this function to clear browse logo textBox
        private void LogoBrowseClear_Click(object sender, RoutedEventArgs e)
        {
            logo_nameBox.Text = "";
            logo_save_btn.IsEnabled = true;
        }


        //this function recall when browse subtitle btn clicked
        private void subtitleBrowse_Click(object sender, RoutedEventArgs e)
        {
            string path = channels[channelIndex].subtitleOptions.subtitleBrowse_Click();
            if (path.Length > 0)
            {
                subtitle_nameBox.Text = path;
                subtitle_save_btn.IsEnabled = true;
            }
        }


        //this function to clear browse subtitle textBox
        private void SubtitleBrowseClear_Click(object sender, RoutedEventArgs e)
        {
            subtitle_nameBox.Text = "";
            subtitle_save_btn.IsEnabled = true;
        }


        //this function recall when clicked on browse button in schedule tab  and it puts all movies inside the directory into datagrid on right its call GetAllFilesFromDirectory() 
        private void DirectoryBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "Choose The Directory Of Your Movies";
            dlg.IsFolderPicker = true;
            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var directory = dlg.FileName;
                directoryNameBox.Text = directory;
                ProgressDialogResult result = ProgressDialog.Execute(this, "Loading Movies...", (bw, we) =>
                {

                    GetAllFilesFromDirectory(directory,true);

                });

                if (result.OperationFailed)
                    MessageBox.Show("failed!");
                directorySave_btn.IsEnabled = true;
            }
        }

        private void DirectoryAdvsBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "Choose The Directory Of Your Advs";
            dlg.IsFolderPicker = true;
            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var directory = dlg.FileName;
                directoryAdvsNameBox.Text = directory;
                ProgressDialogResult result = ProgressDialog.Execute(this, "Loading Advs...", (bw, we) =>
                {

                    GetAllFilesFromDirectory(directory, false);

                });

                if (result.OperationFailed)
                    MessageBox.Show("failed!");
                directorySave_btn.IsEnabled = true;
            }
        }


        //refresh button
        #region Calling refreshing button for directory


        private void DirectoryRefrech_Click(object sender, RoutedEventArgs e)
        {
            string directory = directoryNameBox.Text;
            if (Directory.Exists(directory))
            {
                ProgressDialogResult result = ProgressDialog.Execute(this, "Loading Movies...", (bw, we) =>
                {

                    GetAllFilesFromDirectory(directory,true);

                });

                if (result.OperationFailed)
                    MessageBox.Show("failed!");
            }
            else
            {
                MessageBox.Show("Directory not found");
            }
        }

        private void DirectoryRefrech_MouseEnter(object sender, RoutedEventArgs e)
        {
            refreshIcon.Opacity = 0.7;
        }

        private void DirectoryRefrech_MouseLeave(object sender, RoutedEventArgs e)
        {
            refreshIcon.Opacity = 1.0;
        }


        private void DirectoryAdvsRefrech_Click(object sender, RoutedEventArgs e)
        {
            string directory = directoryAdvsNameBox.Text;
            if (Directory.Exists(directory))
            {
                ProgressDialogResult result = ProgressDialog.Execute(this, "Loading Advs...", (bw, we) =>
                {

                    GetAllFilesFromDirectory(directory, false);

                });

                if (result.OperationFailed)
                    MessageBox.Show("failed!");
            }
            else
            {
                MessageBox.Show("Directory not found");
            }
        }

        private void DirectoryAdvsRefrech_MouseEnter(object sender, RoutedEventArgs e)
        {
            refreshAdvsIcon.Opacity = 0.7;
        }

        private void DirectoryAdvsRefrech_MouseLeave(object sender, RoutedEventArgs e)
        {
            refreshAdvsIcon.Opacity = 1.0;
        }

        #endregion


        //this function used to get All movies in this directory and put them in datagrid on right
        public void GetAllFilesFromDirectory(string directory, Boolean movies)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(directory);

            string[] extensions = new[] { ".mp4", ".mkv", ".webm", ".vob", ".mpg", ".m4v", ".svi", ".flv", ".mov", ".wmv", ".avi", ".asf", ".qt" };

            FileInfo[] info =
                dirInfo.EnumerateFiles()
                     .Where(f => extensions.Contains(f.Extension.ToLower()))
                     .ToArray();

            this.Dispatcher.Invoke((Action)(() =>
            {
                if (movies)
                {
                    allMovieGrid.Items.Clear();
                }
                else
                {
                    allAdvsGrid.Items.Clear();
                }
            }));
            Boolean errorFile = false;
            try
            {
                foreach (FileInfo f in info)
                {
                    var ffProbe = new FFProbe();
                    var videoInfo = ffProbe.GetMediaInfo(directory + "\\" + f.Name);
                    string answer = ZEROHOURS;
                    answer = string.Format("{0:D2}:{1:D2}:{2:D2}",
                       videoInfo.Duration.Hours,
                       videoInfo.Duration.Minutes,
                       videoInfo.Duration.Seconds
                       );
                    Movie movie = new Movie();
                    movie.Name = f.Name;
                    movie.Duration = answer;
                    movie.Directory = directory;

                    if (f.Name.Contains("&") || f.Name.Contains("%"))
                    {
                        errorFile = true;
                    }
                    else
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            if (movies)
                            {
                                allMovieGrid.Items.Add(movie);
                            }
                            else
                            {
                                allAdvsGrid.Items.Add(movie);
                            }
                        }));
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("UnKonwn error"); };

            if (errorFile)
            {
                MessageBox.Show("Some movies didn't load because they have ($ or %) on thier names rename them and then click refresh");
            }
        }


        //this function recall when save button clicked for channel options
        private void ChannelOptionsSave_Click(object sender, RoutedEventArgs e)
        {
            if (streamming_state_files.IsChecked == true)
            {
                channels[channelIndex].channelOptions.streamming_state = "files";
            }
            else
            {
                channels[channelIndex].channelOptions.streamming_state = "UDP";
            }
            channels[channelIndex].channelOptions.network = networkBox.Text;
            channels[channelIndex].channelOptions.port = portBox.Text;
            channels[channelIndex].channelOptions.networkUDP = networkUDPBox.Text;
            channels[channelIndex].channelOptions.portUDP = portUDPBox.Text;
            if (channel_StartDate.Text == null)
            {
                channels[channelIndex].channelOptions.startDate = "";
            }
            else
            {
                channels[channelIndex].channelOptions.startDate = channel_StartDate.Text;
            }
            if (channel_StartTime.Text == null)
            {
                channels[channelIndex].channelOptions.startTime = "";
            }
            else
            {
                channels[channelIndex].channelOptions.startTime = channel_StartTime.Text;
            }

            sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

            sqlite_conn.Open();

            sqlite_cmd = sqlite_conn.CreateCommand();

            sqlite_cmd.CommandText = "UPDATE Channels SET channel_name=\"" + channels[channelIndex].channelOptions.channel_name + "\", streamming_state=\"" + channels[channelIndex].channelOptions.streamming_state + "\", network= \"" + channels[channelIndex].channelOptions.network + "\", port=\"" + channels[channelIndex].channelOptions.port + "\", UDPnetwork= \"" + channels[channelIndex].channelOptions.networkUDP + "\", UDPport=\"" + channels[channelIndex].channelOptions.portUDP + "\", startDate=\"" + channels[channelIndex].channelOptions.startDate + "\", startTime=\"" + channels[channelIndex].channelOptions.startTime + "\" WHERE channel_id=" + channels[channelIndex].channel_id + ";";

            sqlite_cmd.ExecuteNonQuery();

            sqlite_conn.Close();

            channel_save_btn.IsEnabled = false;
        }


        //this function recall when save button clicked for logo options
        private void LogoOptionsSave_Click(object sender, RoutedEventArgs e)
        {
            if (logo_state_btn.IsChecked == true)
            {
                channels[channelIndex].logoOptions.logo_state = "on";
            }
            else
            {
                channels[channelIndex].logoOptions.logo_state = "off";
            }
            channels[channelIndex].logoOptions.logo_name = logo_nameBox.Text;
            channels[channelIndex].logoOptions.logo_X = logoX_Box.Text;
            channels[channelIndex].logoOptions.logo_Y = logoY_Box.Text;

            sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

            sqlite_conn.Open();

            sqlite_cmd = sqlite_conn.CreateCommand();

            sqlite_cmd.CommandText = "UPDATE Channels SET logo_state=\"" + channels[channelIndex].logoOptions.logo_state + "\", logo_name=\"" + channels[channelIndex].logoOptions.logo_name + "\", logo_X= \"" + channels[channelIndex].logoOptions.logo_X + "\", logo_Y=\"" + channels[channelIndex].logoOptions.logo_Y + "\" WHERE channel_id=" + channels[channelIndex].channel_id + ";";

            sqlite_cmd.ExecuteNonQuery();

            sqlite_conn.Close();

            logo_save_btn.IsEnabled = false;
        }


        //this function recall when save button clicked for subtitle options
        private void SubtitleOptionsSave_Click(object sender, RoutedEventArgs e)
        {
            if (subtitle_state_btn.IsChecked == true)
            {
                channels[channelIndex].subtitleOptions.subtitle_state = "on";
            }
            else
            {
                channels[channelIndex].subtitleOptions.subtitle_state = "off";
            }
            channels[channelIndex].subtitleOptions.subtitle_name = subtitle_nameBox.Text;
            channels[channelIndex].subtitleOptions.subtitle_X = subtitle_X_Box.Text;
            channels[channelIndex].subtitleOptions.subtitle_Y = subtitle_Y_Box.Text;
            channels[channelIndex].subtitleOptions.subtitle_appear_H = subtitle_appear_H.Text;
            channels[channelIndex].subtitleOptions.subtitle_appear_M = subtitle_appear_M.Text;
            channels[channelIndex].subtitleOptions.subtitle_disappear_M = subtitle_disappear_M.Text;
            channels[channelIndex].subtitleOptions.subtitle_disappear_S = subtitle_disappear_S.Text;
            if (subtitle_appearance_state_always.IsChecked == true)
            {
                channels[channelIndex].subtitleOptions.subtitle_appearance_state = "always";
            }
            else
            {
                channels[channelIndex].subtitleOptions.subtitle_appearance_state = "choosen";
            }

            sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

            sqlite_conn.Open();

            sqlite_cmd = sqlite_conn.CreateCommand();

            sqlite_cmd.CommandText = "UPDATE Channels SET subtitle_state=\"" + channels[channelIndex].subtitleOptions.subtitle_state + "\", subtitle_name=\"" + channels[channelIndex].subtitleOptions.subtitle_name + "\", subtitle_X= \"" + channels[channelIndex].subtitleOptions.subtitle_X + "\", subtitle_Y=\"" + channels[channelIndex].subtitleOptions.subtitle_Y + "\", subtitle_appearance_state=\"" + channels[channelIndex].subtitleOptions.subtitle_appearance_state + "\", subtitle_appear_H=\"" + channels[channelIndex].subtitleOptions.subtitle_appear_H + "\", subtitle_appear_M=\"" + channels[channelIndex].subtitleOptions.subtitle_appear_M + "\", subtitle_disappear_M=\"" + channels[channelIndex].subtitleOptions.subtitle_disappear_M + "\", subtitle_disappear_S=\"" + channels[channelIndex].subtitleOptions.subtitle_disappear_S + "\" WHERE channel_id=" + channels[channelIndex].channel_id + ";";

            sqlite_cmd.ExecuteNonQuery();

            sqlite_conn.Close();

            subtitle_save_btn.IsEnabled = false;

        }


        //this funcrion recall when clicked on save button in schedule tab to save directory in database
        private void DirectoryBrowseSave_Click(object sender, RoutedEventArgs e)
        {
            channels[channelIndex].channelSchedules.directory_name = directoryNameBox.Text;
            channels[channelIndex].channelSchedules.directoryAdvs_name = directoryAdvsNameBox.Text;

            sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

            sqlite_conn.Open();

            sqlite_cmd = sqlite_conn.CreateCommand();

            sqlite_cmd.CommandText = "UPDATE Channels SET directory_name=\"" + channels[channelIndex].channelSchedules.directory_name + "\", directoryAdvs_name=\"" + channels[channelIndex].channelSchedules.directoryAdvs_name + "\" WHERE channel_id=" + channels[channelIndex].channel_id + ";";

            sqlite_cmd.ExecuteNonQuery();

            sqlite_conn.Close();

            directorySave_btn.IsEnabled = false;
        }


        #endregion


        #region changed radio button events
        //this event recall when change streamming state in channel options
        private void StreammingState_Changed(object sender, RoutedEventArgs e)
        {
            if (streamming_state_files.IsChecked == true)
            {
                networkUDPBox.IsEnabled = false;
                portUDPBox.IsEnabled = false;
            }
            if (streamming_state_UDP.IsChecked == true)
            {
                networkUDPBox.IsEnabled = true;
                portUDPBox.IsEnabled = true;
            }
            channel_save_btn.IsEnabled = true;
        }


        //this event recall when change appearance state in subtitle options
        private void AppearnceState_Changed(object sender, RoutedEventArgs e)
        {
            if (subtitle_appearance_state_always.IsChecked == true)
            {
                subtitle_appear_H.IsEnabled = false;
                subtitle_appear_M.IsEnabled = false;
                subtitle_disappear_M.IsEnabled = false;
                subtitle_disappear_S.IsEnabled = false;
            }
            if (subtitle_appearance_state_choosen.IsChecked == true)
            {
                subtitle_appear_H.IsEnabled = true;
                subtitle_appear_M.IsEnabled = true;
                subtitle_disappear_M.IsEnabled = true;
                subtitle_disappear_S.IsEnabled = true;
            }
            subtitle_save_btn.IsEnabled = true;
        }

        #endregion


        #region channging states for channel & logo & subtitle events


        //this event recall when change channel state in channel options
        //this function calls StartStreammingUDP if streamming state is UDP otherwise i calls StartStreammingFiles
        private void ChannelState_Click(object sender, RoutedEventArgs e)
        {
            if (channel_state_btn.IsChecked == true)
            {
                channel_save_btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                string errorMsg = "Error In Channel : " + channels[channelIndex].channelOptions.channel_name + "\n";
                Boolean error = false;
                if (channels[channelIndex].channelOptions.network.Length == 0)
                {
                    error = true;
                    errorMsg += "Network is required \n";
                }
                if (channels[channelIndex].channelOptions.port.Length == 0)
                {
                    error = true;
                    errorMsg += "Port is required \n";
                }
                if (channels[channelIndex].channelOptions.streamming_state.Equals("UDP"))
                {
                    if (channels[channelIndex].channelOptions.networkUDP.Length == 0)
                    {
                        error = true;
                        errorMsg += "UDP Network is required \n";
                    }
                    if (channels[channelIndex].channelOptions.portUDP.Length == 0)
                    {
                        error = true;
                        errorMsg += "UDP Port is required \n";
                    }
                }
                if (moviesDay1[channelIndex].Count == 0 && channels[channelIndex].channelOptions.streamming_state.Equals("files"))
                {
                    error = true;
                    errorMsg += "You have to fill schedule of Day1 and save it \n";
                }
                if (error)
                {
                    channel_state_btn.IsChecked = false;
                    errorMsg += "Check that you save information \n";
                    MessageBox.Show(errorMsg);
                }
                else
                {
                    channels[channelIndex].channel_state = "on";
                    streamming_state_files.IsEnabled = false;
                    streamming_state_UDP.IsEnabled = false;
                    if (streamming_state_UDP.IsChecked == true)
                    {
                        Thread thread;
                        networkBox.IsEnabled = false;
                        portBox.IsEnabled = false;
                        networkUDPBox.IsEnabled = false;
                        portUDPBox.IsEnabled = false;
                        channels[channelIndex].channelExtraInfo.processKill = false;
                        thread = new Thread(() => StartStreammingUDP(channelIndex));
                        thread.Start();
                    }
                    else
                    {
                        Thread thread;
                        scheduleDay1Hour12.IsEnabled = false;
                        scheduleDay1Hour24.IsEnabled = false;
                        scheduleDay1Save_btn.IsEnabled = false;
                        channels[channelIndex].channelExtraInfo.processKill = false;
                        CalculateDateForAllMovies(channelIndex, DateTime.Now, DateTime.Now, 0);
                        thread = new Thread(() => StartStreammingFiles(channelIndex));
                        thread.Start();
                    }
                }
            }
            else
            {
                streamming_state_files.IsEnabled = true;
                streamming_state_UDP.IsEnabled = true;
                if (streamming_state_UDP.IsChecked == true)
                {
                    networkBox.IsEnabled = true;
                    portBox.IsEnabled = true;
                    networkUDPBox.IsEnabled = true;
                    portUDPBox.IsEnabled = true;
                }
                else
                {
                    scheduleDay1Hour12.IsEnabled = true;
                    scheduleDay1Hour24.IsEnabled = true;
                    scheduleDay1Save_btn.IsEnabled = true;
                    try
                    {
                        if (channels[channelIndex].channelExtraInfo.timer.IsEnabled)
                        {
                            channels[channelIndex].channelExtraInfo.timer.Stop();
                        }
                    }
                    catch (Exception ex) { };
                    nextMovieDuration.Content = "";
                }
                channels[channelIndex].channelExtraInfo.processKill = true;
                channels[channelIndex].channel_state = "off";
                try
                {
                    if (Process.GetProcesses().Any(x => x.Id == channels[channelIndex].channelExtraInfo.process.Id))
                    {
                        channels[channelIndex].channelExtraInfo.process.Kill();
                    }
                }
                catch (Exception ex) { };
            }
        }


        //this event recall when changing logo state in logo options
        private void LogoState_Click(object sender, RoutedEventArgs e)
        {
            logo_save_btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            if (logo_state_btn.IsChecked == true)
            {
                string errorMsg = "";
                Boolean error = false;
                if (logo_nameBox.Text.Length == 0)
                {
                    error = true;
                    errorMsg += "Logo file name is required \n";
                }
                else if (!File.Exists(logo_nameBox.Text))
                {
                    error = true;
                    errorMsg += "Logo file not found \n";
                }
                if (logoX_Box.Text.Length == 0)
                {
                    error = true;
                    errorMsg += "Logo X position is required \n";
                }
                if (logoY_Box.Text.Length == 0)
                {
                    error = true;
                    errorMsg += "Logo Y position is required \n";
                }
                if (error)
                {
                    logo_state_btn.IsChecked = false;
                    logo_save_btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    MessageBox.Show(errorMsg);
                }
                else
                {
                    logo_save_btn.IsEnabled = true;
                }
            }
            else
            {
                logo_save_btn.IsEnabled = true;
            }
            logo_save_btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }


        //this event recall when changing subtitle state in lsubtitle options
        private void SubtitleState_Click(object sender, RoutedEventArgs e)
        {
            subtitle_save_btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            if (subtitle_state_btn.IsChecked == true)
            {
                string errorMsg = "";
                Boolean error = false;
                if (subtitle_nameBox.Text.Length == 0)
                {
                    error = true;
                    errorMsg += "Subtitle file name is required \n";
                }
                else if (!File.Exists(subtitle_nameBox.Text))
                {
                    error = true;
                    errorMsg += "Subtitle file not found \n";
                }
                if (subtitle_X_Box.Text.Length == 0)
                {
                    error = true;
                    errorMsg += "Subtitle X position is required \n";
                }
                if (subtitle_Y_Box.Text.Length == 0)
                {
                    error = true;
                    errorMsg += "Subtitle Y position is required \n";
                }
                if (subtitle_appearance_state_choosen.IsChecked == true)
                {
                    if (subtitle_appear_H.Text.Length == 0 || subtitle_appear_M.Text.Length == 0)
                    {
                        error = true;
                        errorMsg += "Subtitle time to appear is required \n";
                    }
                    if (subtitle_disappear_M.Text.Length == 0 || subtitle_disappear_S.Text.Length == 0)
                    {
                        error = true;
                        errorMsg += "Subtitle time to disappear is required \n";
                    }
                    if (subtitle_disappear_M.Text.Length != 0 && subtitle_disappear_S.Text.Length != 0 && subtitle_appear_H.Text.Length != 0 && subtitle_appear_M.Text.Length != 0)
                    {
                        int subtitleAppearSeconds = int.Parse(subtitle_appear_H.Text) * 3600 + int.Parse(subtitle_appear_M.Text) * 60;
                        int subtitleDisappearSeconds = int.Parse(subtitle_disappear_M.Text) * 60 + int.Parse(subtitle_disappear_S.Text);
                        if (subtitleAppearSeconds - subtitleDisappearSeconds < 60)
                        {
                            error = true;
                            errorMsg += "You have to keep at least 1 minute after disappearing subtitle to show it again \n";
                        }
                    }
                    if (streamming_state_UDP.IsChecked == true)
                    {
                        error = true;
                        errorMsg += "You can't use chosen appearance state for subtitle with UDP streaming in channel state \n";
                    }
                }
                if (error)
                {
                    subtitle_state_btn.IsChecked = false;
                    MessageBox.Show(errorMsg);
                }
                else
                {
                    subtitle_save_btn.IsEnabled = true;

                }
            }
            else
            {
                subtitle_save_btn.IsEnabled = true;
            }
            subtitle_save_btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }


        #endregion


        #region movements and anomations
        //to make movement to channel options when click on its box
        private void ChannelOption_Click(object sender, RoutedEventArgs e)
        {
            if ((logoOptionList.Height == 200 || logoOptionList.Height == 34) && (subtitleOptionList.Height == 300 || subtitleOptionList.Height == 34))
            {
                if (channelOptionList.Height == 34)
                {
                    if (logoOptionList.Height == 200)
                    {
                        logoOptionList.BeginAnimation(Ellipse.HeightProperty, HideMenuLogo);
                    }
                    if (subtitleOptionList.Height == 300)
                    {
                        subtitleOptionList.BeginAnimation(Ellipse.HeightProperty, HideMenuChannelAndSubtitle);
                    }
                    channelOptionList.BeginAnimation(Ellipse.HeightProperty, ShowMenuChannelAndSubtitle);
                }
                else
                {
                    if (channelOptionList.Height == 300 & (logoOptionList.Height == 200 || subtitleOptionList.Height == 300))
                    {
                        channelOptionList.BeginAnimation(Ellipse.HeightProperty, HideMenuChannelAndSubtitle);
                    }

                }
            }
        }

        //to make movement to logo options when click on its box
        private void LogoOption_Click(object sender, RoutedEventArgs e)
        {
            if ((channelOptionList.Height == 300 || channelOptionList.Height == 34) && (subtitleOptionList.Height == 300 || subtitleOptionList.Height == 34))
            {
                if (logoOptionList.Height == 34)
                {
                    if (subtitleOptionList.Height == 300)
                    {
                        subtitleOptionList.BeginAnimation(Ellipse.HeightProperty, HideMenuChannelAndSubtitle);
                    }
                    if (channelOptionList.Height == 300)
                    {
                        channelOptionList.BeginAnimation(Ellipse.HeightProperty, HideMenuChannelAndSubtitle);
                    }
                    logoOptionList.BeginAnimation(Ellipse.HeightProperty, ShowMenuLogo);
                }
                else
                {
                    if (logoOptionList.Height == 200 & (channelOptionList.Height == 300 || subtitleOptionList.Height == 300))
                    {
                        logoOptionList.BeginAnimation(Ellipse.HeightProperty, HideMenuLogo);
                    }

                }
            }
        }

        //to make movement to subtitle options when click on its box
        private void SubtitleOption_Click(object sender, RoutedEventArgs e)
        {
            if ((channelOptionList.Height == 300 || channelOptionList.Height == 34) && (logoOptionList.Height == 200 || logoOptionList.Height == 34))
            {
                if (subtitleOptionList.Height == 34)
                {
                    if (logoOptionList.Height == 200)
                    {
                        logoOptionList.BeginAnimation(Ellipse.HeightProperty, HideMenuLogo);
                    }
                    if (channelOptionList.Height == 300)
                    {
                        channelOptionList.BeginAnimation(Ellipse.HeightProperty, HideMenuChannelAndSubtitle);
                    }
                    subtitleOptionList.BeginAnimation(Ellipse.HeightProperty, ShowMenuChannelAndSubtitle);
                }
                else
                {
                    if (subtitleOptionList.Height == 300 & (channelOptionList.Height == 300 || logoOptionList.Height == 200))
                    {
                        subtitleOptionList.BeginAnimation(Ellipse.HeightProperty, HideMenuChannelAndSubtitle); ;
                    }

                }
            }

        }

        //movements

        DoubleAnimation ShowMenuChannelAndSubtitle = new DoubleAnimation
        {
            From = 34,
            To = 300,
            Duration = TimeSpan.FromSeconds(0.7)
        };

        DoubleAnimation ShowMenuLogo = new DoubleAnimation
        {
            From = 34,
            To = 200,
            Duration = TimeSpan.FromSeconds(0.7)
        };

        DoubleAnimation HideMenuChannelAndSubtitle = new DoubleAnimation
        {
            From = 300,
            To = 34,
            Duration = TimeSpan.FromSeconds(0.7)
        };

        DoubleAnimation HideMenuLogo = new DoubleAnimation
        {
            From = 200,
            To = 34,
            Duration = TimeSpan.FromSeconds(0.7)
        };
        #endregion


        #region editing informations in datagrids and schedules


        #region moving movie from right datagrid event


        //this function called when click to arrow to move movie to another datagrid
        //this function call ChangeDigitalTimeDuration
        private void MoveMovieToDayGrid_Click(object sender, RoutedEventArgs e)
        {
            int index = allMovieGrid.SelectedIndex;
            if (index >= 0)
            {
                for (int i = 0; i < allMovieGrid.SelectedItems.Count; i++)
                { 
                    Movie movie = new Movie((Movie)allMovieGrid.SelectedItems[i]);
                    if (DaysTab.SelectedIndex == 0)
                    {
                        if (channel_state_btn.IsChecked == false)
                        {
                            Day1Grid.Items.Add(movie);
                            Boolean error = false;
                            string elapsedTime = (string)scheduleDay1ElapsedTime.Content;
                            string restTime = (string)scheduleDay1RestTime.Content;
                            string maxTime = TWELVEHOURS;
                            if (scheduleDay1Hour24.IsChecked == true)
                            {
                                maxTime = FORTEENHOURS;
                            }
                            ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, true, ref error);
                            scheduleDay1ElapsedTime.Content = elapsedTime;
                            scheduleDay1RestTime.Content = restTime;
                            if (error)
                            {
                                scheduleDay1OutTime.Visibility = Visibility.Visible;
                                scheduleDay1ElapsedTime.Foreground = Brushes.Red;
                                scheduleDay1RestTime.Foreground = Brushes.Red;
                            }
                            scheduleDay1Save_btn.IsEnabled = true;
                        }
                        else
                        {
                            MessageBox.Show("You can't change Day1 schedule because channel state is on");
                            break;
                        }
                    }
                    else if (DaysTab.SelectedIndex == 1)
                    {
                        Day2Grid.Items.Add(movie);
                        Boolean error = false;
                        string elapsedTime = (string)scheduleDay2ElapsedTime.Content;
                        string restTime = (string)scheduleDay2RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay2Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, true, ref error);
                        scheduleDay2ElapsedTime.Content = elapsedTime;
                        scheduleDay2RestTime.Content = restTime;
                        if (error)
                        {
                            scheduleDay2OutTime.Visibility = Visibility.Visible;
                            scheduleDay2ElapsedTime.Foreground = Brushes.Red;
                            scheduleDay2RestTime.Foreground = Brushes.Red;
                        }
                        scheduleDay2Save_btn.IsEnabled = true;
                    }
                    else if (DaysTab.SelectedIndex == 2)
                    {
                        Day3Grid.Items.Add(movie);
                        Boolean error = false;
                        string elapsedTime = (string)scheduleDay3ElapsedTime.Content;
                        string restTime = (string)scheduleDay3RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay3Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, true, ref error);
                        scheduleDay3ElapsedTime.Content = elapsedTime;
                        scheduleDay3RestTime.Content = restTime;
                        if (error)
                        {
                            scheduleDay3OutTime.Visibility = Visibility.Visible;
                            scheduleDay3ElapsedTime.Foreground = Brushes.Red;
                            scheduleDay3RestTime.Foreground = Brushes.Red;
                        }
                        scheduleDay3Save_btn.IsEnabled = true;
                    }
                    else if (DaysTab.SelectedIndex == 3)
                    {
                        Day4Grid.Items.Add(movie);
                        Boolean error = false;
                        string elapsedTime = (string)scheduleDay4ElapsedTime.Content;
                        string restTime = (string)scheduleDay4RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay4Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, true, ref error);
                        scheduleDay4ElapsedTime.Content = elapsedTime;
                        scheduleDay4RestTime.Content = restTime;
                        if (error)
                        {
                            scheduleDay4OutTime.Visibility = Visibility.Visible;
                            scheduleDay4ElapsedTime.Foreground = Brushes.Red;
                            scheduleDay4RestTime.Foreground = Brushes.Red;
                        }
                        scheduleDay4Save_btn.IsEnabled = true;
                    }
                    else if (DaysTab.SelectedIndex == 4)
                    {
                        Day5Grid.Items.Add(movie);
                        Boolean error = false;
                        string elapsedTime = (string)scheduleDay5ElapsedTime.Content;
                        string restTime = (string)scheduleDay5RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay5Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, true, ref error);
                        scheduleDay5ElapsedTime.Content = elapsedTime;
                        scheduleDay5RestTime.Content = restTime;
                        if (error)
                        {
                            scheduleDay5OutTime.Visibility = Visibility.Visible;
                            scheduleDay5ElapsedTime.Foreground = Brushes.Red;
                            scheduleDay5RestTime.Foreground = Brushes.Red;
                        }
                        scheduleDay5Save_btn.IsEnabled = true;
                    }
                    else if (DaysTab.SelectedIndex == 5)
                    {
                        Day6Grid.Items.Add(movie);
                        Boolean error = false;
                        string elapsedTime = (string)scheduleDay6ElapsedTime.Content;
                        string restTime = (string)scheduleDay6RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay6Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, true, ref error);
                        scheduleDay6ElapsedTime.Content = elapsedTime;
                        scheduleDay6RestTime.Content = restTime;
                        if (error)
                        {
                            scheduleDay6OutTime.Visibility = Visibility.Visible;
                            scheduleDay6ElapsedTime.Foreground = Brushes.Red;
                            scheduleDay6RestTime.Foreground = Brushes.Red;
                        }
                        scheduleDay6Save_btn.IsEnabled = true;
                    }
                    else if (DaysTab.SelectedIndex == 6)
                    {
                        Day7Grid.Items.Add(movie);
                        Boolean error = false;
                        string elapsedTime = (string)scheduleDay7ElapsedTime.Content;
                        string restTime = (string)scheduleDay7RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay7Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, true, ref error);
                        scheduleDay7ElapsedTime.Content = elapsedTime;
                        scheduleDay7RestTime.Content = restTime;
                        if (error)
                        {
                            scheduleDay7OutTime.Visibility = Visibility.Visible;
                            scheduleDay7ElapsedTime.Foreground = Brushes.Red;
                            scheduleDay7RestTime.Foreground = Brushes.Red;
                        }
                        scheduleDay7Save_btn.IsEnabled = true;
                    }
                    
                }
            }
            else
            {
                MessageBox.Show("Please select an item");
            }
            allMovieGrid.SelectedIndex = -1;
        }

        private void MoveAdvsToDayGrid_Click(object sender, RoutedEventArgs e)
        {
            int index = allAdvsGrid.SelectedIndex;
            if (index >= 0)
            {
                for (int i = 0; i < allAdvsGrid.SelectedItems.Count; i++)
                {
                    Movie movie = new Movie((Movie)allAdvsGrid.SelectedItems[i]);
                    if (DaysTab.SelectedIndex == 0)
                    {
                        if (channel_state_btn.IsChecked == false)
                        {
                            Day1Grid.Items.Add(movie);
                            Boolean error = false;
                            string elapsedTime = (string)scheduleDay1ElapsedTime.Content;
                            string restTime = (string)scheduleDay1RestTime.Content;
                            string maxTime = TWELVEHOURS;
                            if (scheduleDay1Hour24.IsChecked == true)
                            {
                                maxTime = FORTEENHOURS;
                            }
                            ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, true, ref error);
                            scheduleDay1ElapsedTime.Content = elapsedTime;
                            scheduleDay1RestTime.Content = restTime;
                            if (error)
                            {
                                scheduleDay1OutTime.Visibility = Visibility.Visible;
                                scheduleDay1ElapsedTime.Foreground = Brushes.Red;
                                scheduleDay1RestTime.Foreground = Brushes.Red;
                            }
                            scheduleDay1Save_btn.IsEnabled = true;
                        }
                        else
                        {
                            MessageBox.Show("You can't change Day1 schedule because channel state is on");
                            break;
                        }
                    }
                    else if (DaysTab.SelectedIndex == 1)
                    {
                        Day2Grid.Items.Add(movie);
                        Boolean error = false;
                        string elapsedTime = (string)scheduleDay2ElapsedTime.Content;
                        string restTime = (string)scheduleDay2RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay2Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, true, ref error);
                        scheduleDay2ElapsedTime.Content = elapsedTime;
                        scheduleDay2RestTime.Content = restTime;
                        if (error)
                        {
                            scheduleDay2OutTime.Visibility = Visibility.Visible;
                            scheduleDay2ElapsedTime.Foreground = Brushes.Red;
                            scheduleDay2RestTime.Foreground = Brushes.Red;
                        }
                        scheduleDay2Save_btn.IsEnabled = true;
                    }
                    else if (DaysTab.SelectedIndex == 2)
                    {
                        Day3Grid.Items.Add(movie);
                        Boolean error = false;
                        string elapsedTime = (string)scheduleDay3ElapsedTime.Content;
                        string restTime = (string)scheduleDay3RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay3Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, true, ref error);
                        scheduleDay3ElapsedTime.Content = elapsedTime;
                        scheduleDay3RestTime.Content = restTime;
                        if (error)
                        {
                            scheduleDay3OutTime.Visibility = Visibility.Visible;
                            scheduleDay3ElapsedTime.Foreground = Brushes.Red;
                            scheduleDay3RestTime.Foreground = Brushes.Red;
                        }
                        scheduleDay3Save_btn.IsEnabled = true;
                    }
                    else if (DaysTab.SelectedIndex == 3)
                    {
                        Day4Grid.Items.Add(movie);
                        Boolean error = false;
                        string elapsedTime = (string)scheduleDay4ElapsedTime.Content;
                        string restTime = (string)scheduleDay4RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay4Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, true, ref error);
                        scheduleDay4ElapsedTime.Content = elapsedTime;
                        scheduleDay4RestTime.Content = restTime;
                        if (error)
                        {
                            scheduleDay4OutTime.Visibility = Visibility.Visible;
                            scheduleDay4ElapsedTime.Foreground = Brushes.Red;
                            scheduleDay4RestTime.Foreground = Brushes.Red;
                        }
                        scheduleDay4Save_btn.IsEnabled = true;
                    }
                    else if (DaysTab.SelectedIndex == 4)
                    {
                        Day5Grid.Items.Add(movie);
                        Boolean error = false;
                        string elapsedTime = (string)scheduleDay5ElapsedTime.Content;
                        string restTime = (string)scheduleDay5RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay5Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, true, ref error);
                        scheduleDay5ElapsedTime.Content = elapsedTime;
                        scheduleDay5RestTime.Content = restTime;
                        if (error)
                        {
                            scheduleDay5OutTime.Visibility = Visibility.Visible;
                            scheduleDay5ElapsedTime.Foreground = Brushes.Red;
                            scheduleDay5RestTime.Foreground = Brushes.Red;
                        }
                        scheduleDay5Save_btn.IsEnabled = true;
                    }
                    else if (DaysTab.SelectedIndex == 5)
                    {
                        Day6Grid.Items.Add(movie);
                        Boolean error = false;
                        string elapsedTime = (string)scheduleDay6ElapsedTime.Content;
                        string restTime = (string)scheduleDay6RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay6Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, true, ref error);
                        scheduleDay6ElapsedTime.Content = elapsedTime;
                        scheduleDay6RestTime.Content = restTime;
                        if (error)
                        {
                            scheduleDay6OutTime.Visibility = Visibility.Visible;
                            scheduleDay6ElapsedTime.Foreground = Brushes.Red;
                            scheduleDay6RestTime.Foreground = Brushes.Red;
                        }
                        scheduleDay6Save_btn.IsEnabled = true;
                    }
                    else if (DaysTab.SelectedIndex == 6)
                    {
                        Day7Grid.Items.Add(movie);
                        Boolean error = false;
                        string elapsedTime = (string)scheduleDay7ElapsedTime.Content;
                        string restTime = (string)scheduleDay7RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay7Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, true, ref error);
                        scheduleDay7ElapsedTime.Content = elapsedTime;
                        scheduleDay7RestTime.Content = restTime;
                        if (error)
                        {
                            scheduleDay7OutTime.Visibility = Visibility.Visible;
                            scheduleDay7ElapsedTime.Foreground = Brushes.Red;
                            scheduleDay7RestTime.Foreground = Brushes.Red;
                        }
                        scheduleDay7Save_btn.IsEnabled = true;
                    }

                }
            }
            else
            {
                MessageBox.Show("Please select an item");
            }
            allAdvsGrid.SelectedIndex = -1;
        }

        private void MoveMovieToDayGrid_Enter(object sender, RoutedEventArgs e)
        {
            moveIcon.Opacity = 0.7;
        }

        private void MoveMovieToDayGrid_Leave(object sender, RoutedEventArgs e)
        {
            moveIcon.Opacity = 1.0;
        }

        private void MoveAdvsToDayGrid_Enter(object sender, RoutedEventArgs e)
        {
            moveAdvsIcon.Opacity = 0.7;
        }

        private void MoveAdvsToDayGrid_Leave(object sender, RoutedEventArgs e)
        {
            moveAdvsIcon.Opacity = 1.0;
        }


        #endregion


        #region delete or edit delay for datagrid on left


        private void DeleteRowGrid_Click(object sender, RoutedEventArgs e)
        {
            if (DaysTab.SelectedIndex == 0)
            {
                int index = Day1Grid.SelectedIndex;
                Movie movie = (Movie)Day1Grid.Items[index];
                if (channel_state_btn.IsChecked == false)
                {
                    Day1Grid.Items.Remove(movie);
                    Day1Grid.Items.Refresh();
                    Boolean error = true;
                    string elapsedTime = (string)scheduleDay1ElapsedTime.Content;
                    string restTime = (string)scheduleDay1RestTime.Content;
                    string maxTime = TWELVEHOURS;
                    if (scheduleDay1Hour24.IsChecked == true)
                    {
                        maxTime = FORTEENHOURS;
                    }
                    ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, false, ref error);
                    scheduleDay1ElapsedTime.Content = elapsedTime;
                    scheduleDay1RestTime.Content = restTime;
                    if (!error)
                    {
                        scheduleDay1OutTime.Visibility = Visibility.Hidden;
                        scheduleDay1ElapsedTime.Foreground = Brushes.Black;
                        scheduleDay1RestTime.Foreground = Brushes.Black;
                    }
                    scheduleDay1Save_btn.IsEnabled = true;
                }
            }
            else if (DaysTab.SelectedIndex == 1)
            {
                int index = Day2Grid.SelectedIndex;
                Movie movie = (Movie)Day2Grid.Items[index];
                Day2Grid.Items.Remove(movie);
                Day2Grid.Items.Refresh();
                Boolean error = true;
                string elapsedTime = (string)scheduleDay2ElapsedTime.Content;
                string restTime = (string)scheduleDay2RestTime.Content;
                string maxTime = TWELVEHOURS;
                if (scheduleDay2Hour24.IsChecked == true)
                {
                    maxTime = FORTEENHOURS;
                }
                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, false, ref error);
                scheduleDay2ElapsedTime.Content = elapsedTime;
                scheduleDay2RestTime.Content = restTime;
                if (!error)
                {
                    scheduleDay2OutTime.Visibility = Visibility.Hidden;
                    scheduleDay2ElapsedTime.Foreground = Brushes.Black;
                    scheduleDay2RestTime.Foreground = Brushes.Black;
                }
                scheduleDay2Save_btn.IsEnabled = true;
            }
            else if (DaysTab.SelectedIndex == 2)
            {
                int index = Day3Grid.SelectedIndex;
                Movie movie = (Movie)Day3Grid.Items[index];
                Day3Grid.Items.Remove(movie);
                Day3Grid.Items.Refresh();
                Boolean error = true;
                string elapsedTime = (string)scheduleDay3ElapsedTime.Content;
                string restTime = (string)scheduleDay3RestTime.Content;
                string maxTime = TWELVEHOURS;
                if (scheduleDay3Hour24.IsChecked == true)
                {
                    maxTime = FORTEENHOURS;
                }
                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, false, ref error);
                scheduleDay3ElapsedTime.Content = elapsedTime;
                scheduleDay3RestTime.Content = restTime;
                if (!error)
                {
                    scheduleDay3OutTime.Visibility = Visibility.Hidden;
                    scheduleDay3ElapsedTime.Foreground = Brushes.Black;
                    scheduleDay3RestTime.Foreground = Brushes.Black;
                }
                scheduleDay3Save_btn.IsEnabled = true;
            }
            else if (DaysTab.SelectedIndex == 3)
            {
                int index = Day4Grid.SelectedIndex;
                Movie movie = (Movie)Day4Grid.Items[index];
                Day4Grid.Items.Remove(movie);
                Day4Grid.Items.Refresh();
                Boolean error = true;
                string elapsedTime = (string)scheduleDay4ElapsedTime.Content;
                string restTime = (string)scheduleDay4RestTime.Content;
                string maxTime = TWELVEHOURS;
                if (scheduleDay4Hour24.IsChecked == true)
                {
                    maxTime = FORTEENHOURS;
                }
                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, false, ref error);
                scheduleDay4ElapsedTime.Content = elapsedTime;
                scheduleDay4RestTime.Content = restTime;
                if (!error)
                {
                    scheduleDay4OutTime.Visibility = Visibility.Hidden;
                    scheduleDay4ElapsedTime.Foreground = Brushes.Black;
                    scheduleDay4RestTime.Foreground = Brushes.Black;
                }
                scheduleDay4Save_btn.IsEnabled = true;
            }
            else if (DaysTab.SelectedIndex == 4)
            {
                int index = Day5Grid.SelectedIndex;
                Movie movie = (Movie)Day5Grid.Items[index];
                Day5Grid.Items.Remove(movie);
                Day5Grid.Items.Refresh();
                Boolean error = true;
                string elapsedTime = (string)scheduleDay5ElapsedTime.Content;
                string restTime = (string)scheduleDay5RestTime.Content;
                string maxTime = TWELVEHOURS;
                if (scheduleDay5Hour24.IsChecked == true)
                {
                    maxTime = FORTEENHOURS;
                }
                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, false, ref error);
                scheduleDay5ElapsedTime.Content = elapsedTime;
                scheduleDay5RestTime.Content = restTime;
                if (!error)
                {
                    scheduleDay5OutTime.Visibility = Visibility.Hidden;
                    scheduleDay5ElapsedTime.Foreground = Brushes.Black;
                    scheduleDay5RestTime.Foreground = Brushes.Black;
                }
                scheduleDay5Save_btn.IsEnabled = true;
            }
            else if (DaysTab.SelectedIndex == 5)
            {
                int index = Day6Grid.SelectedIndex;
                Movie movie = (Movie)Day6Grid.Items[index];
                Day6Grid.Items.Remove(movie);
                Day6Grid.Items.Refresh();
                Boolean error = true;
                string elapsedTime = (string)scheduleDay6ElapsedTime.Content;
                string restTime = (string)scheduleDay6RestTime.Content;
                string maxTime = TWELVEHOURS;
                if (scheduleDay6Hour24.IsChecked == true)
                {
                    maxTime = FORTEENHOURS;
                }
                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, false, ref error);
                scheduleDay6ElapsedTime.Content = elapsedTime;
                scheduleDay6RestTime.Content = restTime;
                if (!error)
                {
                    scheduleDay6OutTime.Visibility = Visibility.Hidden;
                    scheduleDay6ElapsedTime.Foreground = Brushes.Black;
                    scheduleDay6RestTime.Foreground = Brushes.Black;
                }
                scheduleDay6Save_btn.IsEnabled = true;
            }
            else if (DaysTab.SelectedIndex == 6)
            {
                int index = Day7Grid.SelectedIndex;
                Movie movie = (Movie)Day7Grid.Items[index];
                Day7Grid.Items.Remove(movie);
                Day7Grid.Items.Refresh();
                Boolean error = true;
                string elapsedTime = (string)scheduleDay7ElapsedTime.Content;
                string restTime = (string)scheduleDay7RestTime.Content;
                string maxTime = TWELVEHOURS;
                if (scheduleDay7Hour24.IsChecked == true)
                {
                    maxTime = FORTEENHOURS;
                }
                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, movie.Duration, movie.Delay, maxTime, false, ref error);
                scheduleDay7ElapsedTime.Content = elapsedTime;
                scheduleDay7RestTime.Content = restTime;
                if (!error)
                {
                    scheduleDay7OutTime.Visibility = Visibility.Hidden;
                    scheduleDay7ElapsedTime.Foreground = Brushes.Black;
                    scheduleDay7RestTime.Foreground = Brushes.Black;
                }
                scheduleDay7Save_btn.IsEnabled = true;
            }
        }

        private void EditDelayRowGrid_Click(object sender, RoutedEventArgs e)
        {
            if (DaysTab.SelectedIndex == 0)
            {
                if (channel_state_btn.IsChecked == false)
                {
                    string prevDelay, nextDelay;
                    int index = Day1Grid.SelectedIndex;
                    Movie movie = (Movie)Day1Grid.Items[index];
                    string delay = movie.Delay;
                    prevDelay = movie.Delay;
                    nextDelay = movie.Delay;
                    var dialog = new DelayDialog(delay);

                    if (dialog.ShowDialog() == true)
                    {
                        string time = dialog.Time;
                        delay = time;
                        nextDelay = time;
                    }
                    Day1Grid.Items.Remove(movie);
                    movie.Delay = delay;
                    Day1Grid.Items.Insert(index, movie);
                    int prevDelaySeconds = ConvertTimeToSeconds(prevDelay);
                    int nextDelaySeconds = ConvertTimeToSeconds(nextDelay);
                    if (prevDelaySeconds != nextDelaySeconds)
                    {
                        if (prevDelaySeconds > nextDelaySeconds)
                        {
                            string delayDuration = ConvertSecondsToTime(prevDelaySeconds - nextDelaySeconds);
                            Boolean error = true;
                            string elapsedTime = (string)scheduleDay1ElapsedTime.Content;
                            string restTime = (string)scheduleDay1RestTime.Content;
                            string maxTime = TWELVEHOURS;
                            if (scheduleDay1Hour24.IsChecked == true)
                            {
                                maxTime = FORTEENHOURS;
                            }
                            ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, delayDuration, ZEROHOURS, maxTime, false, ref error);
                            scheduleDay1ElapsedTime.Content = elapsedTime;
                            scheduleDay1RestTime.Content = restTime;
                            if (!error)
                            {
                                scheduleDay1OutTime.Visibility = Visibility.Hidden;
                                scheduleDay1ElapsedTime.Foreground = Brushes.Black;
                                scheduleDay1RestTime.Foreground = Brushes.Black;
                            }
                        }
                        else
                        {
                            string delayDuration = ConvertSecondsToTime(nextDelaySeconds - prevDelaySeconds);
                            Boolean error = false;
                            string elapsedTime = (string)scheduleDay1ElapsedTime.Content;
                            string restTime = (string)scheduleDay1RestTime.Content;
                            string maxTime = TWELVEHOURS;
                            if (scheduleDay1Hour24.IsChecked == true)
                            {
                                maxTime = FORTEENHOURS;
                            }
                            ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, delayDuration, ZEROHOURS, maxTime, true, ref error);
                            scheduleDay1ElapsedTime.Content = elapsedTime;
                            scheduleDay1RestTime.Content = restTime;
                            if (error)
                            {
                                scheduleDay1OutTime.Visibility = Visibility.Visible;
                                scheduleDay1ElapsedTime.Foreground = Brushes.Red;
                                scheduleDay1RestTime.Foreground = Brushes.Red;
                            }
                        }
                        scheduleDay1Save_btn.IsEnabled = true;
                    }

                }
            }
            else if (DaysTab.SelectedIndex == 1)
            {
                string prevDelay, nextDelay;
                int index = Day2Grid.SelectedIndex;
                Movie movie = (Movie)Day2Grid.Items[index];
                string delay = movie.Delay;
                prevDelay = movie.Delay;
                nextDelay = movie.Delay;
                var dialog = new DelayDialog(delay);

                if (dialog.ShowDialog() == true)
                {
                    string time = dialog.Time;
                    delay = time;
                    nextDelay = time;
                }
                Day2Grid.Items.Remove(movie);
                movie.Delay = delay;
                Day2Grid.Items.Insert(index, movie);
                int prevDelaySeconds = ConvertTimeToSeconds(prevDelay);
                int nextDelaySeconds = ConvertTimeToSeconds(nextDelay);
                if (prevDelaySeconds != nextDelaySeconds)
                {
                    if (prevDelaySeconds > nextDelaySeconds)
                    {
                        string delayDuration = ConvertSecondsToTime(prevDelaySeconds - nextDelaySeconds);
                        Boolean error = true;
                        string elapsedTime = (string)scheduleDay2ElapsedTime.Content;
                        string restTime = (string)scheduleDay2RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay2Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, delayDuration, ZEROHOURS, maxTime, false, ref error);
                        scheduleDay2ElapsedTime.Content = elapsedTime;
                        scheduleDay2RestTime.Content = restTime;
                        if (!error)
                        {
                            scheduleDay2OutTime.Visibility = Visibility.Hidden;
                            scheduleDay2ElapsedTime.Foreground = Brushes.Black;
                            scheduleDay2RestTime.Foreground = Brushes.Black;
                        }
                    }
                    else
                    {
                        string delayDuration = ConvertSecondsToTime(nextDelaySeconds - prevDelaySeconds);
                        Boolean error = false;
                        string elapsedTime = (string)scheduleDay2ElapsedTime.Content;
                        string restTime = (string)scheduleDay2RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay2Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, delayDuration, ZEROHOURS, maxTime, true, ref error);
                        scheduleDay2ElapsedTime.Content = elapsedTime;
                        scheduleDay2RestTime.Content = restTime;
                        if (error)
                        {
                            scheduleDay2OutTime.Visibility = Visibility.Visible;
                            scheduleDay2ElapsedTime.Foreground = Brushes.Red;
                            scheduleDay2RestTime.Foreground = Brushes.Red;
                        }
                    }
                }
                scheduleDay2Save_btn.IsEnabled = true;
            }
            else if (DaysTab.SelectedIndex == 2)
            {
                string prevDelay, nextDelay;
                int index = Day3Grid.SelectedIndex;
                Movie movie = (Movie)Day3Grid.Items[index];
                string delay = movie.Delay;
                prevDelay = movie.Delay;
                nextDelay = movie.Delay;
                var dialog = new DelayDialog(delay);

                if (dialog.ShowDialog() == true)
                {
                    string time = dialog.Time;
                    delay = time;
                    nextDelay = time;
                }
                Day3Grid.Items.Remove(movie);
                movie.Delay = delay;
                Day3Grid.Items.Insert(index, movie);
                int prevDelaySeconds = ConvertTimeToSeconds(prevDelay);
                int nextDelaySeconds = ConvertTimeToSeconds(nextDelay);
                if (prevDelaySeconds != nextDelaySeconds)
                {
                    if (prevDelaySeconds > nextDelaySeconds)
                    {
                        string delayDuration = ConvertSecondsToTime(prevDelaySeconds - nextDelaySeconds);
                        Boolean error = true;
                        string elapsedTime = (string)scheduleDay3ElapsedTime.Content;
                        string restTime = (string)scheduleDay3RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay3Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, delayDuration, ZEROHOURS, maxTime, false, ref error);
                        scheduleDay3ElapsedTime.Content = elapsedTime;
                        scheduleDay3RestTime.Content = restTime;
                        if (!error)
                        {
                            scheduleDay3OutTime.Visibility = Visibility.Hidden;
                            scheduleDay3ElapsedTime.Foreground = Brushes.Black;
                            scheduleDay3RestTime.Foreground = Brushes.Black;
                        }
                    }
                    else
                    {
                        string delayDuration = ConvertSecondsToTime(nextDelaySeconds - prevDelaySeconds);
                        Boolean error = false;
                        string elapsedTime = (string)scheduleDay3ElapsedTime.Content;
                        string restTime = (string)scheduleDay3RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay3Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, delayDuration, ZEROHOURS, maxTime, true, ref error);
                        scheduleDay3ElapsedTime.Content = elapsedTime;
                        scheduleDay3RestTime.Content = restTime;
                        if (error)
                        {
                            scheduleDay3OutTime.Visibility = Visibility.Visible;
                            scheduleDay3ElapsedTime.Foreground = Brushes.Red;
                            scheduleDay3RestTime.Foreground = Brushes.Red;
                        }
                    }
                }
                scheduleDay3Save_btn.IsEnabled = true;
            }
            else if (DaysTab.SelectedIndex == 3)
            {
                string prevDelay, nextDelay;
                int index = Day4Grid.SelectedIndex;
                Movie movie = (Movie)Day4Grid.Items[index];
                string delay = movie.Delay;
                prevDelay = movie.Delay;
                nextDelay = movie.Delay;
                var dialog = new DelayDialog(delay);

                if (dialog.ShowDialog() == true)
                {
                    string time = dialog.Time;
                    delay = time;
                    nextDelay = time;
                }
                Day4Grid.Items.Remove(movie);
                movie.Delay = delay;
                Day4Grid.Items.Insert(index, movie);
                int prevDelaySeconds = ConvertTimeToSeconds(prevDelay);
                int nextDelaySeconds = ConvertTimeToSeconds(nextDelay);
                if (prevDelaySeconds != nextDelaySeconds)
                {
                    if (prevDelaySeconds > nextDelaySeconds)
                    {
                        string delayDuration = ConvertSecondsToTime(prevDelaySeconds - nextDelaySeconds);
                        Boolean error = true;
                        string elapsedTime = (string)scheduleDay4ElapsedTime.Content;
                        string restTime = (string)scheduleDay4RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay4Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, delayDuration, ZEROHOURS, maxTime, false, ref error);
                        scheduleDay4ElapsedTime.Content = elapsedTime;
                        scheduleDay4RestTime.Content = restTime;
                        if (!error)
                        {
                            scheduleDay4OutTime.Visibility = Visibility.Hidden;
                            scheduleDay4ElapsedTime.Foreground = Brushes.Black;
                            scheduleDay4RestTime.Foreground = Brushes.Black;
                        }
                    }
                    else
                    {
                        string delayDuration = ConvertSecondsToTime(nextDelaySeconds - prevDelaySeconds);
                        Boolean error = false;
                        string elapsedTime = (string)scheduleDay4ElapsedTime.Content;
                        string restTime = (string)scheduleDay4RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay4Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, delayDuration, ZEROHOURS, maxTime, true, ref error);
                        scheduleDay4ElapsedTime.Content = elapsedTime;
                        scheduleDay4RestTime.Content = restTime;
                        if (error)
                        {
                            scheduleDay4OutTime.Visibility = Visibility.Visible;
                            scheduleDay4ElapsedTime.Foreground = Brushes.Red;
                            scheduleDay4RestTime.Foreground = Brushes.Red;
                        }
                    }
                }
                scheduleDay4Save_btn.IsEnabled = true;
            }
            else if (DaysTab.SelectedIndex == 4)
            {
                string prevDelay, nextDelay;
                int index = Day5Grid.SelectedIndex;
                Movie movie = (Movie)Day5Grid.Items[index];
                string delay = movie.Delay;
                prevDelay = movie.Delay;
                nextDelay = movie.Delay;
                var dialog = new DelayDialog(delay);

                if (dialog.ShowDialog() == true)
                {
                    string time = dialog.Time;
                    delay = time;
                    nextDelay = time;
                }
                Day5Grid.Items.Remove(movie);
                movie.Delay = delay;
                Day5Grid.Items.Insert(index, movie);
                int prevDelaySeconds = ConvertTimeToSeconds(prevDelay);
                int nextDelaySeconds = ConvertTimeToSeconds(nextDelay);
                if (prevDelaySeconds != nextDelaySeconds)
                {
                    if (prevDelaySeconds > nextDelaySeconds)
                    {
                        string delayDuration = ConvertSecondsToTime(prevDelaySeconds - nextDelaySeconds);
                        Boolean error = true;
                        string elapsedTime = (string)scheduleDay5ElapsedTime.Content;
                        string restTime = (string)scheduleDay5RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay5Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, delayDuration, ZEROHOURS, maxTime, false, ref error);
                        scheduleDay5ElapsedTime.Content = elapsedTime;
                        scheduleDay5RestTime.Content = restTime;
                        if (!error)
                        {
                            scheduleDay5OutTime.Visibility = Visibility.Hidden;
                            scheduleDay5ElapsedTime.Foreground = Brushes.Black;
                            scheduleDay5RestTime.Foreground = Brushes.Black;
                        }
                    }
                    else
                    {
                        string delayDuration = ConvertSecondsToTime(nextDelaySeconds - prevDelaySeconds);
                        Boolean error = false;
                        string elapsedTime = (string)scheduleDay5ElapsedTime.Content;
                        string restTime = (string)scheduleDay5RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay5Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, delayDuration, ZEROHOURS, maxTime, true, ref error);
                        scheduleDay5ElapsedTime.Content = elapsedTime;
                        scheduleDay5RestTime.Content = restTime;
                        if (error)
                        {
                            scheduleDay5OutTime.Visibility = Visibility.Visible;
                            scheduleDay5ElapsedTime.Foreground = Brushes.Red;
                            scheduleDay5RestTime.Foreground = Brushes.Red;
                        }
                    }
                }
                scheduleDay5Save_btn.IsEnabled = true;
            }
            else if (DaysTab.SelectedIndex == 5)
            {
                string prevDelay, nextDelay;
                int index = Day6Grid.SelectedIndex;
                Movie movie = (Movie)Day6Grid.Items[index];
                string delay = movie.Delay;
                prevDelay = movie.Delay;
                nextDelay = movie.Delay;
                var dialog = new DelayDialog(delay);

                if (dialog.ShowDialog() == true)
                {
                    string time = dialog.Time;
                    delay = time;
                    nextDelay = time;
                }
                Day6Grid.Items.Remove(movie);
                movie.Delay = delay;
                Day6Grid.Items.Insert(index, movie);
                int prevDelaySeconds = ConvertTimeToSeconds(prevDelay);
                int nextDelaySeconds = ConvertTimeToSeconds(nextDelay);
                if (prevDelaySeconds != nextDelaySeconds)
                {
                    if (prevDelaySeconds > nextDelaySeconds)
                    {
                        string delayDuration = ConvertSecondsToTime(prevDelaySeconds - nextDelaySeconds);
                        Boolean error = true;
                        string elapsedTime = (string)scheduleDay6ElapsedTime.Content;
                        string restTime = (string)scheduleDay6RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay6Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, delayDuration, ZEROHOURS, maxTime, false, ref error);
                        scheduleDay6ElapsedTime.Content = elapsedTime;
                        scheduleDay6RestTime.Content = restTime;
                        if (!error)
                        {
                            scheduleDay6OutTime.Visibility = Visibility.Hidden;
                            scheduleDay6ElapsedTime.Foreground = Brushes.Black;
                            scheduleDay6RestTime.Foreground = Brushes.Black;
                        }
                    }
                    else
                    {
                        string delayDuration = ConvertSecondsToTime(nextDelaySeconds - prevDelaySeconds);
                        Boolean error = false;
                        string elapsedTime = (string)scheduleDay6ElapsedTime.Content;
                        string restTime = (string)scheduleDay6RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay6Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, delayDuration, ZEROHOURS, maxTime, true, ref error);
                        scheduleDay6ElapsedTime.Content = elapsedTime;
                        scheduleDay6RestTime.Content = restTime;
                        if (error)
                        {
                            scheduleDay6OutTime.Visibility = Visibility.Visible;
                            scheduleDay6ElapsedTime.Foreground = Brushes.Red;
                            scheduleDay6RestTime.Foreground = Brushes.Red;
                        }
                    }
                }
                scheduleDay6Save_btn.IsEnabled = true;
            }
            else if (DaysTab.SelectedIndex == 6)
            {
                string prevDelay, nextDelay;
                int index = Day7Grid.SelectedIndex;
                Movie movie = (Movie)Day7Grid.Items[index];
                string delay = movie.Delay;
                prevDelay = movie.Delay;
                nextDelay = movie.Delay;
                var dialog = new DelayDialog(delay);

                if (dialog.ShowDialog() == true)
                {
                    string time = dialog.Time;
                    delay = time;
                    nextDelay = time;
                }
                Day7Grid.Items.Remove(movie);
                movie.Delay = delay;
                Day7Grid.Items.Insert(index, movie);
                int prevDelaySeconds = ConvertTimeToSeconds(prevDelay);
                int nextDelaySeconds = ConvertTimeToSeconds(nextDelay);
                if (prevDelaySeconds != nextDelaySeconds)
                {
                    if (prevDelaySeconds > nextDelaySeconds)
                    {
                        string delayDuration = ConvertSecondsToTime(prevDelaySeconds - nextDelaySeconds);
                        Boolean error = true;
                        string elapsedTime = (string)scheduleDay7ElapsedTime.Content;
                        string restTime = (string)scheduleDay7RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay7Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, delayDuration, ZEROHOURS, maxTime, false, ref error);
                        scheduleDay7ElapsedTime.Content = elapsedTime;
                        scheduleDay7RestTime.Content = restTime;
                        if (!error)
                        {
                            scheduleDay7OutTime.Visibility = Visibility.Hidden;
                            scheduleDay7ElapsedTime.Foreground = Brushes.Black;
                            scheduleDay7RestTime.Foreground = Brushes.Black;
                        }
                    }
                    else
                    {
                        string delayDuration = ConvertSecondsToTime(nextDelaySeconds - prevDelaySeconds);
                        Boolean error = false;
                        string elapsedTime = (string)scheduleDay7ElapsedTime.Content;
                        string restTime = (string)scheduleDay7RestTime.Content;
                        string maxTime = TWELVEHOURS;
                        if (scheduleDay7Hour24.IsChecked == true)
                        {
                            maxTime = FORTEENHOURS;
                        }
                        ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, delayDuration, ZEROHOURS, maxTime, true, ref error);
                        scheduleDay7ElapsedTime.Content = elapsedTime;
                        scheduleDay7RestTime.Content = restTime;
                        if (error)
                        {
                            scheduleDay7OutTime.Visibility = Visibility.Visible;
                            scheduleDay7ElapsedTime.Foreground = Brushes.Red;
                            scheduleDay7RestTime.Foreground = Brushes.Red;
                        }
                    }
                }
                scheduleDay7Save_btn.IsEnabled = true;
            }
        }


        #endregion


        #region changing radio button of format for hours events


        private void ScheduleDay1HoursFormat_Changed(object sender, RoutedEventArgs e)
        {
            if (scheduleDay1Hour12.IsChecked == true)
            {
                Boolean error = false;
                string elapsedTime = (string)scheduleDay1ElapsedTime.Content;
                string restTime = (string)scheduleDay1RestTime.Content;
                string maxTime = TWELVEHOURS;

                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, ZEROHOURS, ZEROHOURS, maxTime, true, ref error);
                scheduleDay1ElapsedTime.Content = elapsedTime;
                scheduleDay1RestTime.Content = restTime;
                if (error)
                {
                    scheduleDay1OutTime.Visibility = Visibility.Visible;
                    scheduleDay1ElapsedTime.Foreground = Brushes.Red;
                    scheduleDay1RestTime.Foreground = Brushes.Red;
                }
            }
            else
            {
                Boolean error = true;
                string elapsedTime = (string)scheduleDay1ElapsedTime.Content;
                string restTime = (string)scheduleDay1RestTime.Content;
                string maxTime = FORTEENHOURS;
                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, ZEROHOURS, ZEROHOURS, maxTime, false, ref error);
                scheduleDay1ElapsedTime.Content = elapsedTime;
                scheduleDay1RestTime.Content = restTime;
                if (!error)
                {
                    scheduleDay1OutTime.Visibility = Visibility.Hidden;
                    scheduleDay1ElapsedTime.Foreground = Brushes.Black;
                    scheduleDay1RestTime.Foreground = Brushes.Black;
                }
            }
            scheduleDay1Save_btn.IsEnabled = true;
        }

        private void ScheduleDay2HoursFormat_Changed(object sender, RoutedEventArgs e)
        {
            if (scheduleDay2Hour12.IsChecked == true)
            {
                Boolean error = false;
                string elapsedTime = (string)scheduleDay2ElapsedTime.Content;
                string restTime = (string)scheduleDay2RestTime.Content;
                string maxTime = TWELVEHOURS;

                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, ZEROHOURS, ZEROHOURS, maxTime, true, ref error);
                scheduleDay2ElapsedTime.Content = elapsedTime;
                scheduleDay2RestTime.Content = restTime;
                if (error)
                {
                    scheduleDay2OutTime.Visibility = Visibility.Visible;
                    scheduleDay2ElapsedTime.Foreground = Brushes.Red;
                    scheduleDay2RestTime.Foreground = Brushes.Red;
                }
            }
            else
            {
                Boolean error = true;
                string elapsedTime = (string)scheduleDay2ElapsedTime.Content;
                string restTime = (string)scheduleDay2RestTime.Content;
                string maxTime = FORTEENHOURS;
                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, ZEROHOURS, ZEROHOURS, maxTime, false, ref error);
                scheduleDay2ElapsedTime.Content = elapsedTime;
                scheduleDay2RestTime.Content = restTime;
                if (!error)
                {
                    scheduleDay2OutTime.Visibility = Visibility.Hidden;
                    scheduleDay2ElapsedTime.Foreground = Brushes.Black;
                    scheduleDay2RestTime.Foreground = Brushes.Black;
                }
            }
            scheduleDay2Save_btn.IsEnabled = true;
        }

        private void ScheduleDay3HoursFormat_Changed(object sender, RoutedEventArgs e)
        {
            if (scheduleDay3Hour12.IsChecked == true)
            {
                Boolean error = false;
                string elapsedTime = (string)scheduleDay3ElapsedTime.Content;
                string restTime = (string)scheduleDay3RestTime.Content;
                string maxTime = TWELVEHOURS;

                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, ZEROHOURS, ZEROHOURS, maxTime, true, ref error);
                scheduleDay3ElapsedTime.Content = elapsedTime;
                scheduleDay3RestTime.Content = restTime;
                if (error)
                {
                    scheduleDay3OutTime.Visibility = Visibility.Visible;
                    scheduleDay3ElapsedTime.Foreground = Brushes.Red;
                    scheduleDay3RestTime.Foreground = Brushes.Red;
                }
            }
            else
            {
                Boolean error = true;
                string elapsedTime = (string)scheduleDay3ElapsedTime.Content;
                string restTime = (string)scheduleDay3RestTime.Content;
                string maxTime = FORTEENHOURS;
                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, ZEROHOURS, ZEROHOURS, maxTime, false, ref error);
                scheduleDay3ElapsedTime.Content = elapsedTime;
                scheduleDay3RestTime.Content = restTime;
                if (!error)
                {
                    scheduleDay3OutTime.Visibility = Visibility.Hidden;
                    scheduleDay3ElapsedTime.Foreground = Brushes.Black;
                    scheduleDay3RestTime.Foreground = Brushes.Black;
                }
            }
            scheduleDay3Save_btn.IsEnabled = true;
        }

        private void ScheduleDay4HoursFormat_Changed(object sender, RoutedEventArgs e)
        {
            if (scheduleDay4Hour12.IsChecked == true)
            {
                Boolean error = false;
                string elapsedTime = (string)scheduleDay4ElapsedTime.Content;
                string restTime = (string)scheduleDay4RestTime.Content;
                string maxTime = TWELVEHOURS;

                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, ZEROHOURS, ZEROHOURS, maxTime, true, ref error);
                scheduleDay4ElapsedTime.Content = elapsedTime;
                scheduleDay4RestTime.Content = restTime;
                if (error)
                {
                    scheduleDay4OutTime.Visibility = Visibility.Visible;
                    scheduleDay4ElapsedTime.Foreground = Brushes.Red;
                    scheduleDay4RestTime.Foreground = Brushes.Red;
                }
            }
            else
            {
                Boolean error = true;
                string elapsedTime = (string)scheduleDay4ElapsedTime.Content;
                string restTime = (string)scheduleDay4RestTime.Content;
                string maxTime = FORTEENHOURS;
                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, ZEROHOURS, ZEROHOURS, maxTime, false, ref error);
                scheduleDay4ElapsedTime.Content = elapsedTime;
                scheduleDay4RestTime.Content = restTime;
                if (!error)
                {
                    scheduleDay4OutTime.Visibility = Visibility.Hidden;
                    scheduleDay4ElapsedTime.Foreground = Brushes.Black;
                    scheduleDay4RestTime.Foreground = Brushes.Black;
                }
            }
            scheduleDay4Save_btn.IsEnabled = true;
        }

        private void ScheduleDay5HoursFormat_Changed(object sender, RoutedEventArgs e)
        {
            if (scheduleDay5Hour12.IsChecked == true)
            {
                Boolean error = false;
                string elapsedTime = (string)scheduleDay5ElapsedTime.Content;
                string restTime = (string)scheduleDay5RestTime.Content;
                string maxTime = TWELVEHOURS;

                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, ZEROHOURS, ZEROHOURS, maxTime, true, ref error);
                scheduleDay5ElapsedTime.Content = elapsedTime;
                scheduleDay5RestTime.Content = restTime;
                if (error)
                {
                    scheduleDay5OutTime.Visibility = Visibility.Visible;
                    scheduleDay5ElapsedTime.Foreground = Brushes.Red;
                    scheduleDay5RestTime.Foreground = Brushes.Red;
                }
            }
            else
            {
                Boolean error = true;
                string elapsedTime = (string)scheduleDay5ElapsedTime.Content;
                string restTime = (string)scheduleDay5RestTime.Content;
                string maxTime = FORTEENHOURS;
                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, ZEROHOURS, ZEROHOURS, maxTime, false, ref error);
                scheduleDay5ElapsedTime.Content = elapsedTime;
                scheduleDay5RestTime.Content = restTime;
                if (!error)
                {
                    scheduleDay5OutTime.Visibility = Visibility.Hidden;
                    scheduleDay5ElapsedTime.Foreground = Brushes.Black;
                    scheduleDay5RestTime.Foreground = Brushes.Black;
                }
            }
            scheduleDay5Save_btn.IsEnabled = true;
        }

        private void ScheduleDay6HoursFormat_Changed(object sender, RoutedEventArgs e)
        {
            if (scheduleDay6Hour12.IsChecked == true)
            {
                Boolean error = false;
                string elapsedTime = (string)scheduleDay6ElapsedTime.Content;
                string restTime = (string)scheduleDay6RestTime.Content;
                string maxTime = TWELVEHOURS;

                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, ZEROHOURS, ZEROHOURS, maxTime, true, ref error);
                scheduleDay6ElapsedTime.Content = elapsedTime;
                scheduleDay6RestTime.Content = restTime;
                if (error)
                {
                    scheduleDay6OutTime.Visibility = Visibility.Visible;
                    scheduleDay6ElapsedTime.Foreground = Brushes.Red;
                    scheduleDay6RestTime.Foreground = Brushes.Red;
                }
            }
            else
            {
                Boolean error = true;
                string elapsedTime = (string)scheduleDay6ElapsedTime.Content;
                string restTime = (string)scheduleDay6RestTime.Content;
                string maxTime = FORTEENHOURS;
                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, ZEROHOURS, ZEROHOURS, maxTime, false, ref error);
                scheduleDay6ElapsedTime.Content = elapsedTime;
                scheduleDay6RestTime.Content = restTime;
                if (!error)
                {
                    scheduleDay6OutTime.Visibility = Visibility.Hidden;
                    scheduleDay6ElapsedTime.Foreground = Brushes.Black;
                    scheduleDay6RestTime.Foreground = Brushes.Black;
                }
            }
            scheduleDay6Save_btn.IsEnabled = true;
        }

        private void ScheduleDay7HoursFormat_Changed(object sender, RoutedEventArgs e)
        {
            if (scheduleDay7Hour12.IsChecked == true)
            {
                Boolean error = false;
                string elapsedTime = (string)scheduleDay7ElapsedTime.Content;
                string restTime = (string)scheduleDay7RestTime.Content;
                string maxTime = TWELVEHOURS;

                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, ZEROHOURS, ZEROHOURS, maxTime, true, ref error);
                scheduleDay7ElapsedTime.Content = elapsedTime;
                scheduleDay7RestTime.Content = restTime;
                if (error)
                {
                    scheduleDay7OutTime.Visibility = Visibility.Visible;
                    scheduleDay7ElapsedTime.Foreground = Brushes.Red;
                    scheduleDay7RestTime.Foreground = Brushes.Red;
                }
            }
            else
            {
                Boolean error = true;
                string elapsedTime = (string)scheduleDay7ElapsedTime.Content;
                string restTime = (string)scheduleDay7RestTime.Content;
                string maxTime = FORTEENHOURS;
                ChangeDigitalTimeDuration(ref elapsedTime, ref restTime, ZEROHOURS, ZEROHOURS, maxTime, false, ref error);
                scheduleDay7ElapsedTime.Content = elapsedTime;
                scheduleDay7RestTime.Content = restTime;
                if (!error)
                {
                    scheduleDay7OutTime.Visibility = Visibility.Hidden;
                    scheduleDay7ElapsedTime.Foreground = Brushes.Black;
                    scheduleDay7RestTime.Foreground = Brushes.Black;
                }
            }
            scheduleDay7Save_btn.IsEnabled = true;
        }


        #endregion


        #region cahnging degital Time functions


        public void ChangeDigitalTimeDuration(ref string elapsedTime, ref string restTime, string movieDuration, string movieDelay, string maxTime, Boolean adding, ref Boolean error)
        {
            int secondsMaxTime = ConvertTimeToSeconds(maxTime);
            int secondsMovie = ConvertTimeToSeconds(movieDuration) + ConvertTimeToSeconds(movieDelay);
            int secondElapsedTime = ConvertTimeToSeconds(elapsedTime);
            int secondRestTime = ConvertTimeToSeconds(restTime);
            if (adding)
            {
                elapsedTime = ConvertSecondsToTime(secondsMovie + secondElapsedTime);
                if ((secondsMovie + secondElapsedTime) > secondsMaxTime)
                {
                    error = true;
                    restTime = ConvertSecondsToTime(secondsMovie + secondElapsedTime - secondsMaxTime);
                }
                else
                {
                    restTime = ConvertSecondsToTime(secondsMaxTime - (secondsMovie + secondElapsedTime));
                }
            }
            else
            {
                elapsedTime = ConvertSecondsToTime(secondElapsedTime - secondsMovie);
                if (secondElapsedTime - secondsMovie > secondsMaxTime)
                {
                    restTime = ConvertSecondsToTime(secondElapsedTime - secondsMovie - secondsMaxTime);
                }
                else
                {
                    error = false;
                    restTime = ConvertSecondsToTime(secondsMaxTime - (secondElapsedTime - secondsMovie));
                }

            }
        }

        public int ConvertTimeToSeconds(string time)
        {
            int hour = int.Parse(time.Substring(0, 2));
            int minute = int.Parse(time.Substring(3, 2));
            int second = int.Parse(time.Substring(6, 2));

            return hour * 3600 + minute * 60 + second;
        }

        public string ConvertSecondsToTime(int seconds)
        {
            string time = "";
            int hours = seconds / 3600;
            if (hours < 10)
            {
                time += "0" + hours.ToString() + ":";
            }
            else
            {
                time += hours.ToString() + ":";
            }
            seconds = seconds % 3600;
            int minutes = seconds / 60;
            if (minutes < 10)
            {
                time += "0" + minutes.ToString() + ":";
            }
            else
            {
                time += minutes.ToString() + ":";
            }
            seconds = seconds % 60;
            if (seconds < 10)
            {
                time += "0" + seconds.ToString();
            }
            else
            {
                time += seconds.ToString();
            }

            return time;
        }


        #endregion


        #region schedules save buttons clicked events


        private void ScheduleOfDay1Save_Click(object sender, RoutedEventArgs e)
        {
            string restTime = (string)scheduleDay1RestTime.Content;
            int restTimeSeconds = ConvertTimeToSeconds(restTime);
            if (restTimeSeconds == 0)
            {
                moviesDay1[channelIndex].Clear();
                string formatOfDay1Movies = "";
                if (scheduleDay1Hour12.IsChecked == true)
                {
                    formatOfDay1Movies = "(12)";
                }
                else
                {
                    formatOfDay1Movies = "(24)";
                }
                // fromatting of schedule is : (fromat)name&duration&dealy%name&duration&dealy%name&duration&dealy.....
                for (int i = 0; i < Day1Grid.Items.Count; i++)
                {
                    if (i > 0)
                    {
                        formatOfDay1Movies += "%";
                    }
                    Movie movie = (Movie)Day1Grid.Items[i];
                    moviesDay1[channelIndex].Add(movie);
                    string subFormat = movie.Name + "&" + movie.Duration + "&" + movie.Delay + "&" + movie.Directory;
                    formatOfDay1Movies += subFormat;
                }
                channels[channelIndex].channelSchedules.schedule_day1 = formatOfDay1Movies;

                sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

                sqlite_conn.Open();

                sqlite_cmd = sqlite_conn.CreateCommand();

                sqlite_cmd.CommandText = "UPDATE Channels SET schedule_day1=\"" + channels[channelIndex].channelSchedules.schedule_day1 + "\" WHERE channel_id=" + channels[channelIndex].channel_id + ";";

                sqlite_cmd.ExecuteNonQuery();

                sqlite_conn.Close();

                scheduleDay1Save_btn.IsEnabled = false;
            }
            else if (Day1Grid.Items.Count == 0)
            {
                moviesDay1[channelIndex].Clear();
                string formatOfDay1Movies = "";
                if (scheduleDay1Hour12.IsChecked == true)
                {
                    formatOfDay1Movies = "(12)";
                }
                else
                {
                    formatOfDay1Movies = "(24)";
                }

                channels[channelIndex].channelSchedules.schedule_day1 = formatOfDay1Movies;

                sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

                sqlite_conn.Open();

                sqlite_cmd = sqlite_conn.CreateCommand();

                sqlite_cmd.CommandText = "UPDATE Channels SET schedule_day1=\"" + channels[channelIndex].channelSchedules.schedule_day1 + "\" WHERE channel_id=" + channels[channelIndex].channel_id + ";";

                sqlite_cmd.ExecuteNonQuery();

                sqlite_conn.Close();

                scheduleDay1Save_btn.IsEnabled = false;
            }
            else
            {
                if (scheduleDay1RestTime.Foreground == Brushes.Red)
                {
                    MessageBox.Show("You have to remove " + restTime + " from total time");
                }
                else
                {
                    MessageBox.Show("You have to add " + restTime + " to total time");
                }
            }
        }

        private void ScheduleOfDay2Save_Click(object sender, RoutedEventArgs e)
        {
            string restTime = (string)scheduleDay2RestTime.Content;
            int restTimeSeconds = ConvertTimeToSeconds(restTime);
            if (restTimeSeconds == 0)
            {
                moviesDay2[channelIndex].Clear();
                string formatOfDay2Movies = "";
                if (scheduleDay2Hour12.IsChecked == true)
                {
                    formatOfDay2Movies = "(12)";
                }
                else
                {
                    formatOfDay2Movies = "(24)";
                }
                // fromatting of schedule is : (fromat)name&duration&dealy%name&duration&dealy%name&duration&dealy.....
                for (int i = 0; i < Day2Grid.Items.Count; i++)
                {
                    if (i > 0)
                    {
                        formatOfDay2Movies += "%";
                    }
                    Movie movie = (Movie)Day2Grid.Items[i];
                    moviesDay2[channelIndex].Add(movie);
                    string subFormat = movie.Name + "&" + movie.Duration + "&" + movie.Delay + "&" + movie.Directory;
                    formatOfDay2Movies += subFormat;
                }
                channels[channelIndex].channelSchedules.schedule_day2 = formatOfDay2Movies;

                sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

                sqlite_conn.Open();

                sqlite_cmd = sqlite_conn.CreateCommand();

                sqlite_cmd.CommandText = "UPDATE Channels SET schedule_day2=\"" + channels[channelIndex].channelSchedules.schedule_day2 + "\" WHERE channel_id=" + channels[channelIndex].channel_id + ";";

                sqlite_cmd.ExecuteNonQuery();

                sqlite_conn.Close();

                scheduleDay2Save_btn.IsEnabled = false;
            }
            else if (Day2Grid.Items.Count == 0)
            {
                moviesDay2[channelIndex].Clear();
                string formatOfDay2Movies = "";
                if (scheduleDay2Hour12.IsChecked == true)
                {
                    formatOfDay2Movies = "(12)";
                }
                else
                {
                    formatOfDay2Movies = "(24)";
                }

                channels[channelIndex].channelSchedules.schedule_day2 = formatOfDay2Movies;

                sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

                sqlite_conn.Open();

                sqlite_cmd = sqlite_conn.CreateCommand();

                sqlite_cmd.CommandText = "UPDATE Channels SET schedule_day2=\"" + channels[channelIndex].channelSchedules.schedule_day2 + "\" WHERE channel_id=" + channels[channelIndex].channel_id + ";";

                sqlite_cmd.ExecuteNonQuery();

                sqlite_conn.Close();

                scheduleDay2Save_btn.IsEnabled = false;
            }
            else
            {
                if (scheduleDay2RestTime.Foreground == Brushes.Red)
                {
                    MessageBox.Show("You have to remove " + restTime + " from total time");
                }
                else
                {
                    MessageBox.Show("You have to add " + restTime + " to total time");
                }
            }
        }

        private void ScheduleOfDay3Save_Click(object sender, RoutedEventArgs e)
        {
            string restTime = (string)scheduleDay3RestTime.Content;
            int restTimeSeconds = ConvertTimeToSeconds(restTime);
            if (restTimeSeconds == 0)
            {
                moviesDay3[channelIndex].Clear();
                string formatOfDay3Movies = "";
                if (scheduleDay3Hour12.IsChecked == true)
                {
                    formatOfDay3Movies = "(12)";
                }
                else
                {
                    formatOfDay3Movies = "(24)";
                }
                // fromatting of schedule is : (fromat)name&duration&dealy%name&duration&dealy%name&duration&dealy.....
                for (int i = 0; i < Day3Grid.Items.Count; i++)
                {
                    if (i > 0)
                    {
                        formatOfDay3Movies += "%";
                    }
                    Movie movie = (Movie)Day3Grid.Items[i];
                    moviesDay3[channelIndex].Add(movie);
                    string subFormat = movie.Name + "&" + movie.Duration + "&" + movie.Delay + "&" + movie.Directory;
                    formatOfDay3Movies += subFormat;
                }
                channels[channelIndex].channelSchedules.schedule_day3 = formatOfDay3Movies;

                sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

                sqlite_conn.Open();

                sqlite_cmd = sqlite_conn.CreateCommand();

                sqlite_cmd.CommandText = "UPDATE Channels SET schedule_day3=\"" + channels[channelIndex].channelSchedules.schedule_day3 + "\" WHERE channel_id=" + channels[channelIndex].channel_id + ";";

                sqlite_cmd.ExecuteNonQuery();

                sqlite_conn.Close();

                scheduleDay3Save_btn.IsEnabled = false;
            }
            else if (Day3Grid.Items.Count == 0)
            {
                moviesDay3[channelIndex].Clear();
                string formatOfDay3Movies = "";
                if (scheduleDay3Hour12.IsChecked == true)
                {
                    formatOfDay3Movies = "(12)";
                }
                else
                {
                    formatOfDay3Movies = "(24)";
                }

                channels[channelIndex].channelSchedules.schedule_day3 = formatOfDay3Movies;

                sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

                sqlite_conn.Open();

                sqlite_cmd = sqlite_conn.CreateCommand();

                sqlite_cmd.CommandText = "UPDATE Channels SET schedule_day3=\"" + channels[channelIndex].channelSchedules.schedule_day3 + "\" WHERE channel_id=" + channels[channelIndex].channel_id + ";";

                sqlite_cmd.ExecuteNonQuery();

                sqlite_conn.Close();

                scheduleDay3Save_btn.IsEnabled = false;
            }
            else
            {
                if (scheduleDay3RestTime.Foreground == Brushes.Red)
                {
                    MessageBox.Show("You have to remove " + restTime + " from total time");
                }
                else
                {
                    MessageBox.Show("You have to add " + restTime + " to total time");
                }
            }
        }

        private void ScheduleOfDay4Save_Click(object sender, RoutedEventArgs e)
        {
            string restTime = (string)scheduleDay4RestTime.Content;
            int restTimeSeconds = ConvertTimeToSeconds(restTime);
            if (restTimeSeconds == 0)
            {
                moviesDay4[channelIndex].Clear();
                string formatOfDay4Movies = "";
                if (scheduleDay4Hour12.IsChecked == true)
                {
                    formatOfDay4Movies = "(12)";
                }
                else
                {
                    formatOfDay4Movies = "(24)";
                }
                // fromatting of schedule is : (fromat)name&duration&dealy%name&duration&dealy%name&duration&dealy.....
                for (int i = 0; i < Day4Grid.Items.Count; i++)
                {
                    if (i > 0)
                    {
                        formatOfDay4Movies += "%";
                    }
                    Movie movie = (Movie)Day4Grid.Items[i];
                    moviesDay4[channelIndex].Add(movie);
                    string subFormat = movie.Name + "&" + movie.Duration + "&" + movie.Delay + "&" + movie.Directory;
                    formatOfDay4Movies += subFormat;
                }
                channels[channelIndex].channelSchedules.schedule_day4 = formatOfDay4Movies;

                sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

                sqlite_conn.Open();

                sqlite_cmd = sqlite_conn.CreateCommand();

                sqlite_cmd.CommandText = "UPDATE Channels SET schedule_day4=\"" + channels[channelIndex].channelSchedules.schedule_day4 + "\" WHERE channel_id=" + channels[channelIndex].channel_id + ";";

                sqlite_cmd.ExecuteNonQuery();

                sqlite_conn.Close();

                scheduleDay4Save_btn.IsEnabled = false;
            }
            else if (Day4Grid.Items.Count == 0)
            {
                moviesDay4[channelIndex].Clear();
                string formatOfDay4Movies = "";
                if (scheduleDay4Hour12.IsChecked == true)
                {
                    formatOfDay4Movies = "(12)";
                }
                else
                {
                    formatOfDay4Movies = "(24)";
                }

                channels[channelIndex].channelSchedules.schedule_day4 = formatOfDay4Movies;

                sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

                sqlite_conn.Open();

                sqlite_cmd = sqlite_conn.CreateCommand();

                sqlite_cmd.CommandText = "UPDATE Channels SET schedule_day4=\"" + channels[channelIndex].channelSchedules.schedule_day4 + "\" WHERE channel_id=" + channels[channelIndex].channel_id + ";";

                sqlite_cmd.ExecuteNonQuery();

                sqlite_conn.Close();

                scheduleDay4Save_btn.IsEnabled = false;
            }
            else
            {
                if (scheduleDay4RestTime.Foreground == Brushes.Red)
                {
                    MessageBox.Show("You have to remove " + restTime + " from total time");
                }
                else
                {
                    MessageBox.Show("You have to add " + restTime + " to total time");
                }
            }
        }

        private void ScheduleOfDay5Save_Click(object sender, RoutedEventArgs e)
        {
            string restTime = (string)scheduleDay5RestTime.Content;
            int restTimeSeconds = ConvertTimeToSeconds(restTime);
            if (restTimeSeconds == 0)
            {
                moviesDay5[channelIndex].Clear();
                string formatOfDay5Movies = "";
                if (scheduleDay5Hour12.IsChecked == true)
                {
                    formatOfDay5Movies = "(12)";
                }
                else
                {
                    formatOfDay5Movies = "(24)";
                }
                // fromatting of schedule is : (fromat)name&duration&dealy%name&duration&dealy%name&duration&dealy.....
                for (int i = 0; i < Day5Grid.Items.Count; i++)
                {
                    if (i > 0)
                    {
                        formatOfDay5Movies += "%";
                    }
                    Movie movie = (Movie)Day5Grid.Items[i];
                    moviesDay5[channelIndex].Add(movie);
                    string subFormat = movie.Name + "&" + movie.Duration + "&" + movie.Delay + "&" + movie.Directory;
                    formatOfDay5Movies += subFormat;
                }
                channels[channelIndex].channelSchedules.schedule_day5 = formatOfDay5Movies;

                sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

                sqlite_conn.Open();

                sqlite_cmd = sqlite_conn.CreateCommand();

                sqlite_cmd.CommandText = "UPDATE Channels SET schedule_day5=\"" + channels[channelIndex].channelSchedules.schedule_day5 + "\" WHERE channel_id=" + channels[channelIndex].channel_id + ";";

                sqlite_cmd.ExecuteNonQuery();

                sqlite_conn.Close();

                scheduleDay5Save_btn.IsEnabled = false;
            }
            else if (Day5Grid.Items.Count == 0)
            {
                moviesDay5[channelIndex].Clear();
                string formatOfDay5Movies = "";
                if (scheduleDay5Hour12.IsChecked == true)
                {
                    formatOfDay5Movies = "(12)";
                }
                else
                {
                    formatOfDay5Movies = "(24)";
                }

                channels[channelIndex].channelSchedules.schedule_day5 = formatOfDay5Movies;

                sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

                sqlite_conn.Open();

                sqlite_cmd = sqlite_conn.CreateCommand();

                sqlite_cmd.CommandText = "UPDATE Channels SET schedule_day5=\"" + channels[channelIndex].channelSchedules.schedule_day5 + "\" WHERE channel_id=" + channels[channelIndex].channel_id + ";";

                sqlite_cmd.ExecuteNonQuery();

                sqlite_conn.Close();

                scheduleDay5Save_btn.IsEnabled = false;
            }
            else
            {
                if (scheduleDay5RestTime.Foreground == Brushes.Red)
                {
                    MessageBox.Show("You have to remove " + restTime + " from total time");
                }
                else
                {
                    MessageBox.Show("You have to add " + restTime + " to total time");
                }
            }
        }

        private void ScheduleOfDay6Save_Click(object sender, RoutedEventArgs e)
        {
            string restTime = (string)scheduleDay6RestTime.Content;
            int restTimeSeconds = ConvertTimeToSeconds(restTime);
            if (restTimeSeconds == 0)
            {
                moviesDay6[channelIndex].Clear();
                string formatOfDay6Movies = "";
                if (scheduleDay6Hour12.IsChecked == true)
                {
                    formatOfDay6Movies = "(12)";
                }
                else
                {
                    formatOfDay6Movies = "(24)";
                }
                // fromatting of schedule is : (fromat)name&duration&dealy%name&duration&dealy%name&duration&dealy.....
                for (int i = 0; i < Day6Grid.Items.Count; i++)
                {
                    if (i > 0)
                    {
                        formatOfDay6Movies += "%";
                    }
                    Movie movie = (Movie)Day6Grid.Items[i];
                    moviesDay6[channelIndex].Add(movie);
                    string subFormat = movie.Name + "&" + movie.Duration + "&" + movie.Delay + "&" + movie.Directory;
                    formatOfDay6Movies += subFormat;
                }
                channels[channelIndex].channelSchedules.schedule_day6 = formatOfDay6Movies;

                sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

                sqlite_conn.Open();

                sqlite_cmd = sqlite_conn.CreateCommand();

                sqlite_cmd.CommandText = "UPDATE Channels SET schedule_day6=\"" + channels[channelIndex].channelSchedules.schedule_day6 + "\" WHERE channel_id=" + channels[channelIndex].channel_id + ";";

                sqlite_cmd.ExecuteNonQuery();

                sqlite_conn.Close();

                scheduleDay6Save_btn.IsEnabled = false;
            }
            else if (Day6Grid.Items.Count == 0)
            {
                moviesDay6[channelIndex].Clear();
                string formatOfDay6Movies = "";
                if (scheduleDay6Hour12.IsChecked == true)
                {
                    formatOfDay6Movies = "(12)";
                }
                else
                {
                    formatOfDay6Movies = "(24)";
                }

                channels[channelIndex].channelSchedules.schedule_day6 = formatOfDay6Movies;

                sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

                sqlite_conn.Open();

                sqlite_cmd = sqlite_conn.CreateCommand();

                sqlite_cmd.CommandText = "UPDATE Channels SET schedule_day6=\"" + channels[channelIndex].channelSchedules.schedule_day6 + "\" WHERE channel_id=" + channels[channelIndex].channel_id + ";";

                sqlite_cmd.ExecuteNonQuery();

                sqlite_conn.Close();

                scheduleDay6Save_btn.IsEnabled = false;
            }
            else
            {
                if (scheduleDay6RestTime.Foreground == Brushes.Red)
                {
                    MessageBox.Show("You have to remove " + restTime + " from total time");
                }
                else
                {
                    MessageBox.Show("You have to add " + restTime + " to total time");
                }
            }
        }

        private void ScheduleOfDay7Save_Click(object sender, RoutedEventArgs e)
        {
            string restTime = (string)scheduleDay7RestTime.Content;
            int restTimeSeconds = ConvertTimeToSeconds(restTime);
            if (restTimeSeconds == 0)
            {
                moviesDay7[channelIndex].Clear();
                string formatOfDay7Movies = "";
                if (scheduleDay7Hour12.IsChecked == true)
                {
                    formatOfDay7Movies = "(12)";
                }
                else
                {
                    formatOfDay7Movies = "(24)";
                }
                // fromatting of schedule is : (fromat)name&duration&dealy%name&duration&dealy%name&duration&dealy.....
                for (int i = 0; i < Day7Grid.Items.Count; i++)
                {
                    if (i > 0)
                    {
                        formatOfDay7Movies += "%";
                    }
                    Movie movie = (Movie)Day7Grid.Items[i];
                    moviesDay7[channelIndex].Add(movie);
                    string subFormat = movie.Name + "&" + movie.Duration + "&" + movie.Delay + "&" + movie.Directory;
                    formatOfDay7Movies += subFormat;
                }
                channels[channelIndex].channelSchedules.schedule_day7 = formatOfDay7Movies;

                sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

                sqlite_conn.Open();

                sqlite_cmd = sqlite_conn.CreateCommand();

                sqlite_cmd.CommandText = "UPDATE Channels SET schedule_day7=\"" + channels[channelIndex].channelSchedules.schedule_day7 + "\" WHERE channel_id=" + channels[channelIndex].channel_id + ";";

                sqlite_cmd.ExecuteNonQuery();

                sqlite_conn.Close();

                scheduleDay7Save_btn.IsEnabled = false;
            }
            else if (Day6Grid.Items.Count == 0)
            {
                moviesDay7[channelIndex].Clear();
                string formatOfDay7Movies = "";
                if (scheduleDay7Hour12.IsChecked == true)
                {
                    formatOfDay7Movies = "(12)";
                }
                else
                {
                    formatOfDay7Movies = "(24)";
                }

                channels[channelIndex].channelSchedules.schedule_day7 = formatOfDay7Movies;

                sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

                sqlite_conn.Open();

                sqlite_cmd = sqlite_conn.CreateCommand();

                sqlite_cmd.CommandText = "UPDATE Channels SET schedule_day7=\"" + channels[channelIndex].channelSchedules.schedule_day7 + "\" WHERE channel_id=" + channels[channelIndex].channel_id + ";";

                sqlite_cmd.ExecuteNonQuery();

                sqlite_conn.Close();

                scheduleDay7Save_btn.IsEnabled = false;
            }
            else
            {
                if (scheduleDay7RestTime.Foreground == Brushes.Red)
                {
                    MessageBox.Show("You have to remove " + restTime + " from total time");
                }
                else
                {
                    MessageBox.Show("You have to add " + restTime + " to total time");
                }
            }
        }


        #endregion


        #endregion


        #region Drag and Drop Rows

        #region edit mode monitoring

        public bool IsEditing { get; set; }

        private void OnBeginEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            IsEditing = true;
            if (IsDragging) ResetDragDrop();
        }

        private void OnEndEdit(object sender, DataGridCellEditEndingEventArgs e)
        {
            IsEditing = false;
        }

        #endregion

        public bool IsDragging { get; set; }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsEditing) return;
            if (DaysTab.SelectedIndex == 0)
            {
                var row = UIHelpers.TryFindFromPoint<DataGridRow>((UIElement)sender, e.GetPosition(Day1Grid));
                if (row == null || row.IsEditing) return;
                IsDragging = true;
                DraggedItem = (Movie)row.Item;
                sourceIndex = row.GetIndex();
            }
            else if (DaysTab.SelectedIndex == 1)
            {
                var row = UIHelpers.TryFindFromPoint<DataGridRow>((UIElement)sender, e.GetPosition(Day2Grid));
                if (row == null || row.IsEditing) return;
                IsDragging = true;
                DraggedItem = (Movie)row.Item;
                sourceIndex = row.GetIndex();
            }
            else if (DaysTab.SelectedIndex == 2)
            {
                var row = UIHelpers.TryFindFromPoint<DataGridRow>((UIElement)sender, e.GetPosition(Day3Grid));
                if (row == null || row.IsEditing) return;
                IsDragging = true;
                DraggedItem = (Movie)row.Item;
                sourceIndex = row.GetIndex();
            }
            else if (DaysTab.SelectedIndex == 3)
            {
                var row = UIHelpers.TryFindFromPoint<DataGridRow>((UIElement)sender, e.GetPosition(Day4Grid));
                if (row == null || row.IsEditing) return;
                IsDragging = true;
                DraggedItem = (Movie)row.Item;
                sourceIndex = row.GetIndex();
            }
            else if (DaysTab.SelectedIndex == 4)
            {
                var row = UIHelpers.TryFindFromPoint<DataGridRow>((UIElement)sender, e.GetPosition(Day5Grid));
                if (row == null || row.IsEditing) return;
                IsDragging = true;
                DraggedItem = (Movie)row.Item;
                sourceIndex = row.GetIndex();
            }
            else if (DaysTab.SelectedIndex == 5)
            {
                var row = UIHelpers.TryFindFromPoint<DataGridRow>((UIElement)sender, e.GetPosition(Day6Grid));
                if (row == null || row.IsEditing) return;
                IsDragging = true;
                DraggedItem = (Movie)row.Item;
                sourceIndex = row.GetIndex();
            }
            else if (DaysTab.SelectedIndex == 6)
            {
                var row = UIHelpers.TryFindFromPoint<DataGridRow>((UIElement)sender, e.GetPosition(Day7Grid));
                if (row == null || row.IsEditing) return;
                IsDragging = true;
                DraggedItem = (Movie)row.Item;
                sourceIndex = row.GetIndex();
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsDragging || IsEditing)
            {
                return;
            }
            if (DaysTab.SelectedIndex == 0)
            {
                Movie targetItem = (Movie)Day1Grid.SelectedItem;
                if (targetItem == null || !ReferenceEquals(DraggedItem, targetItem))
                {
                    if (channel_state_btn.IsChecked == false)
                    {
                        var targetIndex = Day1Grid.Items.IndexOf(targetItem);
                        if (sourceIndex <= targetIndex)
                        {
                            Day1Grid.Items.Remove(DraggedItem);
                            Day1Grid.Items.Insert(targetIndex + 1, DraggedItem);
                            Day1Grid.SelectedItem = DraggedItem;
                            scheduleDay1Save_btn.IsEnabled = true;
                        }
                        else if (targetIndex >= 0)
                        {
                            Day1Grid.Items.Remove(DraggedItem);
                            Day1Grid.Items.Insert(targetIndex, DraggedItem);
                            Day1Grid.SelectedItem = DraggedItem;
                            scheduleDay1Save_btn.IsEnabled = true;
                        }


                    }
                    else
                    {
                        int firstIndex = Day1Grid.Items.IndexOf(DraggedItem);
                        int secondIndex = Day1Grid.Items.IndexOf(targetItem);
                        int allowedIndex = GetIndexOfRunningMovieInGridDay1();
                        if (firstIndex > allowedIndex && secondIndex > allowedIndex)
                        {
                            string nextmovieName = moviesDay1[channelIndex][allowedIndex + 1].Name;
                            Day1Grid.Items.Remove(DraggedItem);
                            var targetIndex = Day1Grid.Items.IndexOf(targetItem);
                            if (sourceIndex <= targetIndex)
                                Day1Grid.Items.Insert(targetIndex + 1, DraggedItem);
                            else if (targetIndex >= 0)
                                Day1Grid.Items.Insert(targetIndex, DraggedItem);
                            Day1Grid.SelectedItem = DraggedItem;

                            moviesDay1[channelIndex].Clear();
                            for (int i = 0; i < Day1Grid.Items.Count; i++)
                            {
                                Movie movie = (Movie)Day1Grid.Items[i];
                                moviesDay1[channelIndex].Add(movie);
                            }
                            if (!moviesDay1[channelIndex][allowedIndex + 1].Name.Equals(nextmovieName))
                            {
                                channels[channelIndex].channelExtraInfo.nextMovieName = moviesDay1[channelIndex][allowedIndex + 1].Name;
                                nextMovieName.Content = moviesDay1[channelIndex][allowedIndex + 1].Name;
                            }
                        }
                    }

                }
            }
            else if (DaysTab.SelectedIndex == 1)
            {
                Movie targetItem = (Movie)Day2Grid.SelectedItem;
                if (targetItem == null || !ReferenceEquals(DraggedItem, targetItem))
                {
                    var targetIndex = Day2Grid.Items.IndexOf(targetItem);
                    if (sourceIndex <= targetIndex)
                    {
                        Day2Grid.Items.Remove(DraggedItem);
                        Day2Grid.Items.Insert(targetIndex + 1, DraggedItem);
                        Day2Grid.SelectedItem = DraggedItem;
                        scheduleDay2Save_btn.IsEnabled = true;
                    }
                    else if (targetIndex >= 0)
                    {
                        Day2Grid.Items.Remove(DraggedItem);
                        Day2Grid.Items.Insert(targetIndex, DraggedItem);
                        Day2Grid.SelectedItem = DraggedItem;
                        scheduleDay2Save_btn.IsEnabled = true;
                    }

                }
            }
            else if (DaysTab.SelectedIndex == 2)
            {
                Movie targetItem = (Movie)Day3Grid.SelectedItem;
                if (targetItem == null || !ReferenceEquals(DraggedItem, targetItem))
                {
                    var targetIndex = Day3Grid.Items.IndexOf(targetItem);
                    if (sourceIndex <= targetIndex)
                    {
                        Day3Grid.Items.Remove(DraggedItem);
                        Day3Grid.Items.Insert(targetIndex + 1, DraggedItem);
                        Day3Grid.SelectedItem = DraggedItem;
                        scheduleDay3Save_btn.IsEnabled = true;
                    }
                    else if (targetIndex >= 0)
                    {
                        Day3Grid.Items.Remove(DraggedItem);
                        Day3Grid.Items.Insert(targetIndex, DraggedItem);
                        Day3Grid.SelectedItem = DraggedItem;
                        scheduleDay3Save_btn.IsEnabled = true;
                    }
                }
            }
            else if (DaysTab.SelectedIndex == 3)
            {
                Movie targetItem = (Movie)Day4Grid.SelectedItem;
                if (targetItem == null || !ReferenceEquals(DraggedItem, targetItem))
                {
                    var targetIndex = Day4Grid.Items.IndexOf(targetItem);
                    if (sourceIndex <= targetIndex)
                    {
                        Day4Grid.Items.Remove(DraggedItem);
                        Day4Grid.Items.Insert(targetIndex + 1, DraggedItem);
                        Day4Grid.SelectedItem = DraggedItem;
                        scheduleDay4Save_btn.IsEnabled = true;
                    }
                    else if (targetIndex >= 0)
                    {
                        Day4Grid.Items.Remove(DraggedItem);
                        Day4Grid.Items.Insert(targetIndex, DraggedItem);
                        Day4Grid.SelectedItem = DraggedItem;
                        scheduleDay4Save_btn.IsEnabled = true;
                    }
                }
            }
            else if (DaysTab.SelectedIndex == 4)
            {
                Movie targetItem = (Movie)Day5Grid.SelectedItem;
                if (targetItem == null || !ReferenceEquals(DraggedItem, targetItem))
                {
                    var targetIndex = Day5Grid.Items.IndexOf(targetItem);
                    if (sourceIndex <= targetIndex)
                    {
                        Day5Grid.Items.Remove(DraggedItem);
                        Day5Grid.Items.Insert(targetIndex + 1, DraggedItem);
                        Day5Grid.SelectedItem = DraggedItem;
                        scheduleDay5Save_btn.IsEnabled = true;
                    }
                    else if (targetIndex >= 0)
                    {
                        Day5Grid.Items.Remove(DraggedItem);
                        Day5Grid.Items.Insert(targetIndex, DraggedItem);
                        Day5Grid.SelectedItem = DraggedItem;
                        scheduleDay5Save_btn.IsEnabled = true;
                    }
                }
            }
            else if (DaysTab.SelectedIndex == 5)
            {
                Movie targetItem = (Movie)Day6Grid.SelectedItem;
                if (targetItem == null || !ReferenceEquals(DraggedItem, targetItem))
                {
                    var targetIndex = Day6Grid.Items.IndexOf(targetItem);
                    if (sourceIndex <= targetIndex)
                    {
                        Day6Grid.Items.Remove(DraggedItem);
                        Day6Grid.Items.Insert(targetIndex + 1, DraggedItem);
                        Day6Grid.SelectedItem = DraggedItem;
                        scheduleDay6Save_btn.IsEnabled = true;
                    }
                    else if (targetIndex >= 0)
                    {
                        Day6Grid.Items.Remove(DraggedItem);
                        Day6Grid.Items.Insert(targetIndex, DraggedItem);
                        Day6Grid.SelectedItem = DraggedItem;
                        scheduleDay6Save_btn.IsEnabled = true;
                    }
                }
            }
            else if (DaysTab.SelectedIndex == 6)
            {
                Movie targetItem = (Movie)Day7Grid.SelectedItem;
                if (targetItem == null || !ReferenceEquals(DraggedItem, targetItem))
                {
                    var targetIndex = Day7Grid.Items.IndexOf(targetItem);
                    if (sourceIndex <= targetIndex)
                    {
                        Day7Grid.Items.Remove(DraggedItem);
                        Day7Grid.Items.Insert(targetIndex + 1, DraggedItem);
                        Day7Grid.SelectedItem = DraggedItem;
                        scheduleDay7Save_btn.IsEnabled = true;
                    }
                    else if (targetIndex >= 0)
                    {
                        Day7Grid.Items.Remove(DraggedItem);
                        Day7Grid.Items.Insert(targetIndex, DraggedItem);
                        Day7Grid.SelectedItem = DraggedItem;
                        scheduleDay7Save_btn.IsEnabled = true;
                    }
                }
            }
            ResetDragDrop();
        }

        private void ResetDragDrop()
        {
            IsDragging = false;
            if (DaysTab.SelectedIndex == 0)
                Day1Grid.IsReadOnly = false;
            else if (DaysTab.SelectedIndex == 1)
                Day2Grid.IsReadOnly = false;
            else if (DaysTab.SelectedIndex == 2)
                Day3Grid.IsReadOnly = false;
            else if (DaysTab.SelectedIndex == 2)
                Day4Grid.IsReadOnly = false;
            else if (DaysTab.SelectedIndex == 2)
                Day5Grid.IsReadOnly = false;
            else if (DaysTab.SelectedIndex == 2)
                Day6Grid.IsReadOnly = false;
            else if (DaysTab.SelectedIndex == 2)
                Day7Grid.IsReadOnly = false;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsDragging || e.LeftButton != MouseButtonState.Pressed) return;
            if (DaysTab.SelectedIndex == 0)
            {
                Point position = e.GetPosition(Day1Grid);
                var row = UIHelpers.TryFindFromPoint<DataGridRow>(Day1Grid, position);
                if (row != null) Day1Grid.SelectedItem = row.Item;
            }
            else if (DaysTab.SelectedIndex == 1)
            {
                Point position = e.GetPosition(Day2Grid);
                var row = UIHelpers.TryFindFromPoint<DataGridRow>(Day2Grid, position);
                if (row != null) Day2Grid.SelectedItem = row.Item;
            }
            else if (DaysTab.SelectedIndex == 2)
            {
                Point position = e.GetPosition(Day3Grid);
                var row = UIHelpers.TryFindFromPoint<DataGridRow>(Day3Grid, position);
                if (row != null) Day3Grid.SelectedItem = row.Item;
            }
            else if (DaysTab.SelectedIndex == 3)
            {
                Point position = e.GetPosition(Day4Grid);
                var row = UIHelpers.TryFindFromPoint<DataGridRow>(Day4Grid, position);
                if (row != null) Day4Grid.SelectedItem = row.Item;
            }
            else if (DaysTab.SelectedIndex == 4)
            {
                Point position = e.GetPosition(Day5Grid);
                var row = UIHelpers.TryFindFromPoint<DataGridRow>(Day5Grid, position);
                if (row != null) Day5Grid.SelectedItem = row.Item;
            }
            else if (DaysTab.SelectedIndex == 5)
            {
                Point position = e.GetPosition(Day6Grid);
                var row = UIHelpers.TryFindFromPoint<DataGridRow>(Day6Grid, position);
                if (row != null) Day6Grid.SelectedItem = row.Item;
            }
            else if (DaysTab.SelectedIndex == 6)
            {
                Point position = e.GetPosition(Day7Grid);
                var row = UIHelpers.TryFindFromPoint<DataGridRow>(Day7Grid, position);
                if (row != null) Day7Grid.SelectedItem = row.Item;
            }

        }

        public int GetIndexOfRunningMovieInGridDay1()
        {
            for (int i = 0; i < Day1Grid.Items.Count; i++)
            {
                Movie movie = (Movie)Day1Grid.Items[i];
                if (movie.IsFinished.Equals("Icons/running_movie.png"))
                    return i;
            }
            return Day1Grid.Items.Count;
        }

        #endregion


        #region streamming functions


        public void StartStreammingFiles(int channelIdx)
        {
            //initialize streammingIndex and hours12 and indexOfMovies and ImageFor moviesDay1[streammingIndex]
            int streammingIndex = channelIdx;
            Boolean hours12 = false;

            this.Dispatcher.Invoke((Action)(() =>
            {
                string totalHoursDay1 = channels[streammingIndex].channelSchedules.schedule_day1.Substring(1, 2);
                if (totalHoursDay1.Equals("12"))
                {
                    hours12 = true;
                }
                else
                {
                    hours12 = false;
                }
                ChangeDataGridDay1AllImages(hours12, streammingIndex);
            }));

            while (true)
            {
                int movieSeconds = ConvertTimeToSeconds("24:00:00");
                SetListOfMovies(streammingIndex, hours12);
                //get ffmpeg command
                string command = "";
                this.Dispatcher.Invoke((Action)(() =>
                {
                    command = GetFFmpegInstroctionForFileStreammming(streammingIndex);
                }));
                //start process for channel          
                channels[streammingIndex].channelExtraInfo.process = new Process();
                channels[streammingIndex].channelExtraInfo.process.StartInfo.FileName = "cmd.exe";
                channels[streammingIndex].channelExtraInfo.process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                channels[streammingIndex].channelExtraInfo.process.StartInfo.Arguments = "/C libraries\\" + channels[streammingIndex].channelOptions.channel_name + ".exe";
                channels[streammingIndex].channelExtraInfo.process.Start();
                channels[streammingIndex].channelExtraInfo.process.StartInfo.FileName = "libraries\\" + channels[streammingIndex].channelOptions.channel_name + ".exe";
                channels[streammingIndex].channelExtraInfo.process.StartInfo.Arguments = command;
                channels[streammingIndex].channelExtraInfo.process.StartInfo.UseShellExecute = false;
                channels[streammingIndex].channelExtraInfo.process.StartInfo.CreateNoWindow = true;
                channels[streammingIndex].channelExtraInfo.process.Start();
                Stopwatch watch = new Stopwatch();
                watch.Start();
                //channels[streammingIndex].channelExtraInfo.process.WaitForExit((movieSeconds) * 1000 + 3000);
                channels[streammingIndex].channelExtraInfo.process.WaitForExit();
                try
                {
                    if (Process.GetProcesses().Any(x => x.Id == channels[streammingIndex].channelExtraInfo.process.Id))
                    {
                        channels[streammingIndex].channelExtraInfo.process.Kill();
                    }
                }
                catch (Exception ex) { };
                watch.Stop();
                /*
                if (movieSeconds - 120 > watch.Elapsed.TotalSeconds)
                {
                    if (!channels[streammingIndex].channelExtraInfo.processKill && !windwoClose)
                    {
                        MessageBox.Show("Error streamming happend in " + channels[streammingIndex].channelOptions.channel_name);
                    }
                    if (!windwoClose)
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            RestartGridInfo(streammingIndex);
                        }));
                    }
                    break;
                }*/
                // else 
                if (channels[streammingIndex].channel_state.Equals("off"))
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        RestartGridInfo(streammingIndex);

                    }));
                    break;
                }
                else
                {
                    if (channels[streammingIndex].channel_state.Equals("off"))
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            RestartGridInfo(streammingIndex);

                        }));
                        break;
                    }
                    /*
                    if (channels[streammingIndex].channelExtraInfo.timer.IsEnabled == true)
                    {
                        channels[streammingIndex].channelExtraInfo.timer.Stop();
                    }
                    */
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        ScheduleShiftting(streammingIndex);
                    }));
                    if (channels[streammingIndex].channelSchedules.schedule_day1.Substring(4).Length != 0)
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            string totalHoursDay1 = channels[streammingIndex].channelSchedules.schedule_day1.Substring(1, 2);
                            if (totalHoursDay1.Equals("12"))
                            {
                                hours12 = true;
                            }
                            else
                            {
                                hours12 = false;
                            }

                            ChangeDataGridDay1AllImages(hours12, streammingIndex);
                        }));
                    }
                    else
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            RestartGridInfo(streammingIndex);
                        }));
                        break;
                    }
                }

                #region test
                /*
                int indexOfMovie = 0;
                int numberOfMovies = numberOfMovies = moviesDay1[streammingIndex].Count;
                Boolean error = false;
                while (indexOfMovie < numberOfMovies)
                {
                    int movieSeconds = ConvertTimeToSeconds(moviesDay1[streammingIndex][indexOfMovie].Duration) + 3;
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        ChangeDataGridDay1Image(hours12, indexOfMovie, streammingIndex, true);
                        GetInfoForNameAndDurationForCurrentAndNextMovies(streammingIndex, hours12, indexOfMovie);
                        PutTimerToNextMovie(movieSeconds, streammingIndex);
                    }));
                    Stopwatch durationForMovie = new Stopwatch();
                    durationForMovie.Start();
                    while (durationForMovie.Elapsed.TotalSeconds < movieSeconds)
                    {
                        if (!Process.GetProcesses().Any(x => x.Id == channels[streammingIndex].channelExtraInfo.process.Id))
                        {
                            if (indexOfMovie + 1 < numberOfMovies)
                            {
                                error = true;
                                break;
                            }
                            else
                            {
                                if (channels[streammingIndex].channelExtraInfo.processKill)
                                {
                                    error = true;
                                    break;
                                }
                            }
                        }
                    }
                    durationForMovie.Stop();
                    if (channels[streammingIndex].channelExtraInfo.timer.IsEnabled == true)
                    {
                        channels[streammingIndex].channelExtraInfo.timer.Stop();
                    }
                    if (error)
                        break;
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        ChangeDataGridDay1Image(hours12, indexOfMovie, streammingIndex, false);
                    }));
                    indexOfMovie++;
                    if (numberOfMovies == indexOfMovie)
                    {
                        if (hours12)
                        {
                            hours12 = false;
                            indexOfMovie = 0;
                        }
                    }
                }
                */
                #endregion
                /*
                try
                {
                    if (Process.GetProcesses().Any(x => x.Id == channels[streammingIndex].channelExtraInfo.process.Id))
                    {
                        channels[streammingIndex].channelExtraInfo.process.Kill();
                    }
                }
                catch (Exception ex) { };
                if (error)
                {
                    if (!channels[streammingIndex].channelExtraInfo.processKill && !windwoClose)
                    {
                        MessageBox.Show("Error streamming happend in " + channels[streammingIndex].channelOptions.channel_name);
                    }
                    if (!windwoClose)
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            RestartGridInfo(streammingIndex);
                        }));
                    }
                    break;
                }
                else if (channels[streammingIndex].channel_state.Equals("off"))
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        RestartGridInfo(streammingIndex);

                    }));
                    break;
                }
                else
                {
                    if (channels[streammingIndex].channel_state.Equals("off"))
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            RestartGridInfo(streammingIndex);

                        }));
                        break;
                    }
                    if (channels[streammingIndex].channelExtraInfo.timer.IsEnabled == true)
                    {
                        channels[streammingIndex].channelExtraInfo.timer.Stop();
                    }
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        ScheduleShiftting(streammingIndex);
                    }));
                    if (channels[streammingIndex].channelSchedules.schedule_day1.Substring(4).Length != 0)
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            string totalHoursDay1 = channels[streammingIndex].channelSchedules.schedule_day1.Substring(1, 2);
                            if (totalHoursDay1.Equals("12"))
                            {
                                hours12 = true;
                            }
                            else
                            {
                                hours12 = false;
                            }

                            ChangeDataGridDay1AllImages(hours12, streammingIndex);
                        }));
                    }
                    else
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            RestartGridInfo(streammingIndex);
                        }));
                        break;
                    }
                }
                */
            }
        }

        public void SetListOfMovies(int streammingIndex, Boolean hour12)
        {
            string exeDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string targetPathDir = @exeDir + "\\channels_list";
            if (!Directory.Exists(targetPathDir))
            {
                Directory.CreateDirectory(targetPathDir);
            }
            string channelName = channels[streammingIndex].channelOptions.channel_name;
            string targetPathList = @exeDir + "\\channels_list\\" + channelName + ".txt";
            if (File.Exists(targetPathList))
            {
                try
                {
                    File.Delete(targetPathList);
                }
                catch (IOException ex) { }
            }
            //File.Create(targetPathList);
            string[] lines;
            if (hour12)
            {
                lines = new string[moviesDay1[streammingIndex].Count * 2];
            }
            else
            {
                lines = new string[moviesDay1[streammingIndex].Count];
            }
            for (int i = 0; i < moviesDay1[streammingIndex].Count; i++)
            {
                string directory = moviesDay1[streammingIndex][i].Directory;
                string movieName = moviesDay1[streammingIndex][i].Name;
                directory = directory.Replace("'", "'\\''");
                directory = directory.Replace("\\", "\\\\");
                movieName = movieName.Replace("\\", "\\\\");
                movieName = movieName.Replace("'", "'\\''");
                string line = "file '" + directory + "\\\\" + movieName + "'";
                lines[i] = line;
            }
            if (hour12)
            {
                int n = moviesDay1[streammingIndex].Count;
                for (int i = 0; i < moviesDay1[streammingIndex].Count; i++)
                {
                    string directory = moviesDay1[streammingIndex][i].Directory;
                    string movieName = moviesDay1[streammingIndex][i].Name;
                    directory = directory.Replace("'", "'\\''");
                    directory = directory.Replace("\\", "\\\\");
                    movieName = movieName.Replace("\\", "\\\\");
                    movieName = movieName.Replace("'", "'\\''");
                    string line = "file '" + directory + "\\\\" + movieName + "'";
                    lines[i + n] = line;
                }
            }
            File.WriteAllLines(targetPathList, lines);
        }


        public void ChangeDataGridDay1AllImages(Boolean hour12, int streammingIndex)
        {
            for (int i = 0; i < moviesDay1[streammingIndex].Count; i++)
            {
                if (hour12)
                {
                    moviesDay1[streammingIndex][i].IsFinished = "Icons/wait12.png";
                }
                else
                {
                    moviesDay1[streammingIndex][i].IsFinished = "Icons/wait24.png";
                }
            }
            if (channelIndex == streammingIndex)
            {
                FillDataGridWithMovies(moviesDay1[streammingIndex], 1);
            }
        }


        public void FillDataGridWithMovies(List<Movie> movies, int datagridIndex)
        {
            if (datagridIndex == 1)
            {
                Day1Grid.Items.Clear();
                for (int i = 0; i < movies.Count; i++)
                {
                    Day1Grid.Items.Add(movies[i]);
                }
            }
            else if (datagridIndex == 2)
            {
                Day2Grid.Items.Clear();
                for (int i = 0; i < movies.Count; i++)
                {
                    Day2Grid.Items.Add(movies[i]);
                }
            }
            else if (datagridIndex == 3)
            {
                Day3Grid.Items.Clear();
                for (int i = 0; i < movies.Count; i++)
                {
                    Day3Grid.Items.Add(movies[i]);
                }
            }
            else if (datagridIndex == 4)
            {
                Day4Grid.Items.Clear();
                for (int i = 0; i < movies.Count; i++)
                {
                    Day4Grid.Items.Add(movies[i]);
                }
            }
            else if (datagridIndex == 5)
            {
                Day5Grid.Items.Clear();
                for (int i = 0; i < movies.Count; i++)
                {
                    Day5Grid.Items.Add(movies[i]);
                }
            }
            else if (datagridIndex == 6)
            {
                Day6Grid.Items.Clear();
                for (int i = 0; i < movies.Count; i++)
                {
                    Day6Grid.Items.Add(movies[i]);
                }
            }
            else if (datagridIndex == 7)
            {
                Day7Grid.Items.Clear();
                for (int i = 0; i < movies.Count; i++)
                {
                    Day7Grid.Items.Add(movies[i]);
                }
            }
        }


        public string GetFFmpegInstroctionForFileStreammming(int streammingIndex)
        {

            int movieSeconds = ConvertTimeToSeconds("24:00:00");
            string channelName = channels[streammingIndex].channelOptions.channel_name;
            string exeDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string targetPathList = @exeDir + "\\channels_list\\" + channelName + ".txt";
            Boolean logoState = false, subtitleState = false, subtitleAppearaceAlways = false;
            if (!channels[streammingIndex].logoOptions.logo_state.Equals("off"))
            {
                logoState = true;
            }
            if (!channels[streammingIndex].subtitleOptions.subtitle_state.Equals("off"))
            {
                subtitleState = true;
                if (channels[streammingIndex].subtitleOptions.subtitle_appearance_state.Equals("always"))
                {
                    subtitleAppearaceAlways = true;
                }
            }
            string command = "-re -f concat -safe 0 ";
            string filter_complex = "";
            if (logoState == false)
            {
                if (subtitleState == false)
                {
                    string moviePath = "-i " + "\"" + targetPathList + "\"";
                    command += moviePath;
                }
                else
                {
                    if (subtitleAppearaceAlways == true)
                    {
                        string moviePath = "-i " + "\"" + targetPathList + "\" -ignore_loop 0 -i " + "\"" + channels[streammingIndex].subtitleOptions.subtitle_name + "\" ";
                        command += moviePath;
                        filter_complex = "-filter_complex \"[0][1]overlay=" + channels[streammingIndex].subtitleOptions.subtitle_X + ":" + channels[streammingIndex].subtitleOptions.subtitle_Y + "\"";
                        //command += filter_complex;
                    }
                    else
                    {
                        string moviePath = "-i " + "\"" + targetPathList + "\" -ignore_loop 0 -i " + "\"" + channels[streammingIndex].subtitleOptions.subtitle_name + "\" ";
                        int subtitleAppearSeconds = int.Parse(channels[streammingIndex].subtitleOptions.subtitle_appear_H) * 3600 + int.Parse(channels[streammingIndex].subtitleOptions.subtitle_appear_M) * 60;
                        int subtitleDisappearSeconds = int.Parse(channels[streammingIndex].subtitleOptions.subtitle_disappear_M) * 60 + int.Parse(channels[streammingIndex].subtitleOptions.subtitle_disappear_S);
                        filter_complex = GetAppearAndDisappearSubtitleCommand(streammingIndex, subtitleAppearSeconds, subtitleDisappearSeconds, movieSeconds, false);
                        if (filter_complex.Length == 0)
                        {
                            moviePath = "-i " + "\"" + targetPathList + "\"";
                            command += moviePath;
                        }
                        else
                        {
                            command += moviePath;
                            // command += filter_complex;
                        }
                    }
                }
            }
            else
            {
                if (subtitleState == false)
                {
                    string moviePath = "-i " + "\"" + targetPathList + "\" -i " + "\"" + channels[streammingIndex].logoOptions.logo_name + "\" ";
                    command += moviePath;
                    filter_complex = "-filter_complex \"[0][1]overlay=" + channels[streammingIndex].logoOptions.logo_X + ":" + channels[streammingIndex].logoOptions.logo_Y + "\"";
                    //command += filter_complex;
                }
                else
                {
                    if (subtitleAppearaceAlways == true)
                    {
                        string moviePath = "-i " + "\"" + targetPathList + "\" -i " + "\"" + channels[streammingIndex].logoOptions.logo_name + "\" " + " -ignore_loop 0 -i " + "\"" + channels[streammingIndex].subtitleOptions.subtitle_name + "\" ";
                        command += moviePath;
                        filter_complex = "-filter_complex \"[0][1]overlay=" + channels[streammingIndex].logoOptions.logo_X + ":" + channels[streammingIndex].logoOptions.logo_Y + "[a];[a][2]overlay=" + channels[streammingIndex].subtitleOptions.subtitle_X + ":" + channels[streammingIndex].subtitleOptions.subtitle_Y + "\"";
                        //command += filter_complex;
                    }
                    else
                    {
                        string moviePath = "-i " + "\"" + targetPathList + "\" -i " + "\"" + channels[streammingIndex].logoOptions.logo_name + "\" " + " -ignore_loop 0 -i " + "\"" + channels[streammingIndex].subtitleOptions.subtitle_name + "\" ";
                        int subtitleAppearSeconds = int.Parse(channels[streammingIndex].subtitleOptions.subtitle_appear_H) * 3600 + int.Parse(channels[streammingIndex].subtitleOptions.subtitle_appear_M) * 60;
                        int subtitleDisappearSeconds = int.Parse(channels[streammingIndex].subtitleOptions.subtitle_disappear_M) * 60 + int.Parse(channels[streammingIndex].subtitleOptions.subtitle_disappear_S);
                        filter_complex = GetAppearAndDisappearSubtitleCommand(streammingIndex, subtitleAppearSeconds, subtitleDisappearSeconds, movieSeconds, true);
                        if (filter_complex.Length == 0)
                        {
                            moviePath = "-i " + "\"" + targetPathList + "\" -i " + "\"" + channels[streammingIndex].logoOptions.logo_name + "\" ";
                            command += moviePath;
                            filter_complex = "-filter_complex \"[0][1]overlay=" + channels[streammingIndex].logoOptions.logo_X + ":" + channels[streammingIndex].logoOptions.logo_Y + "\"";
                            // command += filter_complex;
                        }
                        else
                        {
                            command += moviePath;
                            // command += filter_complex;
                        }
                    }
                }
            }

            command += " -vcodec libx264  -vb 2000000 -g 60 -vprofile main -acodec aac -ab 128000 -ar 48000 -ac 2 -async 1 -vbsf h264_mp4toannexb -strict experimental ";

            command += filter_complex;

            command += " -async 1 -pass 1 -f mpegts udp://" + channels[streammingIndex].channelOptions.network + ":" + channels[streammingIndex].channelOptions.port + "?pkt_size=1316";

            return command;
        }


        public string GetAppearAndDisappearSubtitleCommand(int streammingIndex, int subtitleAppearSeconds, int subtitleDisappearSeconds, int movieDuration, Boolean logoState)
        {
            if (!logoState)
            {
                string subtitleCommands = "";
                int totalSubtitleDuration = subtitleAppearSeconds + subtitleDisappearSeconds;
                int times = movieDuration / totalSubtitleDuration;
                if (times == 0)
                    return subtitleCommands;
                subtitleCommands = "-filter_complex \"[1:v]split =" + times.ToString();
                for (int i = 1; i <= times; i++)
                {
                    subtitleCommands += "[wm" + i.ToString() + "]";
                }
                subtitleCommands += ";";
                for (int i = 1; i <= times; i++)
                {
                    string temp = "[wm" + i.ToString() + "]fade=in:st=" + ((i - 1) * subtitleAppearSeconds).ToString() + ":d=1:alpha=1,fade=out:st=" + ((i - 1) * subtitleAppearSeconds + subtitleDisappearSeconds).ToString() + ":d=1:alpha=1[ovr" + i.ToString() + "];";
                    subtitleCommands += temp;
                }
                for (int i = 1; i <= times; i++)
                {
                    if (i == 1)
                    {
                        string temp = "[0:v][ovr" + i.ToString() + "]overlay=" + channels[streammingIndex].subtitleOptions.subtitle_X + ":" + channels[streammingIndex].subtitleOptions.subtitle_Y;
                        if (times == 1)
                        {
                            temp += "\"";
                        }
                        else
                        {
                            temp += "[base" + i.ToString() + "];";
                        }
                        subtitleCommands += temp;
                    }
                    else
                    {
                        if (i == times)
                        {
                            string temp = "[base" + (i - 1).ToString() + "][ovr" + i.ToString() + "]overlay=" + channels[streammingIndex].subtitleOptions.subtitle_X + ":" + channels[streammingIndex].subtitleOptions.subtitle_Y + "\"";
                            subtitleCommands += temp;
                        }
                        else
                        {
                            string temp = "[base" + (i - 1).ToString() + "][ovr" + i.ToString() + "]overlay=" + channels[streammingIndex].subtitleOptions.subtitle_X + ":" + channels[streammingIndex].subtitleOptions.subtitle_Y + "[base" + i.ToString() + "];";
                            subtitleCommands += temp;
                        }
                    }

                }
                return subtitleCommands;
            }
            else
            {
                string subtitleCommands = "";
                int totalSubtitleDuration = subtitleAppearSeconds + subtitleDisappearSeconds;
                int times = movieDuration / totalSubtitleDuration;
                if (times == 0)
                    return subtitleCommands;
                subtitleCommands = "-filter_complex \"[2:v]split =" + times.ToString();
                for (int i = 1; i <= times; i++)
                {
                    subtitleCommands += "[wm" + i.ToString() + "]";
                }
                subtitleCommands += ";";
                for (int i = 1; i <= times; i++)
                {
                    string temp = "[wm" + i.ToString() + "]fade=in:st=" + ((i - 1) * subtitleAppearSeconds).ToString() + ":d=1:alpha=1,fade=out:st=" + ((i - 1) * subtitleAppearSeconds + subtitleDisappearSeconds).ToString() + ":d=1:alpha=1[ovr" + i.ToString() + "];";
                    subtitleCommands += temp;
                }
                for (int i = 1; i <= times; i++)
                {
                    if (i == 1)
                    {
                        string temp = "[0][1]overlay=" + channels[streammingIndex].logoOptions.logo_X + ":" + channels[streammingIndex].logoOptions.logo_Y + "[logo];[logo][ovr" + i.ToString() + "]overlay=" + channels[streammingIndex].subtitleOptions.subtitle_X + ":" + channels[streammingIndex].subtitleOptions.subtitle_Y;
                        if (times == 1)
                        {
                            temp += "\"";
                        }
                        else
                        {
                            temp += "[base" + i.ToString() + "];";
                        }
                        subtitleCommands += temp;
                    }
                    else
                    {
                        if (i == times)
                        {
                            string temp = "[base" + (i - 1).ToString() + "][ovr" + i.ToString() + "]overlay=" + channels[streammingIndex].subtitleOptions.subtitle_X + ":" + channels[streammingIndex].subtitleOptions.subtitle_Y + "\"";
                            subtitleCommands += temp;
                        }
                        else
                        {
                            string temp = "[base" + (i - 1).ToString() + "][ovr" + i.ToString() + "]overlay=" + channels[streammingIndex].subtitleOptions.subtitle_X + ":" + channels[streammingIndex].subtitleOptions.subtitle_Y + "[base" + i.ToString() + "];";
                            subtitleCommands += temp;
                        }
                    }
                }
                return subtitleCommands;
            }
        }


        public void GetInfoForNameAndDurationForCurrentAndNextMovies(int streammingIndex, Boolean hours12, int indexOfMovie)
        {
            int movieSeconds = ConvertTimeToSeconds(moviesDay1[streammingIndex][indexOfMovie].Duration);
            int delaySeconds = ConvertTimeToSeconds(moviesDay1[streammingIndex][indexOfMovie].Delay);
            channels[streammingIndex].channelExtraInfo.currentMovieName = moviesDay1[streammingIndex][indexOfMovie].Name;
            channels[streammingIndex].channelExtraInfo.currentMovieDuration = moviesDay1[streammingIndex][indexOfMovie].Duration;
            if (moviesDay1[streammingIndex].Count - indexOfMovie > 1)
            {
                channels[streammingIndex].channelExtraInfo.nextMovieName = moviesDay1[streammingIndex][indexOfMovie + 1].Name;
            }
            else
            {
                if (hours12)
                {
                    string[] tokens = channels[streammingIndex].channelSchedules.schedule_day1.Substring(4).Split('%');
                    string[] movieInfo = tokens[0].Split('&');
                    channels[streammingIndex].channelExtraInfo.nextMovieName = movieInfo[0];
                }
                else
                {
                    if (channels[streammingIndex].channelSchedules.schedule_day2.Substring(4).Length == 0)
                    {
                        channels[streammingIndex].channelExtraInfo.nextMovieName = "";
                    }
                    else
                    {
                        string[] tokens = channels[streammingIndex].channelSchedules.schedule_day2.Substring(4).Split('%');
                        string[] movieInfo = tokens[0].Split('&');
                        channels[streammingIndex].channelExtraInfo.nextMovieName = movieInfo[0];
                    }
                }
            }
            if (channelIndex == streammingIndex)
            {
                currentMovieName.Content = channels[streammingIndex].channelExtraInfo.currentMovieName;
                currentMovieDuration.Content = channels[streammingIndex].channelExtraInfo.currentMovieDuration;
                nextMovieName.Content = channels[streammingIndex].channelExtraInfo.nextMovieName;
            }
        }


        public void PutTimerToNextMovie(int seconds, int streammingIndex)
        {
            channels[streammingIndex].channelExtraInfo.time = seconds;
            channels[streammingIndex].channelExtraInfo.timer = new DispatcherTimer();
            channels[streammingIndex].channelExtraInfo.timer.Interval = new TimeSpan(0, 0, 1);
            channels[streammingIndex].channelExtraInfo.timer.Tick += delegate { timer_Tick(streammingIndex); };
            channels[streammingIndex].channelExtraInfo.timer.Start();
        }


        public void timer_Tick(int streammingIndex)
        {

            if (channels[streammingIndex].channelExtraInfo.time != 0)
            {
                channels[streammingIndex].channelExtraInfo.time--;
                TimeSpan t = TimeSpan.FromSeconds(channels[streammingIndex].channelExtraInfo.time);
                string duration = string.Format("{0:D2}:{1:D2}:{2:D2}",
                             t.Hours,
                             t.Minutes,
                             t.Seconds
                           );
                if (channelIndex == streammingIndex)
                {
                    nextMovieDuration.Content = duration;
                }
            }
            else
            {
                if (channelIndex == streammingIndex)
                {
                    nextMovieDuration.Content = "";
                }
                channels[streammingIndex].channelExtraInfo.timer.Stop();
            }
        }


        public void ChangeDataGridDay1Image(Boolean hour12, int index, int streammingIndex, Boolean run)
        {
            if (!run)
            {
                if (hour12)
                {
                    moviesDay1[streammingIndex][index].IsFinished = "Icons/correct_wait12.png";
                }
                else
                {
                    moviesDay1[streammingIndex][index].IsFinished = "Icons/right24.png";
                }
            }
            else
            {
                moviesDay1[streammingIndex][index].IsFinished = "Icons/running_movie.png";
            }
            if (channelIndex == streammingIndex)
            {
                FillDataGridWithMovies(moviesDay1[streammingIndex], 1);
            }
        }


        public void ScheduleShiftting(int streammingIndex)
        {
            channels[streammingIndex].channelSchedules.schedule_day1 = channels[streammingIndex].channelSchedules.schedule_day2;
            channels[streammingIndex].channelSchedules.schedule_day2 = channels[streammingIndex].channelSchedules.schedule_day3;
            channels[streammingIndex].channelSchedules.schedule_day3 = channels[streammingIndex].channelSchedules.schedule_day4;
            channels[streammingIndex].channelSchedules.schedule_day4 = channels[streammingIndex].channelSchedules.schedule_day5;
            channels[streammingIndex].channelSchedules.schedule_day5 = channels[streammingIndex].channelSchedules.schedule_day6;
            channels[streammingIndex].channelSchedules.schedule_day6 = channels[streammingIndex].channelSchedules.schedule_day7;
            channels[streammingIndex].channelSchedules.schedule_day7 = "(12)";

            sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=false;Compress=True;");

            sqlite_conn.Open();

            sqlite_cmd = sqlite_conn.CreateCommand();

            sqlite_cmd.CommandText = "UPDATE Channels SET schedule_day1=\"" + channels[streammingIndex].channelSchedules.schedule_day1 + "\", schedule_day2=\"" + channels[streammingIndex].channelSchedules.schedule_day2 + "\", schedule_day3=\"" + channels[streammingIndex].channelSchedules.schedule_day3 + "\", schedule_day4=\"" + channels[streammingIndex].channelSchedules.schedule_day4 + "\", schedule_day5=\"" + channels[streammingIndex].channelSchedules.schedule_day5 + "\", schedule_day6=\"" + channels[streammingIndex].channelSchedules.schedule_day6 + "\", schedule_day7=\"" + channels[streammingIndex].channelSchedules.schedule_day7 + "\" WHERE channel_id=" + channels[streammingIndex].channel_id + ";";

            sqlite_cmd.ExecuteNonQuery();

            sqlite_conn.Close();

            moviesDay1[streammingIndex].Clear();
            if (channels[streammingIndex].channelSchedules.schedule_day1.Substring(4).Length > 0)
            {
                string[] tokens = channels[streammingIndex].channelSchedules.schedule_day1.Substring(4).Split('%');
                for (int i = 0; i < tokens.Length; i++)
                {
                    string[] movieInfo = tokens[i].Split('&');
                    Movie movie = new Movie();
                    movie.Name = movieInfo[0];
                    movie.Duration = movieInfo[1];
                    movie.Delay = movieInfo[2];
                    movie.Directory = movieInfo[3];
                    moviesDay1[streammingIndex].Add(movie);
                }
            }

            moviesDay2[streammingIndex].Clear();
            if (channels[streammingIndex].channelSchedules.schedule_day2.Substring(4).Length > 0)
            {
                string[] tokens = channels[streammingIndex].channelSchedules.schedule_day2.Substring(4).Split('%');
                for (int i = 0; i < tokens.Length; i++)
                {
                    string[] movieInfo = tokens[i].Split('&');
                    Movie movie = new Movie();
                    movie.Name = movieInfo[0];
                    movie.Duration = movieInfo[1];
                    movie.Delay = movieInfo[2];
                    movie.Directory = movieInfo[3];
                    moviesDay2[streammingIndex].Add(movie);
                }
            }

            moviesDay3[streammingIndex].Clear();
            if (channels[streammingIndex].channelSchedules.schedule_day3.Substring(4).Length > 0)
            {
                string[] tokens = channels[streammingIndex].channelSchedules.schedule_day3.Substring(4).Split('%');
                for (int i = 0; i < tokens.Length; i++)
                {
                    string[] movieInfo = tokens[i].Split('&');
                    Movie movie = new Movie();
                    movie.Name = movieInfo[0];
                    movie.Duration = movieInfo[1];
                    movie.Delay = movieInfo[2];
                    movie.Directory = movieInfo[3];
                    moviesDay3[streammingIndex].Add(movie);
                }
            }

            moviesDay4[streammingIndex].Clear();
            if (channels[streammingIndex].channelSchedules.schedule_day4.Substring(4).Length > 0)
            {
                string[] tokens = channels[streammingIndex].channelSchedules.schedule_day4.Substring(4).Split('%');
                for (int i = 0; i < tokens.Length; i++)
                {
                    string[] movieInfo = tokens[i].Split('&');
                    Movie movie = new Movie();
                    movie.Name = movieInfo[0];
                    movie.Duration = movieInfo[1];
                    movie.Delay = movieInfo[2];
                    movie.Directory = movieInfo[3];
                    moviesDay4[streammingIndex].Add(movie);
                }
            }

            moviesDay5[streammingIndex].Clear();
            if (channels[streammingIndex].channelSchedules.schedule_day5.Substring(4).Length > 0)
            {
                string[] tokens = channels[streammingIndex].channelSchedules.schedule_day5.Substring(4).Split('%');
                for (int i = 0; i < tokens.Length; i++)
                {
                    string[] movieInfo = tokens[i].Split('&');
                    Movie movie = new Movie();
                    movie.Name = movieInfo[0];
                    movie.Duration = movieInfo[1];
                    movie.Delay = movieInfo[2];
                    movie.Directory = movieInfo[3];
                    moviesDay5[streammingIndex].Add(movie);
                }
            }

            moviesDay6[streammingIndex].Clear();
            if (channels[streammingIndex].channelSchedules.schedule_day6.Substring(4).Length > 0)
            {
                string[] tokens = channels[streammingIndex].channelSchedules.schedule_day6.Substring(4).Split('%');
                for (int i = 0; i < tokens.Length; i++)
                {
                    string[] movieInfo = tokens[i].Split('&');
                    Movie movie = new Movie();
                    movie.Name = movieInfo[0];
                    movie.Duration = movieInfo[1];
                    movie.Delay = movieInfo[2];
                    movie.Directory = movieInfo[3];
                    moviesDay6[streammingIndex].Add(movie);
                }
            }

            moviesDay7[streammingIndex].Clear();
            if (channels[streammingIndex].channelSchedules.schedule_day7.Substring(4).Length > 0)
            {
                string[] tokens = channels[streammingIndex].channelSchedules.schedule_day7.Substring(4).Split('%');
                for (int i = 0; i < tokens.Length; i++)
                {
                    string[] movieInfo = tokens[i].Split('&');
                    Movie movie = new Movie();
                    movie.Name = movieInfo[0];
                    movie.Duration = movieInfo[1];
                    movie.Delay = movieInfo[2];
                    movie.Directory = movieInfo[3];
                    moviesDay7[streammingIndex].Add(movie);
                }
            }

            if (streammingIndex == channelIndex)
            {
                if (channels[channelIndex].channelSchedules.schedule_day1.Substring(4).Length > 0)
                {
                    string channelFormat = channels[channelIndex].channelSchedules.schedule_day1.Substring(1, 2);
                    if (channelFormat.Equals("12"))
                    {
                        scheduleDay1Hour12.IsChecked = true;
                        scheduleDay1Hour24.IsChecked = false;
                        scheduleDay1ElapsedTime.Content = TWELVEHOURS;
                        scheduleDay1RestTime.Content = ZEROHOURS;
                    }
                    else
                    {
                        scheduleDay1Hour12.IsChecked = false;
                        scheduleDay1Hour24.IsChecked = true;
                        scheduleDay1ElapsedTime.Content = FORTEENHOURS;
                        scheduleDay1RestTime.Content = ZEROHOURS;
                    }
                    FillDataGridWithMovies(moviesDay1[streammingIndex], 1);
                }
                else
                {
                    Day1Grid.Items.Clear();
                    string channelFormat = channels[channelIndex].channelSchedules.schedule_day1.Substring(1, 2);
                    if (channelFormat.Equals("12"))
                    {
                        scheduleDay1Hour12.IsChecked = true;
                        scheduleDay1Hour24.IsChecked = false;
                        scheduleDay1ElapsedTime.Content = ZEROHOURS;
                        scheduleDay1RestTime.Content = TWELVEHOURS;
                    }
                    else
                    {
                        scheduleDay1Hour12.IsChecked = false;
                        scheduleDay1Hour24.IsChecked = true;
                        scheduleDay1ElapsedTime.Content = ZEROHOURS;
                        scheduleDay1RestTime.Content = FORTEENHOURS;
                    }
                }
                if (channels[channelIndex].channelSchedules.schedule_day2.Substring(4).Length > 0)
                {
                    string channelFormat = channels[channelIndex].channelSchedules.schedule_day2.Substring(1, 2);
                    if (channelFormat.Equals("12"))
                    {
                        scheduleDay2Hour12.IsChecked = true;
                        scheduleDay2Hour24.IsChecked = false;
                        scheduleDay2ElapsedTime.Content = TWELVEHOURS;
                        scheduleDay2RestTime.Content = ZEROHOURS;
                    }
                    else
                    {
                        scheduleDay2Hour12.IsChecked = false;
                        scheduleDay2Hour24.IsChecked = true;
                        scheduleDay2ElapsedTime.Content = FORTEENHOURS;
                        scheduleDay2RestTime.Content = ZEROHOURS;
                    }
                    FillDataGridWithMovies(moviesDay2[streammingIndex], 2);
                }
                else
                {
                    Day2Grid.Items.Clear();
                    string channelFormat = channels[channelIndex].channelSchedules.schedule_day2.Substring(1, 2);
                    if (channelFormat.Equals("12"))
                    {
                        scheduleDay2Hour12.IsChecked = true;
                        scheduleDay2Hour24.IsChecked = false;
                        scheduleDay2ElapsedTime.Content = ZEROHOURS;
                        scheduleDay2RestTime.Content = TWELVEHOURS;
                    }
                    else
                    {
                        scheduleDay2Hour12.IsChecked = false;
                        scheduleDay2Hour24.IsChecked = true;
                        scheduleDay2ElapsedTime.Content = ZEROHOURS;
                        scheduleDay2RestTime.Content = FORTEENHOURS;
                    }
                }
                if (channels[channelIndex].channelSchedules.schedule_day3.Substring(4).Length > 0)
                {
                    string channelFormat = channels[channelIndex].channelSchedules.schedule_day3.Substring(1, 2);
                    if (channelFormat.Equals("12"))
                    {
                        scheduleDay3Hour12.IsChecked = true;
                        scheduleDay3Hour24.IsChecked = false;
                        scheduleDay3ElapsedTime.Content = TWELVEHOURS;
                        scheduleDay3RestTime.Content = ZEROHOURS;
                    }
                    else
                    {
                        scheduleDay3Hour12.IsChecked = false;
                        scheduleDay3Hour24.IsChecked = true;
                        scheduleDay3ElapsedTime.Content = FORTEENHOURS;
                        scheduleDay3RestTime.Content = ZEROHOURS;
                    }
                    FillDataGridWithMovies(moviesDay3[streammingIndex], 3);
                }
                else
                {
                    Day3Grid.Items.Clear();
                    string channelFormat = channels[channelIndex].channelSchedules.schedule_day3.Substring(1, 2);
                    if (channelFormat.Equals("12"))
                    {
                        scheduleDay3Hour12.IsChecked = true;
                        scheduleDay3Hour24.IsChecked = false;
                        scheduleDay3ElapsedTime.Content = ZEROHOURS;
                        scheduleDay3RestTime.Content = TWELVEHOURS;
                    }
                    else
                    {
                        scheduleDay3Hour12.IsChecked = false;
                        scheduleDay3Hour24.IsChecked = true;
                        scheduleDay3ElapsedTime.Content = ZEROHOURS;
                        scheduleDay3RestTime.Content = FORTEENHOURS;
                    }
                }
                if (channels[channelIndex].channelSchedules.schedule_day4.Substring(4).Length > 0)
                {
                    string channelFormat = channels[channelIndex].channelSchedules.schedule_day4.Substring(1, 2);
                    if (channelFormat.Equals("12"))
                    {
                        scheduleDay4Hour12.IsChecked = true;
                        scheduleDay4Hour24.IsChecked = false;
                        scheduleDay4ElapsedTime.Content = TWELVEHOURS;
                        scheduleDay4RestTime.Content = ZEROHOURS;
                    }
                    else
                    {
                        scheduleDay4Hour12.IsChecked = false;
                        scheduleDay4Hour24.IsChecked = true;
                        scheduleDay4ElapsedTime.Content = FORTEENHOURS;
                        scheduleDay4RestTime.Content = ZEROHOURS;
                    }
                    FillDataGridWithMovies(moviesDay4[streammingIndex], 4);
                }
                else
                {
                    Day4Grid.Items.Clear();
                    string channelFormat = channels[channelIndex].channelSchedules.schedule_day4.Substring(1, 2);
                    if (channelFormat.Equals("12"))
                    {
                        scheduleDay4Hour12.IsChecked = true;
                        scheduleDay4Hour24.IsChecked = false;
                        scheduleDay4ElapsedTime.Content = ZEROHOURS;
                        scheduleDay4RestTime.Content = TWELVEHOURS;
                    }
                    else
                    {
                        scheduleDay4Hour12.IsChecked = false;
                        scheduleDay4Hour24.IsChecked = true;
                        scheduleDay4ElapsedTime.Content = ZEROHOURS;
                        scheduleDay4RestTime.Content = FORTEENHOURS;
                    }
                }
                if (channels[channelIndex].channelSchedules.schedule_day5.Substring(4).Length > 0)
                {
                    string channelFormat = channels[channelIndex].channelSchedules.schedule_day5.Substring(1, 2);
                    if (channelFormat.Equals("12"))
                    {
                        scheduleDay5Hour12.IsChecked = true;
                        scheduleDay5Hour24.IsChecked = false;
                        scheduleDay5ElapsedTime.Content = TWELVEHOURS;
                        scheduleDay5RestTime.Content = ZEROHOURS;
                    }
                    else
                    {
                        scheduleDay5Hour12.IsChecked = false;
                        scheduleDay5Hour24.IsChecked = true;
                        scheduleDay5ElapsedTime.Content = FORTEENHOURS;
                        scheduleDay5RestTime.Content = ZEROHOURS;
                    }
                    FillDataGridWithMovies(moviesDay5[streammingIndex], 5);
                }
                else
                {
                    Day5Grid.Items.Clear();
                    string channelFormat = channels[channelIndex].channelSchedules.schedule_day5.Substring(1, 2);
                    if (channelFormat.Equals("12"))
                    {
                        scheduleDay5Hour12.IsChecked = true;
                        scheduleDay5Hour24.IsChecked = false;
                        scheduleDay5ElapsedTime.Content = ZEROHOURS;
                        scheduleDay5RestTime.Content = TWELVEHOURS;
                    }
                    else
                    {
                        scheduleDay5Hour12.IsChecked = false;
                        scheduleDay5Hour24.IsChecked = true;
                        scheduleDay5ElapsedTime.Content = ZEROHOURS;
                        scheduleDay5RestTime.Content = FORTEENHOURS;
                    }
                }
                if (channels[channelIndex].channelSchedules.schedule_day6.Substring(4).Length > 0)
                {
                    string channelFormat = channels[channelIndex].channelSchedules.schedule_day6.Substring(1, 2);
                    if (channelFormat.Equals("12"))
                    {
                        scheduleDay6Hour12.IsChecked = true;
                        scheduleDay6Hour24.IsChecked = false;
                        scheduleDay6ElapsedTime.Content = TWELVEHOURS;
                        scheduleDay6RestTime.Content = ZEROHOURS;
                    }
                    else
                    {
                        scheduleDay6Hour12.IsChecked = false;
                        scheduleDay6Hour24.IsChecked = true;
                        scheduleDay6ElapsedTime.Content = FORTEENHOURS;
                        scheduleDay6RestTime.Content = ZEROHOURS;
                    }
                    FillDataGridWithMovies(moviesDay6[streammingIndex], 6);
                }
                else
                {
                    Day6Grid.Items.Clear();
                    string channelFormat = channels[channelIndex].channelSchedules.schedule_day6.Substring(1, 2);
                    if (channelFormat.Equals("12"))
                    {
                        scheduleDay6Hour12.IsChecked = true;
                        scheduleDay6Hour24.IsChecked = false;
                        scheduleDay6ElapsedTime.Content = ZEROHOURS;
                        scheduleDay6RestTime.Content = TWELVEHOURS;
                    }
                    else
                    {
                        scheduleDay6Hour12.IsChecked = false;
                        scheduleDay6Hour24.IsChecked = true;
                        scheduleDay6ElapsedTime.Content = ZEROHOURS;
                        scheduleDay6RestTime.Content = FORTEENHOURS;
                    }
                }
                if (channels[channelIndex].channelSchedules.schedule_day7.Substring(4).Length > 0)
                {
                    string channelFormat = channels[channelIndex].channelSchedules.schedule_day7.Substring(1, 2);
                    if (channelFormat.Equals("12"))
                    {
                        scheduleDay7Hour12.IsChecked = true;
                        scheduleDay7Hour24.IsChecked = false;
                        scheduleDay7ElapsedTime.Content = TWELVEHOURS;
                        scheduleDay7RestTime.Content = ZEROHOURS;
                    }
                    else
                    {
                        scheduleDay7Hour12.IsChecked = false;
                        scheduleDay7Hour24.IsChecked = true;
                        scheduleDay7ElapsedTime.Content = FORTEENHOURS;
                        scheduleDay7RestTime.Content = ZEROHOURS;
                    }
                    FillDataGridWithMovies(moviesDay7[streammingIndex], 7);
                }
                else
                {
                    Day7Grid.Items.Clear();
                    string channelFormat = channels[channelIndex].channelSchedules.schedule_day7.Substring(1, 2);
                    if (channelFormat.Equals("12"))
                    {
                        scheduleDay7Hour12.IsChecked = true;
                        scheduleDay7Hour24.IsChecked = false;
                        scheduleDay7ElapsedTime.Content = ZEROHOURS;
                        scheduleDay7RestTime.Content = TWELVEHOURS;
                    }
                    else
                    {
                        scheduleDay7Hour12.IsChecked = false;
                        scheduleDay7Hour24.IsChecked = true;
                        scheduleDay7ElapsedTime.Content = ZEROHOURS;
                        scheduleDay7RestTime.Content = FORTEENHOURS;
                    }
                }
                CalculateDateForAllMovies(streammingIndex, DateTime.Now, DateTime.Now, 0);
            }
        }


        public void RestartGridInfo(int streammingIndex)
        {
            moviesDay1[streammingIndex].Clear();
            if (channels[streammingIndex].channelSchedules.schedule_day1.Substring(4).Length > 0)
            {
                string[] tokens = channels[streammingIndex].channelSchedules.schedule_day1.Substring(4).Split('%');
                for (int i = 0; i < tokens.Length; i++)
                {
                    string[] movieInfo = tokens[i].Split('&');
                    Movie movie = new Movie();
                    movie.Name = movieInfo[0];
                    movie.Duration = movieInfo[1];
                    movie.Delay = movieInfo[2];
                    movie.Directory = movieInfo[3];
                    moviesDay1[streammingIndex].Add(movie);
                }
            }
            for (int i = 0; i < moviesDay2[streammingIndex].Count; i++)
            {
                moviesDay2[streammingIndex][i].MovieTime = String.Empty;
                moviesDay2[streammingIndex][i].MovieTime2 = String.Empty;
            }
            for (int i = 0; i < moviesDay3[streammingIndex].Count; i++)
            {
                moviesDay3[streammingIndex][i].MovieTime = String.Empty;
                moviesDay3[streammingIndex][i].MovieTime2 = String.Empty;
            }
            for (int i = 0; i < moviesDay4[streammingIndex].Count; i++)
            {
                moviesDay4[streammingIndex][i].MovieTime = String.Empty;
                moviesDay4[streammingIndex][i].MovieTime2 = String.Empty;
            }
            for (int i = 0; i < moviesDay5[streammingIndex].Count; i++)
            {
                moviesDay5[streammingIndex][i].MovieTime = String.Empty;
                moviesDay5[streammingIndex][i].MovieTime2 = String.Empty;
            }
            for (int i = 0; i < moviesDay6[streammingIndex].Count; i++)
            {
                moviesDay6[streammingIndex][i].MovieTime = String.Empty;
                moviesDay6[streammingIndex][i].MovieTime2 = String.Empty;
            }
            for (int i = 0; i < moviesDay7[streammingIndex].Count; i++)
            {
                moviesDay7[streammingIndex][i].MovieTime = String.Empty;
                moviesDay7[streammingIndex][i].MovieTime2 = String.Empty;
            }
            try
            {
                if (Process.GetProcesses().Any(x => x.Id == channels[streammingIndex].channelExtraInfo.process.Id))
                {
                    channels[streammingIndex].channelExtraInfo.process.Kill();
                }
                if (channels[streammingIndex].channelExtraInfo.timer.IsEnabled)
                    channels[streammingIndex].channelExtraInfo.timer.Stop();
            }
            catch (Exception ex) { };

            channels[streammingIndex].channel_state = "off";
            channels[streammingIndex].channelExtraInfo.currentMovieName = "";
            channels[streammingIndex].channelExtraInfo.currentMovieDuration = "";
            channels[streammingIndex].channelExtraInfo.nextMovieName = "";
            if (streammingIndex == channelIndex)
            {
                FillDataGridWithMovies(moviesDay1[streammingIndex], 1);
                FillDataGridWithMovies(moviesDay2[streammingIndex], 2);
                FillDataGridWithMovies(moviesDay3[streammingIndex], 3);
                FillDataGridWithMovies(moviesDay4[streammingIndex], 4);
                FillDataGridWithMovies(moviesDay5[streammingIndex], 5);
                FillDataGridWithMovies(moviesDay6[streammingIndex], 6);
                FillDataGridWithMovies(moviesDay7[streammingIndex], 7);

                string scheduleFormatDay1 = channels[streammingIndex].channelSchedules.schedule_day1;
                string totalHoursDay1 = scheduleFormatDay1.Substring(1, 2);
                string scheduleDay1 = scheduleFormatDay1.Substring(4);
                if (totalHoursDay1.Equals("12"))
                {
                    scheduleDay1Hour12.IsChecked = true;
                    if (scheduleDay1.Length == 0)
                    {
                        scheduleDay1ElapsedTime.Content = ZEROHOURS;
                        scheduleDay1RestTime.Content = TWELVEHOURS;
                    }
                    else
                    {
                        scheduleDay1ElapsedTime.Content = TWELVEHOURS;
                        scheduleDay1RestTime.Content = ZEROHOURS;
                    }
                }
                else
                {
                    scheduleDay1Hour24.IsChecked = true;
                    if (scheduleDay1.Length == 0)
                    {
                        scheduleDay1ElapsedTime.Content = ZEROHOURS;
                        scheduleDay1RestTime.Content = FORTEENHOURS;
                    }
                    else
                    {
                        scheduleDay1ElapsedTime.Content = FORTEENHOURS;
                        scheduleDay1RestTime.Content = ZEROHOURS;
                    }
                }

                string scheduleFormatDay2 = channels[streammingIndex].channelSchedules.schedule_day2;
                string totalHoursDay2 = scheduleFormatDay2.Substring(1, 2);
                string scheduleDay2 = scheduleFormatDay2.Substring(4);
                if (totalHoursDay2.Equals("12"))
                {
                    scheduleDay2Hour12.IsChecked = true;
                    if (scheduleDay2.Length == 0)
                    {
                        scheduleDay2ElapsedTime.Content = ZEROHOURS;
                        scheduleDay2RestTime.Content = TWELVEHOURS;
                    }
                    else
                    {
                        scheduleDay2ElapsedTime.Content = TWELVEHOURS;
                        scheduleDay2RestTime.Content = ZEROHOURS;
                    }
                }
                else
                {
                    scheduleDay2Hour24.IsChecked = true;
                    if (scheduleDay2.Length == 0)
                    {
                        scheduleDay2ElapsedTime.Content = ZEROHOURS;
                        scheduleDay2RestTime.Content = FORTEENHOURS;
                    }
                    else
                    {
                        scheduleDay2ElapsedTime.Content = FORTEENHOURS;
                        scheduleDay2RestTime.Content = ZEROHOURS;
                    }
                }

                string scheduleFormatDay3 = channels[streammingIndex].channelSchedules.schedule_day3;
                string totalHoursDay3 = scheduleFormatDay3.Substring(1, 2);
                string scheduleDay3 = scheduleFormatDay3.Substring(4);
                if (totalHoursDay3.Equals("12"))
                {
                    scheduleDay3Hour12.IsChecked = true;
                    if (scheduleDay3.Length == 0)
                    {
                        scheduleDay3ElapsedTime.Content = ZEROHOURS;
                        scheduleDay3RestTime.Content = TWELVEHOURS;
                    }
                    else
                    {
                        scheduleDay3ElapsedTime.Content = TWELVEHOURS;
                        scheduleDay3RestTime.Content = ZEROHOURS;
                    }
                }
                else
                {
                    scheduleDay3Hour24.IsChecked = true;
                    if (scheduleDay3.Length == 0)
                    {
                        scheduleDay3ElapsedTime.Content = ZEROHOURS;
                        scheduleDay3RestTime.Content = FORTEENHOURS;
                    }
                    else
                    {
                        scheduleDay3ElapsedTime.Content = FORTEENHOURS;
                        scheduleDay3RestTime.Content = ZEROHOURS;
                    }
                }

                string scheduleFormatDay4 = channels[streammingIndex].channelSchedules.schedule_day4;
                string totalHoursDay4 = scheduleFormatDay4.Substring(1, 2);
                string scheduleDay4 = scheduleFormatDay4.Substring(4);
                if (totalHoursDay4.Equals("12"))
                {
                    scheduleDay4Hour12.IsChecked = true;
                    if (scheduleDay4.Length == 0)
                    {
                        scheduleDay4ElapsedTime.Content = ZEROHOURS;
                        scheduleDay4RestTime.Content = TWELVEHOURS;
                    }
                    else
                    {
                        scheduleDay4ElapsedTime.Content = TWELVEHOURS;
                        scheduleDay4RestTime.Content = ZEROHOURS;
                    }
                }
                else
                {
                    scheduleDay4Hour24.IsChecked = true;
                    if (scheduleDay4.Length == 0)
                    {
                        scheduleDay4ElapsedTime.Content = ZEROHOURS;
                        scheduleDay4RestTime.Content = FORTEENHOURS;
                    }
                    else
                    {
                        scheduleDay4ElapsedTime.Content = FORTEENHOURS;
                        scheduleDay4RestTime.Content = ZEROHOURS;
                    }
                }

                string scheduleFormatDay5 = channels[streammingIndex].channelSchedules.schedule_day5;
                string totalHoursDay5 = scheduleFormatDay5.Substring(1, 2);
                string scheduleDay5 = scheduleFormatDay5.Substring(4);
                if (totalHoursDay5.Equals("12"))
                {
                    scheduleDay5Hour12.IsChecked = true;
                    if (scheduleDay5.Length == 0)
                    {
                        scheduleDay5ElapsedTime.Content = ZEROHOURS;
                        scheduleDay5RestTime.Content = TWELVEHOURS;
                    }
                    else
                    {
                        scheduleDay5ElapsedTime.Content = TWELVEHOURS;
                        scheduleDay5RestTime.Content = ZEROHOURS;
                    }
                }
                else
                {
                    scheduleDay5Hour24.IsChecked = true;
                    if (scheduleDay5.Length == 0)
                    {
                        scheduleDay5ElapsedTime.Content = ZEROHOURS;
                        scheduleDay5RestTime.Content = FORTEENHOURS;
                    }
                    else
                    {
                        scheduleDay5ElapsedTime.Content = FORTEENHOURS;
                        scheduleDay5RestTime.Content = ZEROHOURS;
                    }
                }

                string scheduleFormatDay6 = channels[streammingIndex].channelSchedules.schedule_day6;
                string totalHoursDay6 = scheduleFormatDay6.Substring(1, 2);
                string scheduleDay6 = scheduleFormatDay6.Substring(4);
                if (totalHoursDay6.Equals("12"))
                {
                    scheduleDay6Hour12.IsChecked = true;
                    if (scheduleDay6.Length == 0)
                    {
                        scheduleDay6ElapsedTime.Content = ZEROHOURS;
                        scheduleDay6RestTime.Content = TWELVEHOURS;
                    }
                    else
                    {
                        scheduleDay6ElapsedTime.Content = TWELVEHOURS;
                        scheduleDay6RestTime.Content = ZEROHOURS;
                    }
                }
                else
                {
                    scheduleDay6Hour24.IsChecked = true;
                    if (scheduleDay6.Length == 0)
                    {
                        scheduleDay6ElapsedTime.Content = ZEROHOURS;
                        scheduleDay6RestTime.Content = FORTEENHOURS;
                    }
                    else
                    {
                        scheduleDay6ElapsedTime.Content = FORTEENHOURS;
                        scheduleDay6RestTime.Content = ZEROHOURS;
                    }
                }

                string scheduleFormatDay7 = channels[streammingIndex].channelSchedules.schedule_day7;
                string totalHoursDay7 = scheduleFormatDay7.Substring(1, 2);
                string scheduleDay7 = scheduleFormatDay1.Substring(4);
                if (totalHoursDay7.Equals("12"))
                {
                    scheduleDay7Hour12.IsChecked = true;
                    if (scheduleDay7.Length == 0)
                    {
                        scheduleDay7ElapsedTime.Content = ZEROHOURS;
                        scheduleDay7RestTime.Content = TWELVEHOURS;
                    }
                    else
                    {
                        scheduleDay7ElapsedTime.Content = TWELVEHOURS;
                        scheduleDay7RestTime.Content = ZEROHOURS;
                    }
                }
                else
                {
                    scheduleDay7Hour24.IsChecked = true;
                    if (scheduleDay7.Length == 0)
                    {
                        scheduleDay7ElapsedTime.Content = ZEROHOURS;
                        scheduleDay7RestTime.Content = FORTEENHOURS;
                    }
                    else
                    {
                        scheduleDay7ElapsedTime.Content = FORTEENHOURS;
                        scheduleDay7RestTime.Content = ZEROHOURS;
                    }
                }

                channel_state_btn.IsChecked = false;
                streamming_state_files.IsEnabled = true;
                streamming_state_UDP.IsEnabled = true;
                scheduleDay1Hour12.IsEnabled = true;
                scheduleDay1Hour24.IsEnabled = true;
                scheduleDay1Save_btn.IsEnabled = true;
                if (streamming_state_UDP.IsChecked == true)
                {
                    networkBox.IsEnabled = true;
                    portBox.IsEnabled = true;
                    networkUDPBox.IsEnabled = true;
                    portUDPBox.IsEnabled = true;
                }
                currentMovieName.Content = "";
                currentMovieDuration.Content = "";
                nextMovieName.Content = "";
                nextMovieDuration.Content = "";
            }
        }


        public void StartStreammingUDP(int channelIdx)
        {
            int streammingIndex = channelIdx;
            channels[streammingIndex].channelExtraInfo.process = new Process();
            channels[streammingIndex].channelExtraInfo.process.StartInfo.FileName = "cmd.exe";
            channels[streammingIndex].channelExtraInfo.process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            channels[streammingIndex].channelExtraInfo.process.StartInfo.Arguments = "/C Playout.exe";
            channels[streammingIndex].channelExtraInfo.process.Start();
            channels[streammingIndex].channelExtraInfo.process.StartInfo.FileName = "Playout.exe";
            string command = "-re ";
            this.Dispatcher.Invoke((Action)(() =>
            {
                if (channels[streammingIndex].logoOptions.logo_state.Equals("off"))
                {
                    if (channels[streammingIndex].subtitleOptions.subtitle_state.Equals("off"))
                    {
                        string udpPath = "-i udp://" + channels[streammingIndex].channelOptions.networkUDP + ":" + channels[streammingIndex].channelOptions.portUDP + "?fifo_size=5000000";
                        command += udpPath;
                    }
                    else
                    {
                        if (channels[streammingIndex].subtitleOptions.subtitle_appearance_state.Equals("always"))
                        {
                            string udpPath = "-i udp://" + channels[streammingIndex].channelOptions.networkUDP + ":" + channels[streammingIndex].channelOptions.portUDP + "?fifo_size=5000000" + " -ignore_loop 0 -i " + "\"" + channels[streammingIndex].subtitleOptions.subtitle_name + "\" ";
                            command += udpPath;
                            string filter_complex = "-filter_complex \"[0][1]overlay=" + channels[streammingIndex].subtitleOptions.subtitle_X + ":" + channels[streammingIndex].subtitleOptions.subtitle_Y + "\"";
                            command += filter_complex;
                        }
                        else
                        {
                            MessageBox.Show("You can't use chosen appearance state for subtitle with UDP streaming in channel state ");
                            if (channelIndex == streammingIndex)
                            {
                                subtitle_nameBox.IsEnabled = true;
                                subtitle_X_Box.IsEnabled = true;
                                subtitle_Y_Box.IsEnabled = true;
                                subtitleBrowse_btn.IsEnabled = true;
                                subtitleBrowseClear_btn.IsEnabled = true;
                                subtitle_appear_H.IsEnabled = true;
                                subtitle_appear_M.IsEnabled = true;
                                subtitle_disappear_M.IsEnabled = true;
                                subtitle_disappear_S.IsEnabled = true;
                                subtitle_state_btn.IsChecked = false;
                            }
                        }
                    }
                }
                else
                {
                    if (channels[streammingIndex].subtitleOptions.subtitle_state.Equals("off"))
                    {
                        string udpPath = "-i " + "udp://" + channels[streammingIndex].channelOptions.networkUDP + ":" + channels[streammingIndex].channelOptions.portUDP + "?fifo_size=5000000" + " -i " + "\"" + channels[streammingIndex].logoOptions.logo_name + "\" ";
                        command += udpPath;
                        string filter_complex = "-filter_complex \"[0][1]overlay=" + channels[streammingIndex].logoOptions.logo_X + ":" + channels[streammingIndex].logoOptions.logo_Y + "\"";
                        command += filter_complex;
                    }
                    else
                    {
                        if (channels[streammingIndex].subtitleOptions.subtitle_appearance_state.Equals("always"))
                        {
                            string udpPath = "-i " + "udp://" + channels[streammingIndex].channelOptions.networkUDP + ":" + channels[streammingIndex].channelOptions.portUDP + "?fifo_size=5000000" + " -i " + "\"" + channels[streammingIndex].logoOptions.logo_name + "\" " + " -ignore_loop 0 -i " + "\"" + channels[streammingIndex].subtitleOptions.subtitle_name + "\" ";
                            command += udpPath;
                            string filter_complex = "-filter_complex \"[0][1]overlay=" + channels[streammingIndex].logoOptions.logo_X + ":" + channels[streammingIndex].logoOptions.logo_Y + "[a];[a][2]overlay=" + channels[streammingIndex].subtitleOptions.subtitle_X + ":" + channels[streammingIndex].subtitleOptions.subtitle_Y + "\"";
                            command += filter_complex;
                        }
                        else
                        {
                            MessageBox.Show("You can't use chosen appearance state for subtitle with UDP streaming in channel state ");
                            if (channelIndex == streammingIndex)
                            {
                                subtitle_nameBox.IsEnabled = true;
                                subtitle_X_Box.IsEnabled = true;
                                subtitle_Y_Box.IsEnabled = true;
                                subtitleBrowse_btn.IsEnabled = true;
                                subtitleBrowseClear_btn.IsEnabled = true;
                                subtitle_appear_H.IsEnabled = true;
                                subtitle_appear_M.IsEnabled = true;
                                subtitle_disappear_M.IsEnabled = true;
                                subtitle_disappear_S.IsEnabled = true;
                                subtitle_state_btn.IsChecked = false;
                            }
                        }
                    }
                }
                command += " -pass 1 -b:v 2.5M -c:v libx264 -acodec aac -f mpegts udp://" + channels[streammingIndex].channelOptions.network + ":" + channels[streammingIndex].channelOptions.port + "?pkt_size=188&buffer_size=3M";

            }));

            channels[streammingIndex].channelExtraInfo.process.StartInfo.FileName = "Playout.exe";
            channels[streammingIndex].channelExtraInfo.process.StartInfo.Arguments = command;
            channels[streammingIndex].channelExtraInfo.process.Start();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            channels[streammingIndex].channelExtraInfo.process.WaitForExit();
            watch.Stop();
            channels[streammingIndex].channel_state = "off";
            if (60 > watch.Elapsed.TotalSeconds && !channels[streammingIndex].channelExtraInfo.processKill)
            {
                MessageBox.Show("Error streamming happend in " + channels[streammingIndex].channelOptions.channel_name);
                this.Dispatcher.Invoke((Action)(() =>
                {
                    if (channelIndex == streammingIndex)
                    {
                        channel_state_btn.IsChecked = false;
                        streamming_state_files.IsEnabled = true;
                        streamming_state_UDP.IsEnabled = true;
                        scheduleDay1Hour12.IsEnabled = true;
                        scheduleDay1Hour24.IsEnabled = true;
                        scheduleDay1Save_btn.IsEnabled = true;
                        if (streamming_state_UDP.IsChecked == true)
                        {
                            networkBox.IsEnabled = true;
                            portBox.IsEnabled = true;
                            networkUDPBox.IsEnabled = true;
                            portUDPBox.IsEnabled = true;
                        }
                    }
                    try
                    {
                        if (Process.GetProcesses().Any(x => x.Id == channels[streammingIndex].channelExtraInfo.process.Id))
                        {
                            channels[streammingIndex].channelExtraInfo.process.Kill();
                        }
                    }
                    catch (Exception ex) { };
                }));
            }
        }


        #endregion


        #region Date and Export Schedule

        public void CalculateDateForAllMovies(int streammingIndex, DateTime date, DateTime time, int day1StartIndex)
        {
            string dateString = date.ToString("M/d/yyyy");
            string timeString = time.ToString("HH:mm:ss");
            int daysToAdd = 0;
            if (moviesDay1[streammingIndex].Count == 0)
                return;
            for (int i = day1StartIndex; i < moviesDay1[streammingIndex].Count; i++)
            {
                Day1Grid.Items.Remove(moviesDay1[streammingIndex][i]);
                moviesDay1[streammingIndex][i].MovieTime = timeString + "\n" + dateString;
                Day1Grid.Items.Insert(i, moviesDay1[streammingIndex][i]);
                int timeSeconds = ConvertTimeToSeconds(timeString);
                int movieSeconds = ConvertTimeToSeconds(moviesDay1[streammingIndex][i].Duration);
                int delaySeconds = ConvertTimeToSeconds(moviesDay1[streammingIndex][i].Delay);
                timeString = ConvertSecondsToTime(timeSeconds + movieSeconds + delaySeconds);
                int newTimeHours = int.Parse(timeString.Substring(0, 2));
                if (newTimeHours >= 24)
                {
                    daysToAdd++;
                    int newTimeSeconds = ConvertTimeToSeconds(timeString);
                    int daySeconds = ConvertTimeToSeconds("24:00:00");
                    timeString = ConvertSecondsToTime(newTimeSeconds - daySeconds);
                }
                dateString = date.AddDays(daysToAdd).ToString("M/d/yyyy");
            }
            if (day1StartIndex > 0)  //channel is running
            {
                for (int i = 0; i < day1StartIndex; i++)
                {
                    Day1Grid.Items.Remove(moviesDay1[streammingIndex][i]);
                    moviesDay1[streammingIndex][i].MovieTime = "";
                    Day1Grid.Items.Insert(i, moviesDay1[streammingIndex][i]);
                }

                Boolean hour12 = false;
                if (day1StartIndex == Day1Grid.Items.Count)
                {
                    if (moviesDay1[streammingIndex][day1StartIndex - 2].IsFinished.Equals("Icons/correct_wait12.png"))
                    {
                        hour12 = true;
                    }
                }
                else
                {
                    if (moviesDay1[streammingIndex][day1StartIndex].IsFinished.Equals("Icons/wait12.png"))
                    {
                        hour12 = true;
                    }
                }

                if (hour12)
                {
                    for (int i = 0; i < moviesDay1[streammingIndex].Count; i++)
                    {
                        Day1Grid.Items.Remove(moviesDay1[streammingIndex][i]);
                        moviesDay1[streammingIndex][i].MovieTime2 = timeString + "\n" + dateString;
                        Day1Grid.Items.Insert(i, moviesDay1[streammingIndex][i]);
                        int timeSeconds = ConvertTimeToSeconds(timeString);
                        int movieSeconds = ConvertTimeToSeconds(moviesDay1[streammingIndex][i].Duration);
                        int delaySeconds = ConvertTimeToSeconds(moviesDay1[streammingIndex][i].Delay);
                        timeString = ConvertSecondsToTime(timeSeconds + movieSeconds + delaySeconds);
                        int newTimeHours = int.Parse(timeString.Substring(0, 2));
                        if (newTimeHours >= 24)
                        {
                            daysToAdd++;
                            int newTimeSeconds = ConvertTimeToSeconds(timeString);
                            int daySeconds = ConvertTimeToSeconds("24:00:00");
                            timeString = ConvertSecondsToTime(newTimeSeconds - daySeconds);
                        }
                        dateString = date.AddDays(daysToAdd).ToString("M/d/yyyy");
                    }
                }
                else
                {
                    for (int i = 0; i < moviesDay1[streammingIndex].Count; i++)
                    {
                        Day1Grid.Items.Remove(moviesDay1[streammingIndex][i]);
                        moviesDay1[streammingIndex][i].MovieTime2 = "";
                        Day1Grid.Items.Insert(i, moviesDay1[streammingIndex][i]);
                    }
                }
            }
            else
            {
                if (channels[streammingIndex].channelSchedules.schedule_day1.Substring(1, 2).Equals("12"))
                {
                    for (int i = 0; i < moviesDay1[streammingIndex].Count; i++)
                    {
                        Day1Grid.Items.Remove(moviesDay1[streammingIndex][i]);
                        moviesDay1[streammingIndex][i].MovieTime2 = timeString + "\n" + dateString;
                        Day1Grid.Items.Insert(i, moviesDay1[streammingIndex][i]);
                        int timeSeconds = ConvertTimeToSeconds(timeString);
                        int movieSeconds = ConvertTimeToSeconds(moviesDay1[streammingIndex][i].Duration);
                        int delaySeconds = ConvertTimeToSeconds(moviesDay1[streammingIndex][i].Delay);
                        timeString = ConvertSecondsToTime(timeSeconds + movieSeconds + delaySeconds);
                        int newTimeHours = int.Parse(timeString.Substring(0, 2));
                        if (newTimeHours >= 24)
                        {
                            daysToAdd++;
                            int newTimeSeconds = ConvertTimeToSeconds(timeString);
                            int daySeconds = ConvertTimeToSeconds("24:00:00");
                            timeString = ConvertSecondsToTime(newTimeSeconds - daySeconds);
                        }
                        dateString = date.AddDays(daysToAdd).ToString("M/d/yyyy");
                    }
                }
                else
                {
                    for (int i = 0; i < moviesDay1[streammingIndex].Count; i++)
                    {
                        Day1Grid.Items.Remove(moviesDay1[streammingIndex][i]);
                        moviesDay1[streammingIndex][i].MovieTime2 = "";
                        Day1Grid.Items.Insert(i, moviesDay1[streammingIndex][i]);
                    }
                }
            }


            if (moviesDay2[streammingIndex].Count == 0)
                return;
            for (int i = 0; i < moviesDay2[streammingIndex].Count; i++)
            {
                Day2Grid.Items.Remove(moviesDay2[streammingIndex][i]);
                moviesDay2[streammingIndex][i].MovieTime = timeString + "\n" + dateString;
                Day2Grid.Items.Insert(i, moviesDay2[streammingIndex][i]);
                int timeSeconds = ConvertTimeToSeconds(timeString);
                int movieSeconds = ConvertTimeToSeconds(moviesDay2[streammingIndex][i].Duration);
                int delaySeconds = ConvertTimeToSeconds(moviesDay2[streammingIndex][i].Delay);
                timeString = ConvertSecondsToTime(timeSeconds + movieSeconds + delaySeconds);
                int newTimeHours = int.Parse(timeString.Substring(0, 2));
                if (newTimeHours >= 24)
                {
                    daysToAdd++;
                    int newTimeSeconds = ConvertTimeToSeconds(timeString);
                    int daySeconds = ConvertTimeToSeconds("24:00:00");
                    timeString = ConvertSecondsToTime(newTimeSeconds - daySeconds);
                }
                dateString = date.AddDays(daysToAdd).ToString("M/d/yyyy");
            }
            if (channels[streammingIndex].channelSchedules.schedule_day2.Substring(1, 2).Equals("12"))
            {
                for (int i = 0; i < moviesDay2[streammingIndex].Count; i++)
                {
                    Day2Grid.Items.Remove(moviesDay2[streammingIndex][i]);
                    moviesDay2[streammingIndex][i].MovieTime2 = timeString + "\n" + dateString;
                    Day2Grid.Items.Insert(i, moviesDay2[streammingIndex][i]);
                    int timeSeconds = ConvertTimeToSeconds(timeString);
                    int movieSeconds = ConvertTimeToSeconds(moviesDay2[streammingIndex][i].Duration);
                    int delaySeconds = ConvertTimeToSeconds(moviesDay2[streammingIndex][i].Delay);
                    timeString = ConvertSecondsToTime(timeSeconds + movieSeconds + delaySeconds);
                    int newTimeHours = int.Parse(timeString.Substring(0, 2));
                    if (newTimeHours >= 24)
                    {
                        daysToAdd++;
                        int newTimeSeconds = ConvertTimeToSeconds(timeString);
                        int daySeconds = ConvertTimeToSeconds("24:00:00");
                        timeString = ConvertSecondsToTime(newTimeSeconds - daySeconds);
                    }
                    dateString = date.AddDays(daysToAdd).ToString("M/d/yyyy");
                }
            }
            else
            {
                for (int i = 0; i < moviesDay2[streammingIndex].Count; i++)
                {
                    Day2Grid.Items.Remove(moviesDay2[streammingIndex][i]);
                    moviesDay2[streammingIndex][i].MovieTime2 = "";
                    Day2Grid.Items.Insert(i, moviesDay2[streammingIndex][i]);
                }
            }

            if (moviesDay3[streammingIndex].Count == 0)
                return;
            for (int i = 0; i < moviesDay3[streammingIndex].Count; i++)
            {
                Day3Grid.Items.Remove(moviesDay3[streammingIndex][i]);
                moviesDay3[streammingIndex][i].MovieTime = timeString + "\n" + dateString;
                Day3Grid.Items.Insert(i, moviesDay3[streammingIndex][i]);
                int timeSeconds = ConvertTimeToSeconds(timeString);
                int movieSeconds = ConvertTimeToSeconds(moviesDay3[streammingIndex][i].Duration);
                int delaySeconds = ConvertTimeToSeconds(moviesDay3[streammingIndex][i].Delay);
                timeString = ConvertSecondsToTime(timeSeconds + movieSeconds + delaySeconds);
                int newTimeHours = int.Parse(timeString.Substring(0, 2));
                if (newTimeHours >= 24)
                {
                    daysToAdd++;
                    int newTimeSeconds = ConvertTimeToSeconds(timeString);
                    int daySeconds = ConvertTimeToSeconds("24:00:00");
                    timeString = ConvertSecondsToTime(newTimeSeconds - daySeconds);
                }
                dateString = date.AddDays(daysToAdd).ToString("M/d/yyyy");
            }
            if (channels[streammingIndex].channelSchedules.schedule_day3.Substring(1, 2).Equals("12"))
            {
                for (int i = 0; i < moviesDay3[streammingIndex].Count; i++)
                {
                    Day3Grid.Items.Remove(moviesDay3[streammingIndex][i]);
                    moviesDay3[streammingIndex][i].MovieTime2 = timeString + "\n" + dateString;
                    Day3Grid.Items.Insert(i, moviesDay3[streammingIndex][i]);
                    int timeSeconds = ConvertTimeToSeconds(timeString);
                    int movieSeconds = ConvertTimeToSeconds(moviesDay3[streammingIndex][i].Duration);
                    int delaySeconds = ConvertTimeToSeconds(moviesDay3[streammingIndex][i].Delay);
                    timeString = ConvertSecondsToTime(timeSeconds + movieSeconds + delaySeconds);
                    int newTimeHours = int.Parse(timeString.Substring(0, 2));
                    if (newTimeHours >= 24)
                    {
                        daysToAdd++;
                        int newTimeSeconds = ConvertTimeToSeconds(timeString);
                        int daySeconds = ConvertTimeToSeconds("24:00:00");
                        timeString = ConvertSecondsToTime(newTimeSeconds - daySeconds);
                    }
                    dateString = date.AddDays(daysToAdd).ToString("M/d/yyyy");
                }
            }
            else
            {
                for (int i = 0; i < moviesDay3[streammingIndex].Count; i++)
                {
                    Day3Grid.Items.Remove(moviesDay3[streammingIndex][i]);
                    moviesDay3[streammingIndex][i].MovieTime2 = "";
                    Day3Grid.Items.Insert(i, moviesDay3[streammingIndex][i]);
                }
            }

            if (moviesDay4[streammingIndex].Count == 0)
                return;
            for (int i = 0; i < moviesDay4[streammingIndex].Count; i++)
            {
                Day4Grid.Items.Remove(moviesDay4[streammingIndex][i]);
                moviesDay4[streammingIndex][i].MovieTime = timeString + "\n" + dateString;
                Day4Grid.Items.Insert(i, moviesDay4[streammingIndex][i]);
                int timeSeconds = ConvertTimeToSeconds(timeString);
                int movieSeconds = ConvertTimeToSeconds(moviesDay4[streammingIndex][i].Duration);
                int delaySeconds = ConvertTimeToSeconds(moviesDay4[streammingIndex][i].Delay);
                timeString = ConvertSecondsToTime(timeSeconds + movieSeconds + delaySeconds);
                int newTimeHours = int.Parse(timeString.Substring(0, 2));
                if (newTimeHours >= 24)
                {
                    daysToAdd++;
                    int newTimeSeconds = ConvertTimeToSeconds(timeString);
                    int daySeconds = ConvertTimeToSeconds("24:00:00");
                    timeString = ConvertSecondsToTime(newTimeSeconds - daySeconds);
                }
                dateString = date.AddDays(daysToAdd).ToString("M/d/yyyy");
            }
            if (channels[streammingIndex].channelSchedules.schedule_day4.Substring(1, 2).Equals("12"))
            {
                for (int i = 0; i < moviesDay4[streammingIndex].Count; i++)
                {
                    Day4Grid.Items.Remove(moviesDay4[streammingIndex][i]);
                    moviesDay4[streammingIndex][i].MovieTime2 = timeString + "\n" + dateString;
                    Day4Grid.Items.Insert(i, moviesDay4[streammingIndex][i]);
                    int timeSeconds = ConvertTimeToSeconds(timeString);
                    int movieSeconds = ConvertTimeToSeconds(moviesDay4[streammingIndex][i].Duration);
                    int delaySeconds = ConvertTimeToSeconds(moviesDay4[streammingIndex][i].Delay);
                    timeString = ConvertSecondsToTime(timeSeconds + movieSeconds + delaySeconds);
                    int newTimeHours = int.Parse(timeString.Substring(0, 2));
                    if (newTimeHours >= 24)
                    {
                        daysToAdd++;
                        int newTimeSeconds = ConvertTimeToSeconds(timeString);
                        int daySeconds = ConvertTimeToSeconds("24:00:00");
                        timeString = ConvertSecondsToTime(newTimeSeconds - daySeconds);
                    }
                    dateString = date.AddDays(daysToAdd).ToString("M/d/yyyy");
                }
            }
            else
            {
                for (int i = 0; i < moviesDay4[streammingIndex].Count; i++)
                {
                    Day4Grid.Items.Remove(moviesDay4[streammingIndex][i]);
                    moviesDay4[streammingIndex][i].MovieTime2 = "";
                    Day4Grid.Items.Insert(i, moviesDay4[streammingIndex][i]);
                }
            }

            if (moviesDay5[streammingIndex].Count == 0)
                return;
            for (int i = 0; i < moviesDay5[streammingIndex].Count; i++)
            {
                Day5Grid.Items.Remove(moviesDay5[streammingIndex][i]);
                moviesDay5[streammingIndex][i].MovieTime = timeString + "\n" + dateString;
                Day5Grid.Items.Insert(i, moviesDay5[streammingIndex][i]);
                int timeSeconds = ConvertTimeToSeconds(timeString);
                int movieSeconds = ConvertTimeToSeconds(moviesDay5[streammingIndex][i].Duration);
                int delaySeconds = ConvertTimeToSeconds(moviesDay5[streammingIndex][i].Delay);
                timeString = ConvertSecondsToTime(timeSeconds + movieSeconds + delaySeconds);
                int newTimeHours = int.Parse(timeString.Substring(0, 2));
                if (newTimeHours >= 24)
                {
                    daysToAdd++;
                    int newTimeSeconds = ConvertTimeToSeconds(timeString);
                    int daySeconds = ConvertTimeToSeconds("24:00:00");
                    timeString = ConvertSecondsToTime(newTimeSeconds - daySeconds);
                }
                dateString = date.AddDays(daysToAdd).ToString("M/d/yyyy");
            }
            if (channels[streammingIndex].channelSchedules.schedule_day5.Substring(1, 2).Equals("12"))
            {
                for (int i = 0; i < moviesDay5[streammingIndex].Count; i++)
                {
                    Day5Grid.Items.Remove(moviesDay5[streammingIndex][i]);
                    moviesDay5[streammingIndex][i].MovieTime2 = timeString + "\n" + dateString;
                    Day5Grid.Items.Insert(i, moviesDay5[streammingIndex][i]);
                    int timeSeconds = ConvertTimeToSeconds(timeString);
                    int movieSeconds = ConvertTimeToSeconds(moviesDay5[streammingIndex][i].Duration);
                    int delaySeconds = ConvertTimeToSeconds(moviesDay5[streammingIndex][i].Delay);
                    timeString = ConvertSecondsToTime(timeSeconds + movieSeconds + delaySeconds);
                    int newTimeHours = int.Parse(timeString.Substring(0, 2));
                    if (newTimeHours >= 24)
                    {
                        daysToAdd++;
                        int newTimeSeconds = ConvertTimeToSeconds(timeString);
                        int daySeconds = ConvertTimeToSeconds("24:00:00");
                        timeString = ConvertSecondsToTime(newTimeSeconds - daySeconds);
                    }
                    dateString = date.AddDays(daysToAdd).ToString("M/d/yyyy");
                }
            }
            else
            {
                for (int i = 0; i < moviesDay5[streammingIndex].Count; i++)
                {
                    Day5Grid.Items.Remove(moviesDay5[streammingIndex][i]);
                    moviesDay5[streammingIndex][i].MovieTime2 = "";
                    Day5Grid.Items.Insert(i, moviesDay5[streammingIndex][i]);
                }
            }

            if (moviesDay6[streammingIndex].Count == 0)
                return;
            for (int i = 0; i < moviesDay6[streammingIndex].Count; i++)
            {
                Day6Grid.Items.Remove(moviesDay6[streammingIndex][i]);
                moviesDay6[streammingIndex][i].MovieTime = timeString + "\n" + dateString;
                Day6Grid.Items.Insert(i, moviesDay6[streammingIndex][i]);
                int timeSeconds = ConvertTimeToSeconds(timeString);
                int movieSeconds = ConvertTimeToSeconds(moviesDay6[streammingIndex][i].Duration);
                int delaySeconds = ConvertTimeToSeconds(moviesDay6[streammingIndex][i].Delay);
                timeString = ConvertSecondsToTime(timeSeconds + movieSeconds + delaySeconds);
                int newTimeHours = int.Parse(timeString.Substring(0, 2));
                if (newTimeHours >= 24)
                {
                    daysToAdd++;
                    int newTimeSeconds = ConvertTimeToSeconds(timeString);
                    int daySeconds = ConvertTimeToSeconds("24:00:00");
                    timeString = ConvertSecondsToTime(newTimeSeconds - daySeconds);
                }
                dateString = date.AddDays(daysToAdd).ToString("M/d/yyyy");
            }
            if (channels[streammingIndex].channelSchedules.schedule_day6.Substring(1, 2).Equals("12"))
            {
                for (int i = 0; i < moviesDay6[streammingIndex].Count; i++)
                {
                    Day6Grid.Items.Remove(moviesDay6[streammingIndex][i]);
                    moviesDay6[streammingIndex][i].MovieTime2 = timeString + "\n" + dateString;
                    Day6Grid.Items.Insert(i, moviesDay6[streammingIndex][i]);
                    int timeSeconds = ConvertTimeToSeconds(timeString);
                    int movieSeconds = ConvertTimeToSeconds(moviesDay6[streammingIndex][i].Duration);
                    int delaySeconds = ConvertTimeToSeconds(moviesDay6[streammingIndex][i].Delay);
                    timeString = ConvertSecondsToTime(timeSeconds + movieSeconds + delaySeconds);
                    int newTimeHours = int.Parse(timeString.Substring(0, 2));
                    if (newTimeHours >= 24)
                    {
                        daysToAdd++;
                        int newTimeSeconds = ConvertTimeToSeconds(timeString);
                        int daySeconds = ConvertTimeToSeconds("24:00:00");
                        timeString = ConvertSecondsToTime(newTimeSeconds - daySeconds);
                    }
                    dateString = date.AddDays(daysToAdd).ToString("M/d/yyyy");
                }
            }
            else
            {
                for (int i = 0; i < moviesDay6[streammingIndex].Count; i++)
                {
                    Day6Grid.Items.Remove(moviesDay6[streammingIndex][i]);
                    moviesDay6[streammingIndex][i].MovieTime2 = "";
                    Day6Grid.Items.Insert(i, moviesDay6[streammingIndex][i]);
                }
            }
            if (moviesDay7[streammingIndex].Count == 0)
                return;
            for (int i = 0; i < moviesDay7[streammingIndex].Count; i++)
            {
                Day7Grid.Items.Remove(moviesDay7[streammingIndex][i]);
                moviesDay7[streammingIndex][i].MovieTime = timeString + "\n" + dateString;
                Day7Grid.Items.Insert(i, moviesDay7[streammingIndex][i]);
                int timeSeconds = ConvertTimeToSeconds(timeString);
                int movieSeconds = ConvertTimeToSeconds(moviesDay7[streammingIndex][i].Duration);
                int delaySeconds = ConvertTimeToSeconds(moviesDay7[streammingIndex][i].Delay);
                timeString = ConvertSecondsToTime(timeSeconds + movieSeconds + delaySeconds);
                int newTimeHours = int.Parse(timeString.Substring(0, 2));
                if (newTimeHours >= 24)
                {
                    daysToAdd++;
                    int newTimeSeconds = ConvertTimeToSeconds(timeString);
                    int daySeconds = ConvertTimeToSeconds("24:00:00");
                    timeString = ConvertSecondsToTime(newTimeSeconds - daySeconds);
                }
                dateString = date.AddDays(daysToAdd).ToString("M/d/yyyy");
            }
            if (channels[streammingIndex].channelSchedules.schedule_day7.Substring(1, 2).Equals("12"))
            {
                for (int i = 0; i < moviesDay7[streammingIndex].Count; i++)
                {
                    Day7Grid.Items.Remove(moviesDay7[streammingIndex][i]);
                    moviesDay7[streammingIndex][i].MovieTime2 = timeString + "\n" + dateString;
                    Day7Grid.Items.Insert(i, moviesDay7[streammingIndex][i]);
                    int timeSeconds = ConvertTimeToSeconds(timeString);
                    int movieSeconds = ConvertTimeToSeconds(moviesDay7[streammingIndex][i].Duration);
                    int delaySeconds = ConvertTimeToSeconds(moviesDay7[streammingIndex][i].Delay);
                    timeString = ConvertSecondsToTime(timeSeconds + movieSeconds + delaySeconds);
                    int newTimeHours = int.Parse(timeString.Substring(0, 2));
                    if (newTimeHours >= 24)
                    {
                        daysToAdd++;
                        int newTimeSeconds = ConvertTimeToSeconds(timeString);
                        int daySeconds = ConvertTimeToSeconds("24:00:00");
                        timeString = ConvertSecondsToTime(newTimeSeconds - daySeconds);
                    }
                    dateString = date.AddDays(daysToAdd).ToString("M/d/yyyy");
                }
            }
            else
            {
                for (int i = 0; i < moviesDay7[streammingIndex].Count; i++)
                {
                    Day7Grid.Items.Remove(moviesDay7[streammingIndex][i]);
                    moviesDay7[streammingIndex][i].MovieTime2 = "";
                    Day7Grid.Items.Insert(i, moviesDay7[streammingIndex][i]);
                }
            }
        }

        private void SetDatesOfMovies_Click(object sender, RoutedEventArgs e)
        {
            if (channels[channelIndex].channel_state.Equals("off"))
            {
                if (channels[channelIndex].channelOptions.startDate.Length > 0 && channels[channelIndex].channelOptions.startTime.Length > 0)
                {
                    string[] dateString = channels[channelIndex].channelOptions.startDate.Split('/');
                    int day = int.Parse(dateString[1]);
                    int month = int.Parse(dateString[0]);
                    int year = int.Parse(dateString[2]);
                    DateTime date = new DateTime(year, month, day);

                    string[] timeString = channels[channelIndex].channelOptions.startTime.Split(':');
                    int hour = int.Parse(timeString[0]);
                    int minute = int.Parse(timeString[1]);
                    int second = int.Parse(timeString[2]);
                    DateTime time = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, minute, second);

                    DateTime fullDate = new DateTime(year, month, day, hour, minute, second);
                    TimeSpan difference = fullDate - DateTime.Now;
                    // إذا كان التاريخ أكبر من الحالي
                    if (difference.TotalDays > 0)
                    {
                        CalculateDateForAllMovies(channelIndex, date, time, 0);
                    }
                    else
                    {
                        MessageBox.Show("The Date is Old");
                    }
                }
                else
                {
                    MessageBox.Show("There is on Specific Date ");
                }
            }
            else
            {
                int runIndex = GetRunMovieIndexAnd12HourState();
                string durationOfRunningMovie = (string)nextMovieDuration.Content;
                TimeSpan timeToNextMovieRun = TimeSpan.Parse(durationOfRunningMovie);
                DateTime date = DateTime.Now.Add(timeToNextMovieRun);
                DateTime time = DateTime.Now.Add(timeToNextMovieRun);
                CalculateDateForAllMovies(channelIndex, date, time, runIndex + 1);
            }
        }

        public int GetRunMovieIndexAnd12HourState()
        {
            for (int i = 0; i < moviesDay1[channelIndex].Count; i++)
            {
                if (moviesDay1[channelIndex][i].IsFinished.Equals("Icons/running_movie.png"))
                {
                    return i;
                }
            }
            return 0;
        }

        private void DefaultSchedules_Click(object sender, RoutedEventArgs e)
        {
            int streammingIndex = channelIndex;
            if (channels[streammingIndex].channel_state.Equals("off"))
            {
                moviesDay1[streammingIndex].Clear();
                if (channels[streammingIndex].channelSchedules.schedule_day1.Substring(4).Length > 0)
                {
                    string[] tokens = channels[streammingIndex].channelSchedules.schedule_day1.Substring(4).Split('%');
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        string[] movieInfo = tokens[i].Split('&');
                        Movie movie = new Movie();
                        movie.Name = movieInfo[0];
                        movie.Duration = movieInfo[1];
                        movie.Delay = movieInfo[2];
                        movie.Directory = movieInfo[3];
                        moviesDay1[streammingIndex].Add(movie);
                    }
                }
                //schedule options (day1)
                string scheduleFormatDay1 = channels[streammingIndex].channelSchedules.schedule_day1;
                string totalHoursDay1 = scheduleFormatDay1.Substring(1, 2);
                string scheduleDay1 = scheduleFormatDay1.Substring(4);
                if (totalHoursDay1.Equals("12"))
                {
                    scheduleDay1Hour12.IsChecked = true;
                    if (scheduleDay1.Length == 0)
                    {
                        scheduleDay1ElapsedTime.Content = ZEROHOURS;
                        scheduleDay1RestTime.Content = TWELVEHOURS;
                    }
                    else
                    {
                        scheduleDay1ElapsedTime.Content = TWELVEHOURS;
                        scheduleDay1RestTime.Content = ZEROHOURS;
                    }
                }
                else
                {
                    scheduleDay1Hour24.IsChecked = true;
                    if (scheduleDay1.Length == 0)
                    {
                        scheduleDay1ElapsedTime.Content = ZEROHOURS;
                        scheduleDay1RestTime.Content = FORTEENHOURS;
                    }
                    else
                    {
                        scheduleDay1ElapsedTime.Content = FORTEENHOURS;
                        scheduleDay1RestTime.Content = ZEROHOURS;
                    }
                }
                scheduleDay1OutTime.Visibility = Visibility.Hidden;
                scheduleDay1ElapsedTime.Foreground = Brushes.Black;
                scheduleDay1RestTime.Foreground = Brushes.Black;
            }

            for (int i = 0; i < moviesDay1[streammingIndex].Count; i++)
            {
                moviesDay1[streammingIndex][i].MovieTime = String.Empty;
                moviesDay1[streammingIndex][i].MovieTime2 = String.Empty;
            }
            for (int i = 0; i < moviesDay2[streammingIndex].Count; i++)
            {
                moviesDay2[streammingIndex][i].MovieTime = String.Empty;
                moviesDay2[streammingIndex][i].MovieTime2 = String.Empty;
            }
            for (int i = 0; i < moviesDay3[streammingIndex].Count; i++)
            {
                moviesDay3[streammingIndex][i].MovieTime = String.Empty;
                moviesDay3[streammingIndex][i].MovieTime2 = String.Empty;
            }
            for (int i = 0; i < moviesDay4[streammingIndex].Count; i++)
            {
                moviesDay4[streammingIndex][i].MovieTime = String.Empty;
                moviesDay4[streammingIndex][i].MovieTime2 = String.Empty;
            }
            for (int i = 0; i < moviesDay5[streammingIndex].Count; i++)
            {
                moviesDay5[streammingIndex][i].MovieTime = String.Empty;
                moviesDay5[streammingIndex][i].MovieTime2 = String.Empty;
            }
            for (int i = 0; i < moviesDay6[streammingIndex].Count; i++)
            {
                moviesDay6[streammingIndex][i].MovieTime = String.Empty;
                moviesDay6[streammingIndex][i].MovieTime2 = String.Empty;
            }
            for (int i = 0; i < moviesDay7[streammingIndex].Count; i++)
            {
                moviesDay7[streammingIndex][i].MovieTime = String.Empty;
                moviesDay7[streammingIndex][i].MovieTime2 = String.Empty;
            }

            FillDataGridWithMovies(moviesDay1[streammingIndex], 1);
            FillDataGridWithMovies(moviesDay2[streammingIndex], 2);
            FillDataGridWithMovies(moviesDay3[streammingIndex], 3);
            FillDataGridWithMovies(moviesDay4[streammingIndex], 4);
            FillDataGridWithMovies(moviesDay5[streammingIndex], 5);
            FillDataGridWithMovies(moviesDay6[streammingIndex], 6);
            FillDataGridWithMovies(moviesDay7[streammingIndex], 7);

            //schedule options (day2)
            string scheduleFormatDay2 = channels[streammingIndex].channelSchedules.schedule_day2;
            string totalHoursDay2 = scheduleFormatDay2.Substring(1, 2);
            string scheduleDay2 = scheduleFormatDay2.Substring(4);
            if (totalHoursDay2.Equals("12"))
            {
                scheduleDay2Hour12.IsChecked = true;
                if (scheduleDay2.Length == 0)
                {
                    scheduleDay2ElapsedTime.Content = ZEROHOURS;
                    scheduleDay2RestTime.Content = TWELVEHOURS;
                }
                else
                {
                    scheduleDay2ElapsedTime.Content = TWELVEHOURS;
                    scheduleDay2RestTime.Content = ZEROHOURS;
                }
            }
            else
            {
                scheduleDay2Hour24.IsChecked = true;
                if (scheduleDay2.Length == 0)
                {
                    scheduleDay2ElapsedTime.Content = ZEROHOURS;
                    scheduleDay2RestTime.Content = FORTEENHOURS;
                }
                else
                {
                    scheduleDay2ElapsedTime.Content = FORTEENHOURS;
                    scheduleDay2RestTime.Content = ZEROHOURS;
                }
            }
            scheduleDay2OutTime.Visibility = Visibility.Hidden;
            scheduleDay2ElapsedTime.Foreground = Brushes.Black;
            scheduleDay2RestTime.Foreground = Brushes.Black;

            //schedule options (day3)
            string scheduleFormatDay3 = channels[streammingIndex].channelSchedules.schedule_day3;
            string totalHoursDay3 = scheduleFormatDay3.Substring(1, 2);
            string scheduleDay3 = scheduleFormatDay3.Substring(4);
            if (totalHoursDay3.Equals("12"))
            {
                scheduleDay3Hour12.IsChecked = true;
                if (scheduleDay3.Length == 0)
                {
                    scheduleDay3ElapsedTime.Content = ZEROHOURS;
                    scheduleDay3RestTime.Content = TWELVEHOURS;
                }
                else
                {
                    scheduleDay3ElapsedTime.Content = TWELVEHOURS;
                    scheduleDay3RestTime.Content = ZEROHOURS;
                }
            }
            else
            {
                scheduleDay3Hour24.IsChecked = true;
                if (scheduleDay3.Length == 0)
                {
                    scheduleDay3ElapsedTime.Content = ZEROHOURS;
                    scheduleDay3RestTime.Content = FORTEENHOURS;
                }
                else
                {
                    scheduleDay3ElapsedTime.Content = FORTEENHOURS;
                    scheduleDay3RestTime.Content = ZEROHOURS;
                }
            }
            scheduleDay3OutTime.Visibility = Visibility.Hidden;
            scheduleDay3ElapsedTime.Foreground = Brushes.Black;
            scheduleDay3RestTime.Foreground = Brushes.Black;

            //schedule options (day4)
            string scheduleFormatDay4 = channels[streammingIndex].channelSchedules.schedule_day4;
            string totalHoursDay4 = scheduleFormatDay4.Substring(1, 2);
            string scheduleDay4 = scheduleFormatDay4.Substring(4);
            if (totalHoursDay4.Equals("12"))
            {
                scheduleDay4Hour12.IsChecked = true;
                if (scheduleDay4.Length == 0)
                {
                    scheduleDay4ElapsedTime.Content = ZEROHOURS;
                    scheduleDay4RestTime.Content = TWELVEHOURS;
                }
                else
                {
                    scheduleDay4ElapsedTime.Content = TWELVEHOURS;
                    scheduleDay4RestTime.Content = ZEROHOURS;
                }
            }
            else
            {
                scheduleDay4Hour24.IsChecked = true;
                if (scheduleDay4.Length == 0)
                {
                    scheduleDay4ElapsedTime.Content = ZEROHOURS;
                    scheduleDay4RestTime.Content = FORTEENHOURS;
                }
                else
                {
                    scheduleDay4ElapsedTime.Content = FORTEENHOURS;
                    scheduleDay4RestTime.Content = ZEROHOURS;
                }
            }
            scheduleDay4OutTime.Visibility = Visibility.Hidden;
            scheduleDay4ElapsedTime.Foreground = Brushes.Black;
            scheduleDay4RestTime.Foreground = Brushes.Black;

            //schedule options (day5)
            string scheduleFormatDay5 = channels[streammingIndex].channelSchedules.schedule_day5;
            string totalHoursDay5 = scheduleFormatDay5.Substring(1, 2);
            string scheduleDay5 = scheduleFormatDay5.Substring(4);
            if (totalHoursDay5.Equals("12"))
            {
                scheduleDay5Hour12.IsChecked = true;
                if (scheduleDay5.Length == 0)
                {
                    scheduleDay5ElapsedTime.Content = ZEROHOURS;
                    scheduleDay5RestTime.Content = TWELVEHOURS;
                }
                else
                {
                    scheduleDay5ElapsedTime.Content = TWELVEHOURS;
                    scheduleDay5RestTime.Content = ZEROHOURS;
                }
            }
            else
            {
                scheduleDay5Hour24.IsChecked = true;
                if (scheduleDay5.Length == 0)
                {
                    scheduleDay5ElapsedTime.Content = ZEROHOURS;
                    scheduleDay5RestTime.Content = FORTEENHOURS;
                }
                else
                {
                    scheduleDay5ElapsedTime.Content = FORTEENHOURS;
                    scheduleDay5RestTime.Content = ZEROHOURS;
                }
            }
            scheduleDay5OutTime.Visibility = Visibility.Hidden;
            scheduleDay5ElapsedTime.Foreground = Brushes.Black;
            scheduleDay5RestTime.Foreground = Brushes.Black;

            //schedule options (day6)
            string scheduleFormatDay6 = channels[streammingIndex].channelSchedules.schedule_day6;
            string totalHoursDay6 = scheduleFormatDay6.Substring(1, 2);
            string scheduleDay6 = scheduleFormatDay6.Substring(4);
            if (totalHoursDay6.Equals("12"))
            {
                scheduleDay6Hour12.IsChecked = true;
                if (scheduleDay6.Length == 0)
                {
                    scheduleDay6ElapsedTime.Content = ZEROHOURS;
                    scheduleDay6RestTime.Content = TWELVEHOURS;
                }
                else
                {
                    scheduleDay6ElapsedTime.Content = TWELVEHOURS;
                    scheduleDay6RestTime.Content = ZEROHOURS;
                }
            }
            else
            {
                scheduleDay6Hour24.IsChecked = true;
                if (scheduleDay6.Length == 0)
                {
                    scheduleDay6ElapsedTime.Content = ZEROHOURS;
                    scheduleDay6RestTime.Content = FORTEENHOURS;
                }
                else
                {
                    scheduleDay6ElapsedTime.Content = FORTEENHOURS;
                    scheduleDay6RestTime.Content = ZEROHOURS;
                }
            }
            scheduleDay6OutTime.Visibility = Visibility.Hidden;
            scheduleDay6ElapsedTime.Foreground = Brushes.Black;
            scheduleDay6RestTime.Foreground = Brushes.Black;

            //schedule options (day7)
            string scheduleFormatDay7 = channels[streammingIndex].channelSchedules.schedule_day7;
            string totalHoursDay7 = scheduleFormatDay7.Substring(1, 2);
            string scheduleDay7 = scheduleFormatDay7.Substring(4);
            if (totalHoursDay7.Equals("12"))
            {
                scheduleDay7Hour12.IsChecked = true;
                if (scheduleDay7.Length == 0)
                {
                    scheduleDay7ElapsedTime.Content = ZEROHOURS;
                    scheduleDay7RestTime.Content = TWELVEHOURS;
                }
                else
                {
                    scheduleDay7ElapsedTime.Content = TWELVEHOURS;
                    scheduleDay7RestTime.Content = ZEROHOURS;
                }
            }
            else
            {
                scheduleDay7Hour24.IsChecked = true;
                if (scheduleDay7.Length == 0)
                {
                    scheduleDay7ElapsedTime.Content = ZEROHOURS;
                    scheduleDay7RestTime.Content = FORTEENHOURS;
                }
                else
                {
                    scheduleDay7ElapsedTime.Content = FORTEENHOURS;
                    scheduleDay7RestTime.Content = ZEROHOURS;
                }
            }
            scheduleDay7OutTime.Visibility = Visibility.Hidden;
            scheduleDay7ElapsedTime.Foreground = Brushes.Black;
            scheduleDay7RestTime.Foreground = Brushes.Black;

            scheduleDay1Save_btn.IsEnabled = false;
            scheduleDay2Save_btn.IsEnabled = false;
            scheduleDay3Save_btn.IsEnabled = false;
            scheduleDay4Save_btn.IsEnabled = false;
            scheduleDay5Save_btn.IsEnabled = false;
            scheduleDay6Save_btn.IsEnabled = false;
            scheduleDay7Save_btn.IsEnabled = false;
        }


        private void ExportSchedulesInfo_Click(object sender, RoutedEventArgs e)
        {
            //setDate_btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "Choose The Directory Of Your Movies";
            dlg.IsFolderPicker = true;
            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                try
                {
                    int yIndex = 0;
                    Boolean ok = true;
                    string targetPath = dlg.FileName;
                    PdfDocument pdf = new PdfDocument();
                    pdf.Info.Title = "My First PDF";
                    PdfPage pdfPage = pdf.AddPage();
                    XGraphics graph = XGraphics.FromPdfPage(pdfPage);
                    XFont font1 = new XFont("Verdana", 20, XFontStyle.Bold);
                    XFont font2 = new XFont("Verdana", 18, XFontStyle.Bold);
                    XFont font3 = new XFont("Verdana", 14, XFontStyle.Regular);
                    graph.DrawString("Dates of movies schedule", font1, XBrushes.Red, new XRect(0, 0, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopCenter);
                    yIndex += 50;
                    if (moviesDay1[channelIndex].Count == 0)
                        ok = false;
                    if (ok)
                    {
                        graph.DrawString("Day1:", font2, XBrushes.Black, new XRect(0, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                        yIndex += 30;
                        for (int i = 0; i < moviesDay1[channelIndex].Count; i++)
                        {
                            if(yIndex+60>700)
                            {
                                pdfPage = pdf.AddPage();
                                graph = XGraphics.FromPdfPage(pdfPage);
                                yIndex = 50;
                            }
                            graph.DrawString((i+1).ToString()+"-", font3, XBrushes.Black, new XRect(0, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Movie Name :", font3, XBrushes.Red, new XRect(20, yIndex , pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay1[channelIndex][i].Name, font3, XBrushes.Black, new XRect(130, yIndex , pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Duration :", font3, XBrushes.Red, new XRect(20, 20+yIndex , pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay1[channelIndex][i].Duration, font3, XBrushes.Black, new XRect(130, 20 + yIndex , pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Firet Time :", font3, XBrushes.Red, new XRect(20, 40 + yIndex , pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay1[channelIndex][i].MovieTime, font3, XBrushes.Black, new XRect(130, 40+yIndex , pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Second Time :", font3, XBrushes.Red, new XRect(320, 40 + yIndex , pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay1[channelIndex][i].MovieTime2, font3, XBrushes.Black, new XRect(440, 40 + yIndex , pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            yIndex += 60;
                        }
                    }
                    else
                    {
                        pdf.Save(targetPath + @"\movies schedule.pdf");
                        return;
                    }
                    //50 is destance detween days 
                    //30 is Day1 word
                    //20 is line length
                    if (yIndex + 100 > 700)
                    {
                        pdfPage = pdf.AddPage();
                        graph = XGraphics.FromPdfPage(pdfPage);
                        yIndex = 50;
                    }
                    //Day2
                    yIndex += 50;
                    if (moviesDay2[channelIndex].Count == 0)
                        ok = false;
                    if (ok)
                    {
                        graph.DrawString("Day2:", font2, XBrushes.Black, new XRect(0, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                        yIndex += 30;
                        for (int i = 0; i < moviesDay2[channelIndex].Count; i++)
                        {
                            if (yIndex + 60 > 700)
                            {
                                pdfPage = pdf.AddPage();
                                graph = XGraphics.FromPdfPage(pdfPage);
                                yIndex = 50;
                            }
                            graph.DrawString((i + 1).ToString() + "-", font3, XBrushes.Black, new XRect(0, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Movie Name :", font3, XBrushes.Red, new XRect(20, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay2[channelIndex][i].Name, font3, XBrushes.Black, new XRect(130, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Duration :", font3, XBrushes.Red, new XRect(20, 20 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay2[channelIndex][i].Duration, font3, XBrushes.Black, new XRect(130, 20 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Firet Time :", font3, XBrushes.Red, new XRect(20, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay2[channelIndex][i].MovieTime, font3, XBrushes.Black, new XRect(130, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Second Time :", font3, XBrushes.Red, new XRect(320, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay2[channelIndex][i].MovieTime2, font3, XBrushes.Black, new XRect(440, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            yIndex += 60;
                        }
                    }
                    else
                    {
                        pdf.Save(targetPath + @"\movies schedule.pdf");
                        return;
                    }
                    if (yIndex + 100 > 700)
                    {
                        pdfPage = pdf.AddPage();
                        graph = XGraphics.FromPdfPage(pdfPage);
                        yIndex = 50;
                    }
                    //Day3
                    yIndex += 50;
                    if (moviesDay3[channelIndex].Count == 0)
                        ok = false;
                    if (ok)
                    {
                        graph.DrawString("Day3:", font2, XBrushes.Black, new XRect(0, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                        yIndex += 30;
                        for (int i = 0; i < moviesDay3[channelIndex].Count; i++)
                        {
                            if (yIndex + 60 > 700)
                            {
                                pdfPage = pdf.AddPage();
                                graph = XGraphics.FromPdfPage(pdfPage);
                                yIndex = 50;
                            }
                            graph.DrawString((i + 1).ToString() + "-", font3, XBrushes.Black, new XRect(0, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Movie Name :", font3, XBrushes.Red, new XRect(20, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay3[channelIndex][i].Name, font3, XBrushes.Black, new XRect(130, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Duration :", font3, XBrushes.Red, new XRect(20, 20 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay3[channelIndex][i].Duration, font3, XBrushes.Black, new XRect(130, 20 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Firet Time :", font3, XBrushes.Red, new XRect(20, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay3[channelIndex][i].MovieTime, font3, XBrushes.Black, new XRect(130, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Second Time :", font3, XBrushes.Red, new XRect(320, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay3[channelIndex][i].MovieTime2, font3, XBrushes.Black, new XRect(440, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            yIndex += 60;
                        }
                    }
                    else
                    {
                        pdf.Save(targetPath + @"\movies schedule.pdf");
                        return;
                    }
                    if (yIndex + 100 > 700)
                    {
                        pdfPage = pdf.AddPage();
                        graph = XGraphics.FromPdfPage(pdfPage);
                        yIndex = 50;
                    }
                    //Day4
                    yIndex += 50;
                    if (moviesDay4[channelIndex].Count == 0)
                        ok = false;
                    if (ok)
                    {
                        graph.DrawString("Day4:", font2, XBrushes.Black, new XRect(0, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                        yIndex += 30;
                        for (int i = 0; i < moviesDay4[channelIndex].Count; i++)
                        {
                            if (yIndex + 60 > 700)
                            {
                                pdfPage = pdf.AddPage();
                                graph = XGraphics.FromPdfPage(pdfPage);
                                yIndex = 50;
                            }
                            graph.DrawString((i + 1).ToString() + "-", font3, XBrushes.Black, new XRect(0, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Movie Name :", font3, XBrushes.Red, new XRect(20, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay4[channelIndex][i].Name, font3, XBrushes.Black, new XRect(130, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Duration :", font3, XBrushes.Red, new XRect(20, 20 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay4[channelIndex][i].Duration, font3, XBrushes.Black, new XRect(130, 20 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Firet Time :", font3, XBrushes.Red, new XRect(20, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay4[channelIndex][i].MovieTime, font3, XBrushes.Black, new XRect(130, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Second Time :", font3, XBrushes.Red, new XRect(320, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay4[channelIndex][i].MovieTime2, font3, XBrushes.Black, new XRect(440, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            yIndex += 60;
                        }
                    }
                    else
                    {
                        pdf.Save(targetPath + @"\movies schedule.pdf");
                        return;
                    }
                    if (yIndex + 100 > 700)
                    {
                        pdfPage = pdf.AddPage();
                        graph = XGraphics.FromPdfPage(pdfPage);
                        yIndex = 50;
                    }
                    //Day5
                    yIndex += 50;
                    if (moviesDay5[channelIndex].Count == 0)
                        ok = false;
                    if (ok)
                    {
                        graph.DrawString("Day5:", font2, XBrushes.Black, new XRect(0, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                        yIndex += 30;
                        for (int i = 0; i < moviesDay5[channelIndex].Count; i++)
                        {
                            if (yIndex + 60 > 700)
                            {
                                pdfPage = pdf.AddPage();
                                graph = XGraphics.FromPdfPage(pdfPage);
                                yIndex = 50;
                            }
                            graph.DrawString((i + 1).ToString() + "-", font3, XBrushes.Black, new XRect(0, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Movie Name :", font3, XBrushes.Red, new XRect(20, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay5[channelIndex][i].Name, font3, XBrushes.Black, new XRect(130, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Duration :", font3, XBrushes.Red, new XRect(20, 20 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay5[channelIndex][i].Duration, font3, XBrushes.Black, new XRect(130, 20 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Firet Time :", font3, XBrushes.Red, new XRect(20, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay5[channelIndex][i].MovieTime, font3, XBrushes.Black, new XRect(130, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Second Time :", font3, XBrushes.Red, new XRect(320, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay5[channelIndex][i].MovieTime2, font3, XBrushes.Black, new XRect(440, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            yIndex += 60;
                        }
                    }
                    else
                    {
                        pdf.Save(targetPath + @"\movies schedule.pdf");
                        return;
                    }
                    if (yIndex + 100 > 700)
                    {
                        pdfPage = pdf.AddPage();
                        graph = XGraphics.FromPdfPage(pdfPage);
                        yIndex = 50;
                    }
                    //Day6
                    yIndex += 50;
                    if (moviesDay6[channelIndex].Count == 0)
                        ok = false;
                    if (ok)
                    {
                        graph.DrawString("Day6:", font2, XBrushes.Black, new XRect(0, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                        yIndex += 30;
                        for (int i = 0; i < moviesDay6[channelIndex].Count; i++)
                        {
                            if (yIndex + 60 > 700)
                            {
                                pdfPage = pdf.AddPage();
                                graph = XGraphics.FromPdfPage(pdfPage);
                                yIndex = 50;
                            }
                            graph.DrawString((i + 1).ToString() + "-", font3, XBrushes.Black, new XRect(0, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Movie Name :", font3, XBrushes.Red, new XRect(20, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay6[channelIndex][i].Name, font3, XBrushes.Black, new XRect(130, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Duration :", font3, XBrushes.Red, new XRect(20, 20 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay6[channelIndex][i].Duration, font3, XBrushes.Black, new XRect(130, 20 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Firet Time :", font3, XBrushes.Red, new XRect(20, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay6[channelIndex][i].MovieTime, font3, XBrushes.Black, new XRect(130, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Second Time :", font3, XBrushes.Red, new XRect(320, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay6[channelIndex][i].MovieTime2, font3, XBrushes.Black, new XRect(440, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            yIndex += 60;
                        }
                    }
                    else
                    {
                        pdf.Save(targetPath + @"\movies schedule.pdf");
                        return;
                    }
                    if (yIndex + 100 > 700)
                    {
                        pdfPage = pdf.AddPage();
                        graph = XGraphics.FromPdfPage(pdfPage);
                        yIndex = 50;
                    }
                    //Day7
                    yIndex += 50;
                    if (moviesDay7[channelIndex].Count == 0)
                        ok = false;
                    if (ok)
                    {
                        graph.DrawString("Day7:", font2, XBrushes.Black, new XRect(0, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                        yIndex += 30;
                        for (int i = 0; i < moviesDay7[channelIndex].Count; i++)
                        {
                            if (yIndex + 60 > 700)
                            {
                                pdfPage = pdf.AddPage();
                                graph = XGraphics.FromPdfPage(pdfPage);
                                yIndex = 50;
                            }
                            graph.DrawString((i + 1).ToString() + "-", font3, XBrushes.Black, new XRect(0, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Movie Name :", font3, XBrushes.Red, new XRect(20, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay7[channelIndex][i].Name, font3, XBrushes.Black, new XRect(130, yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Duration :", font3, XBrushes.Red, new XRect(20, 20 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay7[channelIndex][i].Duration, font3, XBrushes.Black, new XRect(130, 20 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Firet Time :", font3, XBrushes.Red, new XRect(20, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay7[channelIndex][i].MovieTime, font3, XBrushes.Black, new XRect(130, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString("Second Time :", font3, XBrushes.Red, new XRect(320, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            graph.DrawString(moviesDay7[channelIndex][i].MovieTime2, font3, XBrushes.Black, new XRect(440, 40 + yIndex, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
                            yIndex += 60;
                        }
                    }
                    else
                    {
                        pdf.Save(targetPath + @"\movies schedule.pdf");
                        return;
                    }
                    pdf.Save(targetPath + @"\movies schedule.pdf");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("The file is in use");
                }
            }
        }

        #endregion


        #region enter and leave and focus events

        private void TextBoxChannelOption_TextChanged(object sender, RoutedEventArgs e)
        {
            channel_save_btn.IsEnabled = true;
        }

        private void TextBoxLogoOption_TextChanged(object sender, RoutedEventArgs e)
        {
            logo_save_btn.IsEnabled = true;
        }

        private void TextBoxSubtitleOption_TextChanged(object sender, RoutedEventArgs e)
        {
            subtitle_save_btn.IsEnabled = true;
        }

        private void NewChannel_Enter(object sender, RoutedEventArgs e)
        {
            addChannel.Opacity = 0.7;
            addChannel2.Opacity = 0.7;
        }

        private void NewChannel_Leave(object sender, RoutedEventArgs e)
        {
            addChannel.Opacity = 1.0;
            addChannel2.Opacity = 1.0;
        }

        private void window1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            grid1.Focus();
        }

        #endregion
        

        #region Ecryption & Decryption

        private void FillSettings()
        {
            Properties.Settings.Default.BIOSserNo = HardwareInfo.GetBIOSserNo();
            Properties.Settings.Default.HDDSerialNo = HardwareInfo.GetHDDSerialNo();
            Properties.Settings.Default.MACAddress = HardwareInfo.GetMACAddress();
            Properties.Settings.Default.ProcessorId = HardwareInfo.GetProcessorId();
            Properties.Settings.Default.Save();
        }

        private void getHardWareInfo()
        {
            try
            {
                string[] info = { HardwareInfo.GetBIOSserNo(),
                               HardwareInfo.GetHDDSerialNo(),
                               HardwareInfo.GetMACAddress(),
                               HardwareInfo.GetProcessorId()};

                string mydocpath =
               Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                using (StreamWriter outputFile = new StreamWriter(mydocpath + @"\License.lic"))
                {
                    foreach (string line in info)
                    {
                        ezcrypt1.Reset();
                        ezcrypt1.Algorithm = EzcryptAlgorithms.ezAES;
                        ezcrypt1.UseHex = true;
                        ezcrypt1.KeyPassword = "password";
                        ezcrypt1.InputMessage = line;
                        ezcrypt1.Encrypt();
                        outputFile.WriteLine(ezcrypt1.OutputMessage);
                    }

                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void ShowImportDialog()
        {
            var dialog = new ImportDialog();
            if (dialog.ShowDialog() == true)
            {
                string importPath = dialog.ImportPath;
                CheckImportedFile(importPath);
            }
        }

        private void CheckImportedFile(string importedPath)
        {
            try
            {
                string[] liness = File.ReadAllLines(importedPath);
                string[] afterDecrypt = new string[liness.Length];
                for (int i = 0; i < liness.Length; i++)
                {
                    afterDecrypt[i] = DecryptMessage("TripleDES", liness[i]);

                }
                if (afterDecrypt[0].Equals(Properties.Settings.Default.BIOSserNo) &&
                        afterDecrypt[1].Equals(Properties.Settings.Default.HDDSerialNo) &&
                        afterDecrypt[2].Equals(Properties.Settings.Default.MACAddress) &&
                        afterDecrypt[3].Equals(Properties.Settings.Default.ProcessorId)
                       )
                {
                    Properties.Settings.Default.NoChannel = afterDecrypt[4];
                    Properties.Settings.Default.Save();
                    if (File.Exists("database.db"))
                    {
                        File.Delete("database.db");
                    }
                    InitializeDigitalClock();
                    CreateDatabaseForChannels();
                    GetChannelsInfo();
                    InsertChannelsInChannelsGrid();
                    InsertItemsInChannelInterface(channelIndex);
                }
                else
                {
                    MessageBox.Show("The file invalid please try again!");
                    ShowImportDialog();
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
                ShowImportDialog();
            }

        }

        private string DecryptMessage(string algo, string inputMessag)
        {
            ezcrypt1.Reset();
            switch (algo)
            {
                case "AES":
                    ezcrypt1.Algorithm = EzcryptAlgorithms.ezAES;
                    break;
                case "TripleDES":
                    ezcrypt1.Algorithm = EzcryptAlgorithms.ezTripleDES;
                    break;
            }
            ezcrypt1.UseHex = true;
            ezcrypt1.KeyPassword = "password";
            ezcrypt1.InputMessage = inputMessag;
            ezcrypt1.Decrypt();
            return ezcrypt1.OutputMessage;
        }

        #endregion


        #region About

        private void AboutMouseEnter(object sender, RoutedEventArgs e)
        {
            aboutText.Foreground = new SolidColorBrush(Colors.Red);
        }

        private void AboutMouseLeave(object sender, RoutedEventArgs e)
        {
            aboutText.Foreground = new SolidColorBrush(Colors.White);
        }

        private void AboutMouseDown(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        #endregion


        //this function used to make textBox accept only numbers
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        void DataWindow_Closing(object sender, CancelEventArgs e)
        {
            windwoClose = true;
            try
            {
                for (int i = 0; i < channels.Count; i++)
                {
                    if (channels[i].channelExtraInfo.process != null)
                    {
                        if (Process.GetProcesses().Any(x => x.Id == channels[i].channelExtraInfo.process.Id))
                        {
                            channels[i].channelExtraInfo.process.Kill();
                        }
                    }
                }
            }
            catch (Exception ex) { };
        }
    }
}