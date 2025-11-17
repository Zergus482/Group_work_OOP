using System;
using System.Collections.Generic;
using System.Linq;
using TheFinancialSystem;
using CitySimulation.Industries.Common.Enums;
using CitySimulation.Industries.Common.Interfaces;
using CitySimulation.Industries.Common.Models;

namespace CitySimulation.Industries.Agriculture.Models
{
    /// <summary>
    /// Базовая модель фермы
    /// </summary>
    public class FarmModel : IndustryOrganization, IResourceConsumer, IResourceProducer, IUpgradable
    {
        public FarmType Type { get; set; }
        public double Area { get; set; }
        public double UsedArea { get; set; }
        public int WorkerCapacity { get; set; }
        public double Efficiency { get; set; } = 0.8;
        public List<AgriculturalProduction> CurrentProductions { get; set; }
        public Dictionary<AgriculturalProductType, double> Storage { get; set; }

        // Производственные показатели
        public double DailyOperatingCost { get; set; }
        public double BaseProductivity { get; set; }

        // Система улучшений
        public int UpgradeLevel { get; private set; } = 1;
        public int MaxUpgradeLevel { get; } = 5;

        public FarmModel(string name, FarmType farmType, double area, int workerCapacity, decimal initialBudget)
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
            Revenue = (decimal)(BaseProductivity * Area * Efficiency * 0.5);
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
                var sellPercentage = 0.2;
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
            switch (productType)
            {
                case AgriculturalProductType.Wheat: return 15000m;
                case AgriculturalProductType.Corn: return 12000m;
                case AgriculturalProductType.Rice: return 20000m;
                case AgriculturalProductType.Potatoes: return 10000m;
                case AgriculturalProductType.Tomatoes: return 25000m;
                case AgriculturalProductType.Apples: return 30000m;
                case AgriculturalProductType.Beef: return 150000m;
                case AgriculturalProductType.Pork: return 120000m;
                case AgriculturalProductType.Chicken: return 80000m;
                case AgriculturalProductType.Milk: return 50000m;
                case AgriculturalProductType.Eggs: return 60000m;
                default: return 20000m;
            }
        }

        // Реализация IResourceConsumer
        public Dictionary<ResourceType, double> GetDailyResourceRequirements()
        {
            var requirements = new Dictionary<ResourceType, double>
            {
                { ResourceType.Electricity, Area * 10 },
                { ResourceType.Water, Area * 1000 },
                { ResourceType.Labor, WorkerCapacity * 8 }
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
                        requirements.Add(req.Key, req.Value);
                }
            }

            return requirements;
        }

        public void ConsumeResources(Dictionary<ResourceType, double> resources)
        {
            // В реальной реализации здесь бы вычитались ресурсы
        }

        public bool CanOperate(Dictionary<ResourceType, double> availableResources)
        {
            var requirements = GetDailyResourceRequirements();
            foreach (var req in requirements)
            {
                if (!availableResources.ContainsKey(req.Key) || availableResources[req.Key] < req.Value)
                    return false;
            }
            return true;
        }

        // Реализация IResourceProducer
        public Dictionary<ResourceType, double> GetDailyProduction()
        {
            var production = new Dictionary<ResourceType, double>();

            // Рассчитываем производство на основе текущих культур
            foreach (var prod in CurrentProductions)
            {
                if (prod.IsHarvestable)
                {
                    var productResource = ConvertToResourceType(prod.ProductType);
                    var amount = prod.CalculateExpectedYield() * Efficiency;

                    if (production.ContainsKey(productResource))
                        production[productResource] += amount;
                    else
                        production.Add(productResource, amount);
                }
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
            switch (productType)
            {
                case AgriculturalProductType.Wheat:
                case AgriculturalProductType.Corn:
                case AgriculturalProductType.Rice:
                    return ResourceType.Grain;
                case AgriculturalProductType.Potatoes:
                case AgriculturalProductType.Tomatoes:
                case AgriculturalProductType.Carrots:
                    return ResourceType.Vegetables;
                case AgriculturalProductType.Apples:
                case AgriculturalProductType.Oranges:
                case AgriculturalProductType.Grapes:
                    return ResourceType.Fruits;
                case AgriculturalProductType.Beef:
                case AgriculturalProductType.Pork:
                    return ResourceType.Meat;
                case AgriculturalProductType.Chicken:
                    return ResourceType.Poultry;
                case AgriculturalProductType.Milk:
                    return ResourceType.Dairy;
                case AgriculturalProductType.Eggs:
                    return ResourceType.Eggs;
                case AgriculturalProductType.Wool:
                    return ResourceType.Wool;
                case AgriculturalProductType.Fish:
                    return ResourceType.Fish;
                default:
                    return ResourceType.RawMaterials;
            }
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
                return false;

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