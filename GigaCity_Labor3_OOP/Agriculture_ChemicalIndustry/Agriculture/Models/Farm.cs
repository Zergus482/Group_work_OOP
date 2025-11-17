using System.Collections.Generic;
using System.Linq;

namespace Agriculture_ChemicalIndustry.Agriculture.Models
{
    public class Farm : Organization
    {
        public FarmType Type { get; set; }
        public double Area { get; set; }
        public int WorkerCapacity { get; set; }
        public double Efficiency { get; set; } = 0.8;
        public List<AgriculturalProduction> CurrentProductions { get; set; }
        public Dictionary<AgriculturalProductType, double> Storage { get; set; }

        // Производственные показатели
        public double DailyProductionCost { get; set; }
        public double ExpectedDailyRevenue { get; set; }

        public Farm(string name, FarmType farmType, double area, int workerCapacity, decimal initialBudget)
            : base(name, "Agriculture", initialBudget)
        {
            Type = farmType;
            Area = area;
            WorkerCapacity = workerCapacity;
            CurrentProductions = new List<AgriculturalProduction>();
            Storage = new Dictionary<AgriculturalProductType, double>();

            // Расчет базовых затрат и доходов
            CalculateBaseCostsAndRevenue();
        }

        private void CalculateBaseCostsAndRevenue()
        {
            // Расчет затрат на основе типа фермы и площади
            switch (Type)
            {
                case FarmType.FieldCrops:
                    DailyProductionCost = (decimal)(Area * 50); // 50 денег за гектар
                    ExpectedDailyRevenue = (decimal)(Area * 80);
                    break;
                case FarmType.Greenhouse:
                    DailyProductionCost = (decimal)(Area * 200);
                    ExpectedDailyRevenue = (decimal)(Area * 300);
                    break;
                case FarmType.Livestock:
                    DailyProductionCost = (decimal)(Area * 100);
                    ExpectedDailyRevenue = (decimal)(Area * 150);
                    break;
                default:
                    DailyProductionCost = (decimal)(Area * 75);
                    ExpectedDailyRevenue = (decimal)(Area * 100);
                    break;
            }
        }

        public void ProcessProduction()
        {
            // Обновляем доходы и расходы для финансовой системы
            Costs = (decimal)DailyProductionCost;
            Revenue = 0;

            foreach (var production in CurrentProductions.ToList())
            {
                production.DaysPlanted++;

                if (production.IsHarvestable)
                {
                    var yield = HarvestProduction(production);
                    Revenue += CalculateRevenueFromYield(production.ProductType, yield);
                }
            }

            // Учитываем хранение продукции
            Revenue += CalculateRevenueFromStorage();
        }

        private double HarvestProduction(AgriculturalProduction production)
        {
            double yield = production.CalculateExpectedYield() * Efficiency;

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
            foreach (var storageItem in Storage)
            {
                // Продаем 20% хранимой продукции каждый день
                var sellAmount = storageItem.Value * 0.2;
                revenue += (decimal)sellAmount * GetMarketPrice(storageItem.Key);
                Storage[storageItem.Key] -= sellAmount;
            }
            return revenue;
        }

        private decimal GetMarketPrice(AgriculturalProductType productType)
        {
            return productType switch
            {
                AgriculturalProductType.Grain => 10000m,
                AgriculturalProductType.Vegetables => 15000m,
                AgriculturalProductType.Fruits => 20000m,
                AgriculturalProductType.Meat => 50000m,
                AgriculturalProductType.Dairy => 30000m,
                AgriculturalProductType.Poultry => 25000m,
                AgriculturalProductType.Fish => 40000m,
                _ => 10000m
            };
        }
    }
}
