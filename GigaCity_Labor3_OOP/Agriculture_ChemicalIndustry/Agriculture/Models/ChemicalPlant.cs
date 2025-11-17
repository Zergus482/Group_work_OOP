using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheFinancialSystem;

namespace Agriculture_ChemicalIndustry.Agriculture.Models
{
    public class ChemicalPlant : Organization
    {
        public ChemicalProductType MainProductType { get; set; }
        public double ProductionCapacity { get; set; }
        public double CurrentProductionRate { get; set; }
        public Dictionary<ResourceType, double> RequiredResources { get; set; }
        public double PollutionLevel { get; set; }
        public double SafetyLevel { get; set; } = 0.9;

        // Производственные показатели
        public double ProductionEfficiency { get; set; } = 0.85;

        public ChemicalPlant(string name, ChemicalProductType productType, double productionCapacity, decimal initialBudget)
            : base(name, "ChemicalIndustry", initialBudget)
        {
            MainProductType = productType;
            ProductionCapacity = productionCapacity;
            RequiredResources = new Dictionary<ResourceType, double>();
            PollutionLevel = 0.1;

            InitializeResourceRequirements();
            CalculateBaseEconomics();
        }

        private void InitializeResourceRequirements()
        {
            // Устанавливаем требования к ресурсам в зависимости от типа продукции
            switch (MainProductType)
            {
                case ChemicalProductType.Fertilizers:
                    RequiredResources[ResourceType.Electricity] = 100;
                    RequiredResources[ResourceType.Water] = 500;
                    RequiredResources[ResourceType.Chemicals] = 200;
                    break;
                case ChemicalProductType.Pharmaceuticals:
                    RequiredResources[ResourceType.Electricity] = 150;
                    RequiredResources[ResourceType.Water] = 300;
                    RequiredResources[ResourceType.Chemicals] = 100;
                    break;
                case ChemicalProductType.Plastics:
                    RequiredResources[ResourceType.Electricity] = 200;
                    RequiredResources[ResourceType.Oil] = 300;
                    break;
            }
        }

        private void CalculateBaseEconomics()
        {
            // Расчет базовых экономических показателей
            var baseCost = ProductionCapacity * 50; // базовые затраты на мощность
            var baseRevenue = ProductionCapacity * 120; // базовый доход

            Costs = (decimal)baseCost;
            Revenue = (decimal)(baseRevenue * ProductionEfficiency);
        }

        public bool CanProduce(Dictionary<ResourceType, double> cityResources)
        {
            return RequiredResources.All(requirement =>
                cityResources.ContainsKey(requirement.Key) &&
                cityResources[requirement.Key] >= requirement.Value);
        }

        public void ProcessProduction(Dictionary<ResourceType, double> cityResources)
        {
            if (CanProduce(cityResources) && Budget > (decimal)(ProductionCapacity * 10))
            {
                // Используем ресурсы
                foreach (var requirement in RequiredResources)
                {
                    cityResources[requirement.Key] -= requirement.Value;
                    // Учитываем стоимость ресурсов в затратах
                    Costs += (decimal)(requirement.Value * GetResourceCost(requirement.Key));
                }

                // Производим продукцию
                CurrentProductionRate = ProductionCapacity * ProductionEfficiency;
                Revenue = (decimal)(CurrentProductionRate * GetProductPrice());

                // Увеличиваем загрязнение
                PollutionLevel += 0.01 * (1 - SafetyLevel);

                // Расходы на эксплуатацию
                var operationalCost = (decimal)(ProductionCapacity * 25 * (1 + PollutionLevel));
                if (SpendMoney(operationalCost, "Операционные расходы"))
                {
                    Costs += operationalCost;
                }
            }
            else
            {
                CurrentProductionRate = 0;
                Revenue = 0;
            }
        }

        private decimal GetProductPrice()
        {
            return MainProductType switch
            {
                ChemicalProductType.Fertilizers => 50000m,
                ChemicalProductType.Pesticides => 75000m,
                ChemicalProductType.Pharmaceuticals => 150000m,
                ChemicalProductType.Plastics => 40000m,
                ChemicalProductType.Petrochemicals => 60000m,
                ChemicalProductType.BasicChemicals => 30000m,
                _ => 35000m
            };
        }

        private decimal GetResourceCost(ResourceType resourceType)
        {
            return resourceType switch
            {
                ResourceType.Electricity => 10m,
                ResourceType.Water => 5m,
                ResourceType.Oil => 100m,
                ResourceType.Chemicals => 50m,
                _ => 20m
            };
        }

        public void InvestInSafety(decimal investment)
        {
            if (SpendMoney(investment, "Инвестиции в безопасность"))
            {
                SafetyLevel += 0.1m;
                if (SafetyLevel > 1.0m) SafetyLevel = 1.0m;
                PollutionLevel *= 0.9; // Снижаем загрязнение
            }
        }
    }
}
