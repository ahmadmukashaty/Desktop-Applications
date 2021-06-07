using System;
using System.ComponentModel;

namespace PlayoutSystem.Models
{
    public class Movie : INotifyPropertyChanged
    {
        private string name = String.Empty;

        private string duration = String.Empty;

        private string delay = "00:00:00";

        private string directory = String.Empty;

        private string movieTime = String.Empty;

        private string movieTime2 = String.Empty;

        private string isFinished = "Icons/no.png";

        public event PropertyChangedEventHandler PropertyChanged;

        public Movie(Movie movie)
        {
            Duration = movie.Duration;
            Name = movie.Name;
            Directory = movie.Directory;
        }

        public Movie()
        {

        }

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        public string Duration
        {
            get { return duration; }
            set
            {
                duration = value;
                OnPropertyChanged("Duration");
            }
        }

        public string Delay
        {
            get { return delay; }
            set
            {
                delay = value;
                OnPropertyChanged("Delay");
            }
        }

        public string Directory
        {
            get { return directory; }
            set
            {
                directory = value;
                OnPropertyChanged("Directory");
            }
        }

        public string MovieTime
        {
            get { return movieTime; }
            set
            {
                movieTime = value;
                OnPropertyChanged("MovieTime");
            }
        }

        public string MovieTime2
        {
            get { return movieTime2; }
            set
            {
                movieTime2 = value;
                OnPropertyChanged("MovieTime2");
            }
        }


        public string IsFinished
        {
            get { return isFinished; }
            set
            {
                isFinished = value;
                OnPropertyChanged("IsFinished");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
