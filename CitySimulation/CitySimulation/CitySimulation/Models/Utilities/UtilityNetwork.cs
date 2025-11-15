using System.Collections.Generic;
using System.Linq;

namespace CitySimulation.Models.Utilities
{
    public class UtilityNetwork
    {
        public UtilityType Type { get; set; }
        public double Capacity { get; set; }
        public double CurrentLoad { get; set; }
        public double Production { get; set; }
        public List<ResidentialBuilding> ConnectedBuildings { get; private set; }
        public bool IsOperational { get; set; }

        public UtilityNetwork(UtilityType type, double capacity)
        {
            Type = type;
            Capacity = capacity;
            CurrentLoad = 0;
            Production = 0;
            ConnectedBuildings = new List<ResidentialBuilding>();
            IsOperational = true;
        }

        public bool ConnectBuilding(ResidentialBuilding building)
        {
            if (ConnectedBuildings.Contains(building)) return false;

            double additionalLoad = GetBuildingConsumption(building);
            if (CurrentLoad + additionalLoad > Capacity) return false;

            ConnectedBuildings.Add(building);
            CurrentLoad += additionalLoad;
            building.HasUtilities = true;
            return true;
        }

        public void DisconnectBuilding(ResidentialBuilding building)
        {
            if (ConnectedBuildings.Remove(building))
            {
                CurrentLoad -= GetBuildingConsumption(building);
                building.HasUtilities = false;
            }
        }

        private double GetBuildingConsumption(ResidentialBuilding building)
        {
            // Заменяем switch expression на традиционный switch
            switch (Type)
            {
                case UtilityType.Electricity:
                    return building.ElectricityConsumption;
                case UtilityType.Water:
                    return building.WaterConsumption;
                case UtilityType.Gas:
                    return building.GasConsumption;
                case UtilityType.Sewage:
                    return building.SewageProduction;
                default:
                    return 0;
            }
        }

        public void UpdateProduction(double newProduction)
        {
            Production = newProduction;
            IsOperational = Production >= CurrentLoad;
        }
    }
}