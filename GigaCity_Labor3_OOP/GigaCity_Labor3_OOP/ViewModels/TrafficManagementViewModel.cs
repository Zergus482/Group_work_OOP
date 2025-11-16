using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GigaCity_Labor3_OOP.Models;
using GigaCity_Labor3_OOP.Services;
using CityFinance.ViewModels;

namespace GigaCity_Labor3_OOP.ViewModels
{
    public class TrafficManagementViewModel : INotifyPropertyChanged
    {
        private readonly TrafficSimulationService _trafficSimulationService;
        private ObservableCollection<RoadViewModel> _roads;
        private ObservableCollection<VehicleViewModel> _vehicles;
        private bool _isSimulationRunning;
        private int _simulationSpeed;
        private readonly Dictionary<(int, int), RoadViewModel> _roadLookup;

        public ObservableCollection<RoadViewModel> Roads
        {
            get => _roads;
            set { _roads = value; OnPropertyChanged(); }
        }

        public ObservableCollection<VehicleViewModel> Vehicles
        {
            get => _vehicles;
            set { _vehicles = value; OnPropertyChanged(); }
        }

        public bool IsSimulationRunning
        {
            get => _isSimulationRunning;
            set { _isSimulationRunning = value; OnPropertyChanged(); }
        }

        public int SimulationSpeed
        {
            get => _simulationSpeed;
            set { _simulationSpeed = value; OnPropertyChanged(); }
        }

        public ICommand StartSimulationCommand { get; }
        public ICommand StopSimulationCommand { get; }
        public ICommand AddVehicleCommand { get; }
        public ICommand BuildRoadCommand { get; }

        public TrafficManagementViewModel(TrafficSimulationService trafficSimulationService, HashSet<(int, int)> roadCoordinates)
        {
            _trafficSimulationService = trafficSimulationService;

            Roads = new ObservableCollection<RoadViewModel>();
            Vehicles = new ObservableCollection<VehicleViewModel>();
            _roadLookup = new Dictionary<(int, int), RoadViewModel>();
            IsSimulationRunning = false;
            SimulationSpeed = 1;

            foreach ((int x, int y) in roadCoordinates)
            {
                var roadModel = new RoadModel { X = x, Y = y, SpeedLimit = 60, IsOneWay = false };
                var roadViewModel = new RoadViewModel(roadModel);
                Roads.Add(roadViewModel);
                _roadLookup[(x, y)] = roadViewModel;
            }

            StartSimulationCommand = new RelayCommand(StartSimulation);
            StopSimulationCommand = new RelayCommand(StopSimulation);
            AddVehicleCommand = new RelayCommand(AddVehicle);

            _trafficSimulationService.VehicleMoved += OnVehicleMoved;
            _trafficSimulationService.VehicleAdded += OnVehicleAdded;
        }

        private void StartSimulation(object parameter)
        {
            if (!IsSimulationRunning)
            {
                IsSimulationRunning = true;
                _trafficSimulationService.StartSimulation(SimulationSpeed);
            }
        }

        private void StopSimulation(object parameter)
        {
            if (IsSimulationRunning)
            {
                IsSimulationRunning = false;
                _trafficSimulationService.StopSimulation();
            }
        }

        private void AddVehicle(object parameter)
        {
            if (parameter is string vehicleTypeString && Enum.TryParse<VehicleType>(vehicleTypeString, true, out var vehicleType))
            {
                _trafficSimulationService.AddVehicle(vehicleType);
            }
        }

        public void AddVehicleOnRoute(VehicleType type, int startX, int startY, int endX, int endY)
        {
            _trafficSimulationService.AddVehicleOnRoute(type, startX, startY, endX, endY);
        }

        private void OnVehicleMoved(object sender, VehicleMovedEventArgs e)
        {
            var vehicleViewModel = Vehicles.FirstOrDefault(v => v.Id == e.VehicleId);
            if (vehicleViewModel != null)
            {
                vehicleViewModel.CurrentX = e.NewX;
                vehicleViewModel.CurrentY = e.NewY;
                vehicleViewModel.MoveProgress = e.Progress;
            }

            // Обновляем загруженность дорог здесь
            UpdateRoadTraffic();
        }

        private void OnVehicleAdded(object sender, VehicleAddedEventArgs e)
        {
            var vehicleViewModel = new VehicleViewModel(e.Vehicle);
            Vehicles.Add(vehicleViewModel);
        }

        private void UpdateRoadTraffic()
        {
            foreach (var road in Roads) road.TrafficLevel = 0;

            var trafficCount = Vehicles
                .Where(v => v.IsMoving)
                .GroupBy(v => (v.CurrentX, v.CurrentY))
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var kvp in trafficCount)
            {
                if (_roadLookup.TryGetValue(kvp.Key, out var roadViewModel))
                {
                    roadViewModel.TrafficLevel = kvp.Value * 10;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}