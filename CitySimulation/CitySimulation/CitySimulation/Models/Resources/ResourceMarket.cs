using System.Collections.Generic;

namespace CitySimulation.Models.Resources
{
    public class ResourceMarket
    {
        public Dictionary<ResourceType, double> Prices { get; private set; }

        public ResourceMarket()
        {
            Prices = new Dictionary<ResourceType, double>
            {
                { ResourceType.Oil, 85.50 },
                { ResourceType.Gas, 45.30 },
                { ResourceType.Coal, 12.75 },
                { ResourceType.Iron, 150.00 },
                { ResourceType.Copper, 8900.00 },
                { ResourceType.Gold, 59000.00 },
                { ResourceType.Water, 0.85 }
            };
        }

        public double CalculateRevenue(ResourceType type, double amount)
        {
            return Prices[type] * amount;
        }

        public void UpdatePrice(ResourceType type, double newPrice)
        {
            Prices[type] = newPrice;
        }
    }
}
