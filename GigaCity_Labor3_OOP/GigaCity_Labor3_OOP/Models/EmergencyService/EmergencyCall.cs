using System;
using System.ComponentModel;

namespace GigaCity_Labor3_OOP.Models.EmergencyService
{
    public class EmergencyCall : INotifyPropertyChanged
    {
        private bool _isResponded;

        public string Id { get; set; }
        public Disaster Disaster { get; set; }
        public FireStation AssignedStation { get; set; }
        public DateTime CallTime { get; set; }
        public DateTime? ResponseTime { get; set; }

        public bool IsResponded
        {
            get => _isResponded;
            set
            {
                _isResponded = value;
                OnPropertyChanged(nameof(IsResponded));
            }
        }

        public EmergencyCall()
        {
            Id = Guid.NewGuid().ToString();
            CallTime = DateTime.Now;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

