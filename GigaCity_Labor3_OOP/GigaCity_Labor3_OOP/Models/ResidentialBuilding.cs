using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace GigaCity_Labor3_OOP.Models
{
    public enum HealthStatus
    {
        Healthy,
        Sick
    }

    public class Resident : INotifyPropertyChanged
    {
        private HealthStatus _healthStatus;
        private double _sicknessTimer;
        private bool _hasCalledAmbulance;

        public string Id { get; set; }
        public string Name { get; set; }
        
        public HealthStatus HealthStatus
        {
            get => _healthStatus;
            set
            {
                _healthStatus = value;
                OnPropertyChanged(nameof(HealthStatus));
                OnPropertyChanged(nameof(IsSick));
            }
        }

        public double SicknessTimer
        {
            get => _sicknessTimer;
            set
            {
                _sicknessTimer = value;
                OnPropertyChanged(nameof(SicknessTimer));
            }
        }

        public bool HasCalledAmbulance
        {
            get => _hasCalledAmbulance;
            set
            {
                _hasCalledAmbulance = value;
                OnPropertyChanged(nameof(HasCalledAmbulance));
            }
        }

        public bool IsSick => HealthStatus == HealthStatus.Sick;

        public Resident()
        {
            Id = Guid.NewGuid().ToString();
            Name = $"Житель {Id.Substring(0, 8)}";
            HealthStatus = HealthStatus.Healthy;
            SicknessTimer = 0;
            HasCalledAmbulance = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ResidentialBuilding : INotifyPropertyChanged
    {
        private ObservableCollection<Resident> _residents;
        private int _maxCapacity;
        private bool _hasSickResident;

        public string Id { get; set; }
        public string Name { get; set; }
        public double XCoordinate { get; set; }
        public double YCoordinate { get; set; }
        public int Width { get; set; } = 2; // Размер дома в клетках
        public int Height { get; set; } = 2;

        public ObservableCollection<Resident> Residents
        {
            get => _residents ??= new ObservableCollection<Resident>();
            set
            {
                _residents = value;
                OnPropertyChanged(nameof(Residents));
                OnPropertyChanged(nameof(ResidentCount));
                OnPropertyChanged(nameof(OccupancyText));
                UpdateSickStatus();
            }
        }

        public int MaxCapacity
        {
            get => _maxCapacity;
            set
            {
                _maxCapacity = value;
                OnPropertyChanged(nameof(MaxCapacity));
                OnPropertyChanged(nameof(OccupancyText));
            }
        }

        public bool HasSickResident
        {
            get => _hasSickResident;
            private set
            {
                _hasSickResident = value;
                OnPropertyChanged(nameof(HasSickResident));
            }
        }

        public int ResidentCount => Residents.Count;
        public int FreeSpots => MaxCapacity - Residents.Count;
        public bool HasFreeSpace => Residents.Count < MaxCapacity;
        public string OccupancyText => $"Жители: {Residents.Count}/{MaxCapacity}";

        public ResidentialBuilding()
        {
            Id = Guid.NewGuid().ToString();
            Name = $"Дом {Id.Substring(0, 8)}";
            Residents = new ObservableCollection<Resident>();
            Residents.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(ResidentCount));
                OnPropertyChanged(nameof(FreeSpots));
                OnPropertyChanged(nameof(HasFreeSpace));
                OnPropertyChanged(nameof(OccupancyText));
                UpdateSickStatus();
            };
        }

        private void UpdateSickStatus()
        {
            HasSickResident = Residents.Any(r => r.IsSick && !r.HasCalledAmbulance);
        }

        public void AddResident(Resident resident)
        {
            if (HasFreeSpace)
            {
                Residents.Add(resident);
                resident.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(Resident.HealthStatus) || e.PropertyName == nameof(Resident.HasCalledAmbulance))
                    {
                        UpdateSickStatus();
                    }
                };
            }
        }

        public void RemoveResident(Resident resident)
        {
            Residents.Remove(resident);
        }

        public Resident GetSickResident()
        {
            return Residents.FirstOrDefault(r => r.IsSick && !r.HasCalledAmbulance);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

