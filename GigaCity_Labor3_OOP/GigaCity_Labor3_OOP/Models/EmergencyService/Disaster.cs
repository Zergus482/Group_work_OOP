using System;
using System.ComponentModel;

namespace GigaCity_Labor3_OOP.Models.EmergencyService
{
    public abstract class Disaster : INotifyPropertyChanged
    {
        private double _severity;
        private bool _isActive;

        public string Id { get; set; }
        public EmergencyType Type { get; set; }
        public int XCoordinate { get; set; }
        public int YCoordinate { get; set; }
        public DateTime ReportedAt { get; set; }

        public double Severity
        {
            get => _severity;
            set
            {
                _severity = value;
                OnPropertyChanged(nameof(Severity));
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged(nameof(IsActive));
            }
        }

        public Disaster()
        {
            Id = Guid.NewGuid().ToString();
            ReportedAt = DateTime.Now;
            IsActive = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

