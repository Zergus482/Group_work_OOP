using CitySimulation.Enums;

namespace CitySimulation.Models.EmergencyService
{
    public class LadderTruck : FireFighterVehicle
    {
        private double _ladderLength;

        public double LadderLength
        {
            get => _ladderLength;
            set => SetProperty(ref _ladderLength, value);
        }

        public override bool IsSuitableFor(EmergencyType emergencyType)
        {
            return emergencyType == EmergencyType.Fire;
        }
    }
}