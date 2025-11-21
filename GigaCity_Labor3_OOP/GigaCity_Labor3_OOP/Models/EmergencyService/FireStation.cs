using System.Collections.ObjectModel;
using System.ComponentModel;

namespace GigaCity_Labor3_OOP.Models.EmergencyService
{
    public class FireStation : INotifyPropertyChanged
    {
        private ObservableCollection<FireFighterVehicle> _vehicles;

        public string Name { get; set; }
        public int XCoordinate { get; set; }
        public int YCoordinate { get; set; }
        public int Capacity { get; set; } // Вместимость (количество техники)
        public int PersonnelCount { get; set; } // Количество персонала

        public ObservableCollection<FireFighterVehicle> Vehicles
        {
            get => _vehicles ??= new ObservableCollection<FireFighterVehicle>();
            set
            {
                _vehicles = value;
                OnPropertyChanged(nameof(Vehicles));
            }
        }

        public FireStation()
        {
            Vehicles = new ObservableCollection<FireFighterVehicle>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

