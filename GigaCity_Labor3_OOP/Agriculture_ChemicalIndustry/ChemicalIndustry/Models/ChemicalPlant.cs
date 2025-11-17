using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheFinancialSystem;

namespace Agriculture_ChemicalIndustry.ChemicalIndustry.Models
{
    /// <summary>
    /// Химический завод
    /// </summary>
    public class ChemicalPlant : Organization, IResourceConsumer, IResourceProducer, IPollutable, IUpgradable
    {
        public ChemicalProductType MainProductType { get; set; }
        public double ProductionCapacity { get; set; } // тонн в день
        public double CurrentProduction { get; set; }
        public Dictionary<ResourceType, double> RequiredResources { get; set; }
        public double ProductionEfficiency { get; set; } = 0.85;

        // Система безопасности
        public double SafetyLevel { get; set; } = 0.9;

        // Реализация IPollutable
        public double PollutionLevel { get; set; }
        public double MaxPollutionLevel { get; } = 10000.0;

        // Реализация IUpgradable
        public int UpgradeLevel { get; private set; } = 1;
        public int MaxUpgradeLevel { get; } = 5;

        public ChemicalPlant(string name, ChemicalProductType productType, double productionCapacity, decimal initialBudget)
            : base(name, "ChemicalIndustry", initialBudget)
        {
            MainProductType = productType;
            ProductionCapacity = productionCapacity;
            RequiredResources = new Dictionary<ResourceType, double>();
            PollutionLevel = 0;

            InitializeResourceRequirements();
            CalculateBaseEconomics();
        }

        private void InitializeResourceRequirements()
        {
            // Устанавливаем требования к ресурсам в зависимости от типа продукции
            switch (MainProductType)
            {
                case ChemicalProductType.Fertilizers:
                    RequiredResources[ResourceType.Electricity] = ProductionCapacity * 10;
                    RequiredResources[ResourceType.Water] = ProductionCapacity * 100;
                    RequiredResources[ResourceType.Chemicals] = ProductionCapacity * 50;
                    RequiredResources[ResourceType.Gas] = ProductionCapacity * 20;
                    break;

                case ChemicalProductType.Pharmaceuticals:
                    RequiredResources[ResourceType.Electricity] = ProductionCapacity * 15;
                    RequiredResources[ResourceType.Water] = ProductionCapacity * 80;
                    RequiredResources[ResourceType.Chemicals] = ProductionCapacity * 30;
                    RequiredResources[ResourceType.RawMaterials] = ProductionCapacity * 40;
                    break;

                case ChemicalProductType.Plastics:
                    RequiredResources[ResourceType.Electricity] = ProductionCapacity * 20;
                    RequiredResources[ResourceType.Oil] = ProductionCapacity * 100;
                    RequiredResources[ResourceType.Chemicals] = ProductionCapacity * 25;
                    break;

                default:
                    RequiredResources[ResourceType.Electricity] = ProductionCapacity * 12;
                    RequiredResources[ResourceType.Water] = ProductionCapacity * 90;
                    RequiredResources[ResourceType.Chemicals] = ProductionCapacity * 35;
                    break;
            }
        }

        private void CalculateBaseEconomics()
        {
            // Расчет базовых экономических показателей
            var baseCost = ProductionCapacity * 100; // базовые затраты
            var baseRevenue = ProductionCapacity * 250; // базовый доход

            Costs = (decimal)baseCost;
            Revenue = (decimal)(baseRevenue * ProductionEfficiency);
        }

        /// <summary>
        /// Обработать производство
        /// </summary>
        public void ProcessProduction(Dictionary<ResourceType, double> cityResources)
        {
            if (CanOperate(cityResources) && Budget > (decimal)(ProductionCapacity * 10))
            {
                // Используем ресурсы
                foreach (var requirement in RequiredResources)
                {
                    if (cityResources.ContainsKey(requirement.Key))
                    {
                        cityResources[requirement.Key] -= requirement.Value;
                        // Учитываем стоимость ресурсов в затратах
                        Costs += (decimal)(requirement.Value * GetResourceCost(requirement.Key));
                    }
                }

                // Производим продукцию
                CurrentProduction = ProductionCapacity * ProductionEfficiency * SafetyLevel;
                Revenue = (decimal)(CurrentProduction * GetProductPrice());

                // Генерируем загрязнение
                GeneratePollution();

                // Операционные расходы
                var operationalCost = (decimal)(ProductionCapacity * 50 * (1 + PollutionLevel / MaxPollutionLevel));
                if (SpendMoney(operationalCost, "Операционные расходы"))
                {
                    Costs += operationalCost;
                }
            }
            else
            {
                CurrentProduction = 0;
                Revenue = 0;
            }
        }

        public bool CanOperate(Dictionary<ResourceType, double> availableResources)
        {
            return RequiredResources.All(requirement =>
                availableResources.ContainsKey(requirement.Key) &&
                availableResources[requirement.Key] >= requirement.Value);
        }

