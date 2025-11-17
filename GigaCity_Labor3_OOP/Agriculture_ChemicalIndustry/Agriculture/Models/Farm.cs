using System;
using System.Collections.Generic;
using System.Linq;
using Agriculture_ChemicalIndustry.Common.Enums;
using Agriculture_ChemicalIndustry.Common.Interfaces;

namespace Agriculture_ChemicalIndustry.Agriculture.Models
{
    /// <summary>
    /// Базовая модель фермы
    /// </summary>
    public class Farm : Organization, IResourceConsumer, IResourceProducer, IUpgradable
    {
        public FarmType Type { get; set; }
        public double Area { get; set; } // общая площадь в гектарах
        public double UsedArea { get; set; } // используемая площадь
        public int WorkerCapacity { get; set; }
        public double Efficiency { get; set; } = 0.8; // эффективность (0-1)
        public List<AgriculturalProduction> CurrentProductions { get; set; }
        public Dictionary<AgriculturalProductType, double> Storage { get; set; }

        // Производственные показатели
        public double DailyOperatingCost { get; set; }
        public double BaseProductivity { get; set; }

        // Система улучшений
        public int UpgradeLevel { get; private set; } = 1;
        public int MaxUpgradeLevel { get; } = 5;

        public Farm(string name, FarmType farmType, double area, int workerCapacity, decimal initialBudget)
            : base(name, "Agriculture", initialBudget)
        {
            Type = farmType;
            Area = area;
            WorkerCapacity = workerCapacity;
            CurrentProductions = new List<AgriculturalProduction>();
            Storage = new Dictionary<AgriculturalProductType, double>();

            InitializeFarm();
        }

        private void InitializeFarm()
        {
            // Инициализация в зависимости от типа фермы
            switch (Type)
            {
                case FarmType.FieldCrops:
                    BaseProductivity = 100;
                    DailyOperatingCost = Area * 50;
                    break;
                case FarmType.Greenhouse:
                    BaseProductivity = 300;
                    DailyOperatingCost = Area * 200;
                    break;
                case FarmType.Livestock:
                    BaseProductivity = 150;
                    DailyOperatingCost = Area * 100;
                    break;
                case FarmType.PoultryFarm:
                    BaseProductivity = 200;
                    DailyOperatingCost = Area * 80;
                    break;
                default:
                    BaseProductivity = 120;
                    DailyOperatingCost = Area * 75;
                    break;
            }

            // Устанавливаем базовые затраты и доходы для финансовой системы
            Costs = (decimal)DailyOperatingCost;
            Revenue = (decimal)(BaseProductivity * Area * Efficiency * 0.5); // начальный ожидаемый доход
        }

        /// <summary>
        /// Обработать производство за день
        /// </summary>
        public void ProcessProduction()
        {
            // Обновляем финансовые показатели
            Costs = (decimal)DailyOperatingCost;
            Revenue = 0;

            // Обрабатываем каждое производство
            foreach (var production in CurrentProductions.ToList())
            {
                production.Update();

                if (production.IsHarvestable)
                {
                    var yield = HarvestProduction(production);
                    Revenue += CalculateRevenueFromYield(production.ProductType, yield);
                }
            }

            // Продажа хранимой продукции
            Revenue += CalculateRevenueFromStorage();

            // Улучшаем эффективность со временем
            if (UpgradeLevel > 1)
            {
                Efficiency = Math.Min(0.95, 0.8 + (UpgradeLevel - 1) * 0.05);
            }
        }

        private double HarvestProduction(AgriculturalProduction production)
        {
            double yield = production.CalculateExpectedYield() * Efficiency;

            // Добавляем в хранилище
            if (!Storage.ContainsKey(production.ProductType))
                Storage[production.ProductType] = 0;

            Storage[production.ProductType] += yield;
            CurrentProductions.Remove(production);

            return yield;
        }

        private decimal CalculateRevenueFromYield(AgriculturalProductType productType, double yield)
        {
            var pricePerTon = GetMarketPrice(productType);
            return (decimal)yield * pricePerTon;
        }

        private decimal CalculateRevenueFromStorage()
        {
            decimal revenue = 0;
            var productsToSell = Storage.Keys.ToList();

            foreach (var productType in productsToSell)
            {
                // Продаем часть хранимой продукции
                var sellPercentage = 0.2; // 20% в день
                var sellAmount = Storage[productType] * sellPercentage;
                revenue += (decimal)sellAmount * GetMarketPrice(productType);
                Storage[productType] -= sellAmount;

                // Очищаем если почти пусто
                if (Storage[productType] < 0.1)
                    Storage.Remove(productType);
            }

            return revenue;
        }

        private decimal GetMarketPrice(AgriculturalProductType productType)
        {
            // Базовая цена за тонну
            return productType switch
            {
                AgriculturalProductType.Wheat => 15000m,
                AgriculturalProductType.Corn => 12000m,
                AgriculturalProductType.Rice => 20000m,
                AgriculturalProductType.Potatoes => 10000m,
                AgriculturalProductType.Tomatoes => 25000m,
                AgriculturalProductType.Apples => 30000m,
                AgriculturalProductType.Beef => 150000m,
                AgriculturalProductType.Pork => 120000m,
                AgriculturalProductType.Chicken => 80000m,
                AgriculturalProductType.Milk => 50000m,
                AgriculturalProductType.Eggs => 60000m,
                _ => 20000m
            };
        }

