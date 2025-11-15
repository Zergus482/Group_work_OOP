using CitySimulation.Enums;

namespace CitySimulation.Models.EmergencyService
{
    public class FireEngine : FireFighterVehicle
    {
        public override bool IsSuitableFor(EmergencyType emergencyType)
        {
            return emergencyType == EmergencyType.Fire ||
                   emergencyType == EmergencyType.TrafficAccident;
        }
    }
}