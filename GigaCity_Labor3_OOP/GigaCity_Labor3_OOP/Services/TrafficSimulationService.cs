using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GigaCity_Labor3_OOP.Models;

namespace GigaCity_Labor3_OOP.Services
{
    public class TrafficSimulationService
    {
        private readonly HashSet<(int, int)> _roadCoordinates;
        private List<VehicleModel> _vehicles;
        private PathFindingService _pathFindingService;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _simulationTask;
        private int _simulationSpeed;
        private Random _random;

        public event EventHandler<VehicleMovedEventArgs> VehicleMoved;
        public event EventHandler<VehicleAddedEventArgs> VehicleAdded;

        public TrafficSimulationService(HashSet<(int, int)> roadCoordinates)
        {
            _roadCoordinates = roadCoordinates;
            _vehicles = new List<VehicleModel>();
            _random = new Random();
            _pathFindingService = new PathFindingService(_roadCoordinates);
        }

        public void StartSimulation(int speed)
        {
            if (_simulationTask != null && !_simulationTask.IsCompleted)
                return;

            _simulationSpeed = speed;
            _cancellationTokenSource = new CancellationTokenSource();
            _simulationTask = Task.Run(() => SimulationLoop(_cancellationTokenSource.Token));
        }

        public void StopSimulation()
        {
            _cancellationTokenSource?.Cancel();
        }

        public VehicleModel AddVehicle(VehicleType type)
        {
            var vehicle = CreateVehicle(type);
            var roadCellsList = _roadCoordinates.ToList();
            if (roadCellsList.Count == 0) return null;

            var startCell = roadCellsList[_random.Next(roadCellsList.Count)];
            var endCell = roadCellsList[_random.Next(roadCellsList.Count)];

            var path = _pathFindingService.FindPath(startCell.Item1, startCell.Item2, endCell.Item1, endCell.Item2);

            if (path.Count > 0)
            {
                vehicle.CurrentX = path[0].X;
                vehicle.CurrentY = path[0].Y;
                vehicle.DestinationX = path[path.Count - 1].X;
                vehicle.DestinationY = path[path.Count - 1].Y;
                vehicle.IsMoving = true;

                _vehicles.Add(vehicle);
                VehicleAdded?.Invoke(this, new VehicleAddedEventArgs { Vehicle = vehicle });
                return vehicle;
            }
            return null;
        }

        private async Task SimulationLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var vehicle in _vehicles.Where(v => v.IsMoving))
                {
                    UpdateVehiclePosition(vehicle);
                }

                await Task.Delay(1000 / _simulationSpeed, cancellationToken);
            }
        }

        private void UpdateVehiclePosition(VehicleModel vehicle)
        {
            if (vehicle.CurrentX == vehicle.DestinationX && vehicle.CurrentY == vehicle.DestinationY)
            {
                vehicle.IsMoving = false;
                return;
            }

            vehicle.MoveProgress += 10;

            if (vehicle.MoveProgress >= 100)
            {
                vehicle.MoveProgress = 0;
                var path = _pathFindingService.FindPath(vehicle.CurrentX, vehicle.CurrentY, vehicle.DestinationX, vehicle.DestinationY);

                if (path.Count > 1)
                {
                    vehicle.CurrentX = path[1].X;
                    vehicle.CurrentY = path[1].Y;

                    VehicleMoved?.Invoke(this, new VehicleMovedEventArgs
                    {
                        VehicleId = vehicle.Id,
                        NewX = vehicle.CurrentX,
                        NewY = vehicle.CurrentY,
                        Progress = vehicle.MoveProgress
                    });
                }
                else
                {
                    vehicle.IsMoving = false;
                }
            }
        }

        private VehicleModel CreateVehicle(VehicleType type)
        {
            var vehicle = new VehicleModel
            {
                Id = _vehicles.Count + 1,
                Type = type,
                OwnerType = OwnerType.Private,
                OwnerId = 1
            };

            switch (type)
            {
                case VehicleType.Car: vehicle.MaxSpeed = 120; break;
                case VehicleType.Bus: vehicle.MaxSpeed = 80; vehicle.Capacity = 50; vehicle.OwnerType = OwnerType.Government; break;
                case VehicleType.Truck: vehicle.MaxSpeed = 90; vehicle.OwnerType = OwnerType.Corporate; break;
                case VehicleType.Taxi: vehicle.MaxSpeed = 100; vehicle.OwnerType = OwnerType.TaxiCompany; break;
                case VehicleType.Emergency: vehicle.MaxSpeed = 130; vehicle.OwnerType = OwnerType.Government; break;
                case VehicleType.Delivery: vehicle.MaxSpeed = 80; vehicle.OwnerType = OwnerType.Corporate; break;
            }

            return vehicle;
        }
        public VehicleModel AddVehicleOnRoute(VehicleType type, int startX, int startY, int endX, int endY)
        {
            var vehicle = CreateVehicle(type);

            // Находим путь между заданными точками
            var path = _pathFindingService.FindPath(startX, startY, endX, endY);

            if (path.Count > 0)
            {
                vehicle.CurrentX = path[0].X;
                vehicle.CurrentY = path[0].Y;
                vehicle.DestinationX = path[path.Count - 1].X;
                vehicle.DestinationY = path[path.Count - 1].Y;
                vehicle.IsMoving = true;

                _vehicles.Add(vehicle);

                // Уведомляем о добавлении транспорта
                VehicleAdded?.Invoke(this, new VehicleAddedEventArgs { Vehicle = vehicle });

                return vehicle;
            }

            return null; // Путь не найден
        }

    }

    public class VehicleMovedEventArgs : EventArgs
    {
        public int VehicleId { get; set; }
        public int NewX { get; set; }
        public int NewY { get; set; }
        public int Progress { get; set; }
    }

    public class VehicleAddedEventArgs : EventArgs
    {
        public VehicleModel Vehicle { get; set; }
    }
}