        // Реализация IResourceConsumer
        public Dictionary<ResourceType, double> GetDailyResourceRequirements()
        {
            var requirements = new Dictionary<ResourceType, double>
            {
                [ResourceType.Electricity] = Area * 10,
                [ResourceType.Water] = Area * 1000,
                [ResourceType.Labor] = WorkerCapacity * 8
            };

            // Добавляем требования от текущих производств
            foreach (var production in CurrentProductions)
            {
                var productionRequirements = production.GetDailyRequirements();
                foreach (var req in productionRequirements)
                {
                    if (requirements.ContainsKey(req.Key))
                        requirements[req.Key] += req.Value;
                    else
                        requirements[req.Key] = req.Value;
                }
            }

            return requirements;
        }

        public void ConsumeResources(Dictionary<ResourceType, double> resources)
        {
            // В реальной реализации здесь бы вычитались ресурсы
            // Для упрощения просто проверяем доступность
        }

        public bool CanOperate(Dictionary<ResourceType, double> availableResources)
        {
            var requirements = GetDailyResourceRequirements();
            return requirements.All(req =>
                availableResources.ContainsKey(req.Key) &&
                availableResources[req.Key] >= req.Value);
        }

        // Реализация IResourceProducer
        public Dictionary<ResourceType, double> GetDailyProduction()
        {
            var production = new Dictionary<ResourceType, double>();

            // Рассчитываем производство на основе текущих культур
            foreach (var prod in CurrentProductions.Where(p => p.IsHarvestable))
            {
                var productResource = ConvertToResourceType(prod.ProductType);
                var amount = prod.CalculateExpectedYield() * Efficiency;

                if (production.ContainsKey(productResource))
                    production[productResource] += amount;
                else
                    production[productResource] = amount;
            }

            return production;
        }

        public void ProduceResources()
        {
            ProcessProduction();
        }

        private ResourceType ConvertToResourceType(AgriculturalProductType productType)
        {
            // Преобразуем тип продукции в тип ресурса
            return productType switch
            {
                AgriculturalProductType.Wheat or AgriculturalProductType.Corn or AgriculturalProductType.Rice
                    => ResourceType.Grain,
                AgriculturalProductType.Potatoes or AgriculturalProductType.Tomatoes or AgriculturalProductType.Carrots
                    => ResourceType.Vegetables,
                AgriculturalProductType.Apples or AgriculturalProductType.Oranges or AgriculturalProductType.Grapes
                    => ResourceType.Fruits,
                AgriculturalProductType.Beef or AgriculturalProductType.Pork => ResourceType.Meat,
                AgriculturalProductType.Chicken => ResourceType.Poultry,
                AgriculturalProductType.Milk => ResourceType.Dairy,
                AgriculturalProductType.Eggs => ResourceType.Eggs,
                AgriculturalProductType.Wool => ResourceType.Wool,
                AgriculturalProductType.Fish => ResourceType.Fish,
                _ => ResourceType.RawMaterials
            };
        }

        // Реализация IUpgradable
        public decimal GetUpgradeCost()
        {
            return (decimal)(Area * 1000 * Math.Pow(2, UpgradeLevel));
        }

        public bool Upgrade()
        {
            if (UpgradeLevel >= MaxUpgradeLevel)
                return false;

            var cost = GetUpgradeCost();
            if (Budget >= cost)
            {
                if (SpendMoney(cost, $"Улучшение фермы до уровня {UpgradeLevel + 1}"))
                {
                    UpgradeLevel++;
                    Efficiency += 0.05;
                    BaseProductivity *= 1.2;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Добавить новое производство
        /// </summary>
        public bool AddProduction(AgriculturalProduction production)
        {
            if (UsedArea + production.AreaUsed > Area)
                return false; // Недостаточно площади

            CurrentProductions.Add(production);
            UsedArea += production.AreaUsed;
            return true;
        }

        /// <summary>
        /// Получить общую статистику фермы
        /// </summary>
        public FarmStatistics GetStatistics()
        {
            return new FarmStatistics
            {
                TotalArea = Area,
                UsedArea = UsedArea,
                FreeArea = Area - UsedArea,
                ProductionCount = CurrentProductions.Count,
                StorageCapacity = Storage.Values.Sum(),
                Efficiency = Efficiency,
                UpgradeLevel = UpgradeLevel
            };
        }
    }

    /// <summary>
    /// Статистика фермы
    /// </summary>
    public class FarmStatistics
    {
        public double TotalArea { get; set; }
        public double UsedArea { get; set; }
        public double FreeArea { get; set; }
        public int ProductionCount { get; set; }
        public double StorageCapacity { get; set; }
        public double Efficiency { get; set; }
        public int UpgradeLevel { get; set; }
    }
}
