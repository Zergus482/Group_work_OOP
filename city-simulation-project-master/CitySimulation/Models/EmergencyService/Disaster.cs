using CitySimulation.Enums;
using CitySimulation.Models.Base;

namespace CitySimulation.Models.EmergencyService
{
    public class Disaster : ObservableObject
    {
        private double _xCoordinate;
        private double _yCoordinate;
        private EmergencyType _type;
        private double _intensity;
        private double _health;

        public double XCoordinate
        {
            get => _xCoordinate;
            set => SetProperty(ref _xCoordinate, value);
        }

        public double YCoordinate
        {
            get => _yCoordinate;
            set => SetProperty(ref _yCoordinate, value);
        }

        public EmergencyType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public double Intensity
        {
            get => _intensity;
            set => SetProperty(ref _intensity, value);
        }

        public double Health
        {
            get => _health;
            set => SetProperty(ref _health, value);
        }
    }
}