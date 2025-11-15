using CitySimulation.Enums;
using CitySimulation.Models.Base;

namespace CitySimulation.Models.EmergencyService
{
    public abstract class FireFighterVehicle : ObservableObject
    {
        private string _name;
        private VehicleState _currentState;
        private double _currentWater;
        private double _maxWater;
        private double _pumpPower;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public VehicleState CurrentState
        {
            get => _currentState;
            set => SetProperty(ref _currentState, value);
        }

        public double CurrentWater
        {
            get => _currentWater;
            set => SetProperty(ref _currentWater, value);
        }

        public double MaxWater
        {
            get => _maxWater;
            set => SetProperty(ref _maxWater, value);
        }

        public double PumpPower
        {
            get => _pumpPower;
            set => SetProperty(ref _pumpPower, value);
        }

        public abstract bool IsSuitableFor(EmergencyType emergencyType);
    }
}