        private decimal GetProductPrice()
        {
            return MainProductType switch
            {
                ChemicalProductType.Fertilizers => 50000m,
                ChemicalProductType.Pesticides => 75000m,
                ChemicalProductType.Herbicides => 70000m,
                ChemicalProductType.Pharmaceuticals => 150000m,
                ChemicalProductType.Plastics => 40000m,
                ChemicalProductType.Petrochemicals => 60000m,
                ChemicalProductType.BasicChemicals => 30000m,
                ChemicalProductType.SyntheticMaterials => 80000m,
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
                ResourceType.Gas => 80m,
                ResourceType.Chemicals => 50m,
                ResourceType.RawMaterials => 30m,
                _ => 20m
            };
        }

        // Реализация IResourceConsumer
        public Dictionary<ResourceType, double> GetDailyResourceRequirements()
        {
            return RequiredResources.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value * (1 + (1 - SafetyLevel))
            );
        }

        public void ConsumeResources(Dictionary<ResourceType, double> resources)
        {
            // В реальной реализации здесь бы вычитались ресурсы
        }

        // Реализация IResourceProducer
        public Dictionary<ResourceType, double> GetDailyProduction()
        {
            var production = new Dictionary<ResourceType, double>();
            var productResource = ConvertToResourceType(MainProductType);
            production[productResource] = CurrentProduction;

            // Побочные продукты
            if (PollutionLevel > 0)
            {
                production[ResourceType.Pollution] = PollutionLevel * 0.1;
                production[ResourceType.Waste] = CurrentProduction * 0.05;
            }

            return production;
        }

        public void ProduceResources()
        {
            // Производство уже обрабатывается в ProcessProduction
        }

        private ResourceType ConvertToResourceType(ChemicalProductType productType)
        {
            return productType switch
            {
                ChemicalProductType.Fertilizers => ResourceType.Fertilizers,
                ChemicalProductType.Pesticides => ResourceType.Pesticides,
                ChemicalProductType.Herbicides => ResourceType.Herbicides,
                ChemicalProductType.Pharmaceuticals => ResourceType.Pharmaceuticals,
                ChemicalProductType.Plastics => ResourceType.Plastics,
                ChemicalProductType.Petrochemicals => ResourceType.Petrochemicals,
                ChemicalProductType.BasicChemicals => ResourceType.BasicChemicals,
                ChemicalProductType.SyntheticMaterials => ResourceType.SyntheticMaterials,
                _ => ResourceType.Chemicals
            };
        }

        // Реализация IPollutable
        public void GeneratePollution()
        {
            var pollutionGenerated = CurrentProduction * (1 - SafetyLevel) * 0.1;
            PollutionLevel += pollutionGenerated;
            PollutionLevel = Math.Min(PollutionLevel, MaxPollutionLevel);
        }

        public void CleanPollution(double amount)
        {
            var cost = (decimal)(amount * 100); // Стоимость очистки
            if (Budget >= cost)
            {
                if (SpendMoney(cost, "Очистка загрязнения"))
                {
                    PollutionLevel = Math.Max(0, PollutionLevel - amount);
                }
            }
        }

        // Реализация IUpgradable
        public decimal GetUpgradeCost()
        {
            return (decimal)(ProductionCapacity * 5000 * Math.Pow(1.5, UpgradeLevel));
        }

        public bool Upgrade()
        {
            if (UpgradeLevel >= MaxUpgradeLevel)
                return false;

            var cost = GetUpgradeCost();
            if (Budget >= cost)
            {
                if (SpendMoney(cost, $"Улучшение завода до уровня {UpgradeLevel + 1}"))
                {
                    UpgradeLevel++;
                    ProductionEfficiency += 0.05;
                    SafetyLevel += 0.05;
                    ProductionCapacity *= 1.15;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Инвестировать в безопасность
        /// </summary>
        public void InvestInSafety(decimal investment)
        {
            if (SpendMoney(investment, "Инвестиции в безопасность"))
            {
                SafetyLevel = Math.Min(0.99, SafetyLevel + 0.1m);
                PollutionLevel *= 0.9; // Снижаем загрязнение
            }
        }

        /// <summary>
        /// Получить статистику завода
        /// </summary>
        public ChemicalPlantStatistics GetStatistics()
        {
            return new ChemicalPlantStatistics
            {
                ProductionCapacity = ProductionCapacity,
                CurrentProduction = CurrentProduction,
                ProductionEfficiency = ProductionEfficiency,
                SafetyLevel = SafetyLevel,
                PollutionLevel = PollutionLevel,
                UpgradeLevel = UpgradeLevel
            };
        }
    }

    /// <summary>
    /// Статистика химического завода
    /// </summary>
    public class ChemicalPlantStatistics
    {
        public double ProductionCapacity { get; set; }
        public double CurrentProduction { get; set; }
        public double ProductionEfficiency { get; set; }
        public double SafetyLevel { get; set; }
        public double PollutionLevel { get; set; }
        public int UpgradeLevel { get; set; }
    }
}
