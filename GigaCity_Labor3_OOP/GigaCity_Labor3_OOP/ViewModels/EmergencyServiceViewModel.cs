using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using GigaCity_Labor3_OOP.Models;
using GigaCity_Labor3_OOP.Models.EmergencyService;

namespace GigaCity_Labor3_OOP.ViewModels
{
    public class EmergencyServiceViewModel : INotifyPropertyChanged
    {
        private readonly MapModel _map;
        private readonly Random _random = new Random();
        private DispatcherTimer _disasterTimer;
        private DispatcherTimer _responseTimer;
        private string _statusMessage;

        public ObservableCollection<FireStation> FireStations { get; private set; }
        public ObservableCollection<Disaster> Disasters { get; private set; }
        public ObservableCollection<EmergencyCall> EmergencyCalls { get; private set; }
        public ObservableCollection<FireFighterVehicle> AllVehicles { get; private set; }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public int VehiclesOnCallCount => AllVehicles.Count(v => v.CurrentState == VehicleState.OnCall);

        public EmergencyServiceViewModel(MapModel map)
        {
            _map = map;
            FireStations = new ObservableCollection<FireStation>();
            Disasters = new ObservableCollection<Disaster>();
            EmergencyCalls = new ObservableCollection<EmergencyCall>();
            AllVehicles = new ObservableCollection<FireFighterVehicle>();

            InitializeDefaultStations();
            StartTimers();
        }

        private void InitializeDefaultStations()
        {
            // Создаем первую пожарную станцию
            var station1 = new FireStation
            {
                Name = "Пожарная станция №1",
                XCoordinate = 30 * 15,
                YCoordinate = 30 * 15,
                Capacity = 5,
                PersonnelCount = 20
            };

            station1.Vehicles.Add(new FireEngine { Name = "Пожарная машина #1" });
            station1.Vehicles.Add(new FireEngine { Name = "Пожарная машина #2" });
            station1.Vehicles.Add(new LadderTruck { Name = "Автолестница #1" });
            station1.Vehicles.Add(new Ambulance { Name = "Скорая помощь #1" });

            foreach (var vehicle in station1.Vehicles)
            {
                AllVehicles.Add(vehicle);
            }

            FireStations.Add(station1);

            // Создаем вторую пожарную станцию
            var station2 = new FireStation
            {
                Name = "Пожарная станция №2",
                XCoordinate = 70 * 15,
                YCoordinate = 70 * 15,
                Capacity = 4,
                PersonnelCount = 15
            };

            station2.Vehicles.Add(new FireEngine { Name = "Пожарная машина #3" });
            station2.Vehicles.Add(new LadderTruck { Name = "Автолестница #2" });
            station2.Vehicles.Add(new Ambulance { Name = "Скорая помощь #2" });

            foreach (var vehicle in station2.Vehicles)
            {
                AllVehicles.Add(vehicle);
            }

            FireStations.Add(station2);
        }

        private void StartTimers()
        {
            // Таймер для создания случайных происшествий
            _disasterTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _disasterTimer.Tick += DisasterTimer_Tick;
            _disasterTimer.Start();

            // Таймер для обновления состояния происшествий и техники
            _responseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _responseTimer.Tick += ResponseTimer_Tick;
            _responseTimer.Start();
        }

        private void DisasterTimer_Tick(object sender, EventArgs e)
        {
            // Создаем случайное происшествие с вероятностью 20%
            if (_random.Next(100) < 20)
            {
                CreateRandomDisaster();
            }
        }

        private void CreateRandomDisaster()
        {
            // Выбираем случайную позицию на карте
            int x = _random.Next(_map.Width) * 15;
            int y = _random.Next(_map.Height) * 15;

            var fire = new Fire
            {
                XCoordinate = x,
                YCoordinate = y,
                Intensity = _random.Next(30, 100),
                Severity = _random.Next(30, 100),
                BuildingType = "Жилое здание"
            };

            Disasters.Add(fire);
            StatusMessage = $"Новое происшествие: Пожар на координатах ({x / 15}, {y / 15})";

            // Автоматически отправляем ближайшую технику
            DispatchNearestVehicles(fire);
        }

        private void DispatchNearestVehicles(Disaster disaster)
        {
            FireStation nearestStation = null;
            double minDistance = double.MaxValue;

            foreach (var station in FireStations)
            {
                double distance = Math.Sqrt(
                    Math.Pow(station.XCoordinate - disaster.XCoordinate, 2) +
                    Math.Pow(station.YCoordinate - disaster.YCoordinate, 2));

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestStation = station;
                }
            }

            if (nearestStation != null)
            {
                var suitableVehicles = nearestStation.Vehicles
                    .Where(v => v.CurrentState == VehicleState.InGarage && v.IsSuitableFor(disaster.Type))
                    .Take(2)
                    .ToList();

                foreach (var vehicle in suitableVehicles)
                {
                    vehicle.CurrentState = VehicleState.OnCall;
                    vehicle.CurrentLocation = $"На пути к происшествию ({disaster.XCoordinate / 15}, {disaster.YCoordinate / 15})";
                    StatusMessage = $"Отправлена техника {vehicle.Name} на происшествие {disaster.Type}";
                }
            }
        }

        private void ResponseTimer_Tick(object sender, EventArgs e)
        {
            // Обновляем состояние происшествий
            foreach (var disaster in Disasters.ToList())
            {
                if (disaster is Fire fire)
                {
                    // Уменьшаем интенсивность пожара, если техника на месте
                    var vehiclesOnScene = AllVehicles
                        .Where(v => v.CurrentState == VehicleState.OnCall)
                        .Count();

                    if (vehiclesOnScene > 0)
                    {
                        fire.Intensity = Math.Max(0, fire.Intensity - 2);
                        fire.Severity = fire.Intensity;

                        if (fire.Intensity <= 0)
                        {
                            fire.IsActive = false;
                            StatusMessage = $"Пожар потушен на координатах ({fire.XCoordinate / 15}, {fire.YCoordinate / 15})";

                            // Возвращаем технику в гараж
                            var vehiclesToReturn = AllVehicles
                                .Where(v => v.CurrentState == VehicleState.OnCall)
                                .ToList();

                            foreach (var vehicle in vehiclesToReturn)
                            {
                                vehicle.CurrentState = VehicleState.Returning;
                                vehicle.CurrentLocation = "Возвращается на станцию";
                            }

                            // Удаляем потушенный пожар через 5 секунд
                            var timer = new DispatcherTimer
                            {
                                Interval = TimeSpan.FromSeconds(5)
                            };
                            timer.Tick += (s, args) =>
                            {
                                Disasters.Remove(fire);
                                timer.Stop();
                            };
                            timer.Start();
                        }
                    }
                    else
                    {
                        // Если техника не на месте, пожар усиливается
                        fire.Intensity = Math.Min(100, fire.Intensity + 1);
                        fire.Severity = fire.Intensity;
                    }
                }
            }

            // Обновляем состояние техники
            foreach (var vehicle in AllVehicles)
            {
                if (vehicle.CurrentState == VehicleState.Returning)
                {
                    // Через некоторое время техника возвращается в гараж
                    var timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(3)
                    };
                    timer.Tick += (s, args) =>
                    {
                        vehicle.CurrentState = VehicleState.InGarage;
                        vehicle.CurrentLocation = "В гараже";
                        timer.Stop();
                    };
                    timer.Start();
                }
            }

            OnPropertyChanged(nameof(VehiclesOnCallCount));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

