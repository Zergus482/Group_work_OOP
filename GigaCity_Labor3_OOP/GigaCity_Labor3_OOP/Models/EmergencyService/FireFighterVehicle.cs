using System;
using System.ComponentModel;

namespace GigaCity_Labor3_OOP.Models.EmergencyService
{
    public abstract class FireFighterVehicle : INotifyPropertyChanged
    {
        private VehicleState _currentState;
        private string _currentLocation;

        public string Name { get; set; }
        public string Type { get; set; }
        public int Capacity { get; set; }
        public double Speed { get; set; } // км/ч
        public bool IsSuitableForFire { get; set; }
        public bool IsSuitableForRescue { get; set; }
        public bool IsSuitableForMedical { get; set; }

        public VehicleState CurrentState
        {
            get => _currentState;
            set
            {
                _currentState = value;
                OnPropertyChanged(nameof(CurrentState));
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public string CurrentLocation
        {
            get => _currentLocation;
            set
            {
                _currentLocation = value;
                OnPropertyChanged(nameof(CurrentLocation));
            }
        }

        public string StatusText
        {
            get
            {
                return CurrentState switch
                {
                    VehicleState.InGarage => "В гараже",
                    VehicleState.OnCall => "На вызове",
                    VehicleState.Returning => "Возвращается",
                    VehicleState.Maintenance => "На обслуживании",
                    _ => "Неизвестно"
                };
            }
        }

        public virtual bool IsSuitableFor(EmergencyType type)
        {
            return type switch
            {
                EmergencyType.Fire => IsSuitableForFire,
                EmergencyType.Rescue => IsSuitableForRescue,
                EmergencyType.Medical => IsSuitableForMedical,
                _ => false
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

