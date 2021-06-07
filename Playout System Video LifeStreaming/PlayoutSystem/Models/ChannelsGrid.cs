using System;
using System.ComponentModel;

namespace PlayoutSystem.Models
{
    public class ChannelsGrid : INotifyPropertyChanged
    {
        private string channelName = String.Empty;
        private string isDeleted = "Icons/deleteChannel.png";
        private string isEdited = "Icons/editChannel.png";
        public event PropertyChangedEventHandler PropertyChanged;

        public string ChannelName
        {
            get { return channelName; }
            set
            {
                channelName = value;
                OnPropertyChanged("ChannelName");
            }
        }

        public string IsDeleted
        {
            get { return isDeleted; }
            set
            {
                isDeleted = value;
                OnPropertyChanged("IsDeleted");
            }
        }

        public string IsEdited
        {
            get { return isEdited; }
            set
            {
                isEdited = value;
                OnPropertyChanged("IsEdited");
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
