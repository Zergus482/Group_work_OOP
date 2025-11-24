using System;
using System.ComponentModel;

namespace GigaCity_Labor3_OOP.Models.EmergencyService
{
    public class MedicalEmergencyCall : INotifyPropertyChanged
    {
        private bool _isResponded;
        private bool _isCompleted;
        private double _xCoordinate;
        private double _yCoordinate;
        private DateTime _callTime;

        public string Id { get; set; }
        
        public double XCoordinate
        {
            get => _xCoordinate;
            set
            {
                _xCoordinate = value;
                OnPropertyChanged(nameof(XCoordinate));
                OnPropertyChanged(nameof(GridX));
            }
        }
        
        public double YCoordinate
        {
            get => _yCoordinate;
            set
            {
                _yCoordinate = value;
                OnPropertyChanged(nameof(YCoordinate));
                OnPropertyChanged(nameof(GridY));
            }
        }
        
        public DateTime CallTime
        {
            get => _callTime;
            set
            {
                _callTime = value;
                OnPropertyChanged(nameof(CallTime));
            }
        }
        
        // Grid координаты для отображения (целые числа)
        public int GridX => (int)(XCoordinate / 15.0);
        public int GridY => (int)(YCoordinate / 15.0);
        
        public DateTime? ResponseTime { get; set; }
        public AmbulanceVehicle AssignedAmbulance { get; set; }

        public bool IsResponded
        {
            get => _isResponded;
            set
            {
                _isResponded = value;
                OnPropertyChanged(nameof(IsResponded));
            }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                _isCompleted = value;
                OnPropertyChanged(nameof(IsCompleted));
            }
        }

        public MedicalEmergencyCall()
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

