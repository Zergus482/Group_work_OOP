using System.Collections.Generic;
using System.Windows.Data;
using CitySimulation.Models.Base;

namespace CitySimulation.Models.EmergencyService
{
    public class FireStation : Building
    {
        private int _maxVehicles;
        private string _servedDistrict;

        public int MaxVehicles
        {
            get => _maxVehicles;
            set => SetProperty(ref _maxVehicles, value);
        }

        public string ServedDistrict
        {
            get => _servedDistrict;
            set => SetProperty(ref _servedDistrict, value);
        }

        public List<FireFighterVehicle> Vehicles { get; set; } = new List<FireFighterVehicle>();
    }
}