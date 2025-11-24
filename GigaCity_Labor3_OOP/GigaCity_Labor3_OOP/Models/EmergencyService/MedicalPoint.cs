using System.Collections.ObjectModel;
using System.ComponentModel;

namespace GigaCity_Labor3_OOP.Models.EmergencyService
{
    public class MedicalPoint : INotifyPropertyChanged
    {
        private ObservableCollection<AmbulanceVehicle> _ambulances;
        private ObservableCollection<Patient> _patients;
        private int _occupiedRooms;

        public string Name { get; set; }
        public double XCoordinate { get; set; }
        public double YCoordinate { get; set; }
        public int MaxRooms { get; set; } = 5; // Максимальное количество палат

        public ObservableCollection<AmbulanceVehicle> Ambulances
        {
            get => _ambulances ??= new ObservableCollection<AmbulanceVehicle>();
            set
            {
                _ambulances = value;
                OnPropertyChanged(nameof(Ambulances));
                OnPropertyChanged(nameof(AmbulanceCount));
            }
        }

        public ObservableCollection<Patient> Patients
        {
            get => _patients ??= new ObservableCollection<Patient>();
            set
            {
                _patients = value;
                OnPropertyChanged(nameof(Patients));
            }
        }

        public int OccupiedRooms
        {
            get => _occupiedRooms;
            set
            {
                _occupiedRooms = value;
                OnPropertyChanged(nameof(OccupiedRooms));
                OnPropertyChanged(nameof(PatientCountText));
            }
        }

        public string PatientCountText => $"Пациенты: {OccupiedRooms}/{MaxRooms}";

        public bool HasFreeRoom => OccupiedRooms < MaxRooms;

        public int AmbulanceCount => Ambulances.Count;

        public MedicalPoint()
        {
            Ambulances = new ObservableCollection<AmbulanceVehicle>();
            Patients = new ObservableCollection<Patient>();
            
            // Подписываемся на изменения коллекции машин для обновления счетчика
            Ambulances.CollectionChanged += (s, e) => OnPropertyChanged(nameof(AmbulanceCount));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

