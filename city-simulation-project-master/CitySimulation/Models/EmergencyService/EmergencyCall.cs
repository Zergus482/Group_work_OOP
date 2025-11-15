using System;
using CitySimulation.Enums;
using CitySimulation.Models.Base;

namespace CitySimulation.Models.EmergencyService
{
    public class EmergencyCall : ObservableObject
    {
        private double _xCoordinate;
        private double _yCoordinate;
        private EmergencyType _emergencyType;
        private int _threatLevel;
        private DateTime _callTime;

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

        public EmergencyType EmergencyType
        {
            get => _emergencyType;
            set => SetProperty(ref _emergencyType, value);
        }

        public int ThreatLevel
        {
            get => _threatLevel;
            set => SetProperty(ref _threatLevel, value);
        }

        public DateTime CallTime
        {
            get => _callTime;
            set => SetProperty(ref _callTime, value);
        }
    }
}