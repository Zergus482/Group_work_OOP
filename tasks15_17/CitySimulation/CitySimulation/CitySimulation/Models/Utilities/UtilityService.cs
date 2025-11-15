using System.Collections.Generic;
using System.Linq;

namespace CitySimulation.Models.Utilities
{
    public class UtilityService
    {
        public string Name { get; set; }
        public Dictionary<UtilityType, UtilityNetwork> Networks { get; private set; }
        public double Budget { get; set; }
        public double MaintenanceCost { get; set; }

        public UtilityService(string name, double initialBudget = 100000)
        {
            Name = name;
            Networks = new Dictionary<UtilityType, UtilityNetwork>();
            InitializeNetworks();
            Budget = initialBudget;
            MaintenanceCost = 5000;
        }

        private void InitializeNetworks()
        {
            Networks[UtilityType.Electricity] = new UtilityNetwork(UtilityType.Electricity, 10000);
            Networks[UtilityType.Water] = new UtilityNetwork(UtilityType.Water, 5000);
            Networks[UtilityType.Gas] = new UtilityNetwork(UtilityType.Gas, 3000);
            Networks[UtilityType.Sewage] = new UtilityNetwork(UtilityType.Sewage, 2000);
        }

        public bool ConnectBuildingToUtilities(ResidentialBuilding building)
        {
            bool allConnected = true;
            foreach (var network in Networks.Values)
            {
                if (!network.ConnectBuilding(building))
                {
                    allConnected = false;
                }
            }
            return allConnected;
        }

        public void DisconnectBuildingFromUtilities(ResidentialBuilding building)
        {
            foreach (var network in Networks.Values)
            {
                network.DisconnectBuilding(building);
            }
        }

        public void PerformMaintenance()
        {
            Budget -= MaintenanceCost;
            foreach (var network in Networks.Values)
            {
                network.IsOperational = Budget > 0;
            }
        }

        public void AddBudget(double amount)
        {
            Budget += amount;
        }

        public string GetNetworkStatus()
        {
            var status = "";
            foreach (var network in Networks.Values)
            {
                var networkName = GetNetworkNameRussian(network.Type);
                var statusRussian = network.IsOperational ? "Работает" : "Не работает";
                status += $"{networkName}: {statusRussian} (Нагрузка: {network.CurrentLoad:F1}/{network.Capacity})\n";
            }
            return status.Trim();
        }

        private string GetNetworkNameRussian(UtilityType type)
        {
            switch (type)
            {
                case UtilityType.Electricity:
                    return "Электричество";
                case UtilityType.Water:
                    return "Вода";
                case UtilityType.Gas:
                    return "Газ";
                case UtilityType.Sewage:
                    return "Канализация";
                default:
                    return type.ToString();
            }
        }
    }
}