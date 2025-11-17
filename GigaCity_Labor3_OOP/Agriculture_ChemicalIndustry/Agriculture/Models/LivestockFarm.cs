using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.Agriculture.Models
{
    /// <summary>
    /// Животноводческая ферма
    /// </summary>
    public class LivestockFarm : Farm, IPollutable
    {
        public int AnimalCount { get; set; }
        public double AnimalHealth { get; set; } = 0.9;
        public double FeedConsumptionRate { get; set; } = 2.5; // кг корма на животное в день
        public double ManureProductionRate { get; set; } = 5.0; // кг навоза на животное в день

        // Реализация IPollutable
        public double PollutionLevel { get; set; }
        public double MaxPollutionLevel { get; } = 1000.0;

        public LivestockFarm(string name, double area, int workerCapacity, decimal initialBudget)
            : base(name, FarmType.Livestock, area, workerCapacity, initialBudget)
        {
            AnimalCount = (int)(Area * 10); // 10 животных на гектар
        }

        public override void ProcessProduction()
        {
            base.ProcessProduction();

            // Производство животноводческой продукции
            var dailyProduction = CalculateDailyAnimalProduction();
            foreach (var production in dailyProduction)
            {
                if (!Storage.ContainsKey(production.Key))
                    Storage[production.Key] = 0;
                Storage[production.Key] += production.Value;
            }

            // Генерация загрязнения
            GeneratePollution();
        }

        private Dictionary<AgriculturalProductType, double> CalculateDailyAnimalProduction()
        {
            var production = new Dictionary<AgriculturalProductType, double>();

            // Базовая продуктивность в зависимости от типа фермы
            if (Name.Contains("молоч") || Name.Contains("dairy", System.StringComparison.OrdinalIgnoreCase))
            {
                production[AgriculturalProductType.Milk] = AnimalCount * 20 * AnimalHealth; // литров молока в день
            }
            else if (Name.Contains("птиц") || Name.Contains("poultry", System.StringComparison.OrdinalIgnoreCase))
            {
                production[AgriculturalProductType.Eggs] = AnimalCount * 0.8 * AnimalHealth; // яиц в день
                production[AgriculturalProductType.Chicken] = AnimalCount * 0.01 * AnimalHealth; // мяса в день
            }
            else
            {
                production[AgriculturalProductType.Beef] = AnimalCount * 0.5 * AnimalHealth; // кг мяса в день
            }

            return production;
        }

        // Реализация IPollutable
        public void GeneratePollution()
        {
            PollutionLevel += AnimalCount * ManureProductionRate * 0.01;
            PollutionLevel = Math.Min(PollutionLevel, MaxPollutionLevel);

            // Загрязнение влияет на здоровье животных
            AnimalHealth = Math.Max(0.5, 0.9 - (PollutionLevel / MaxPollutionLevel) * 0.4);
        }

        public void CleanPollution(double amount)
        {
            PollutionLevel = Math.Max(0, PollutionLevel - amount);
        }

        public override Dictionary<ResourceType, double> GetDailyResourceRequirements()
        {
            var requirements = base.GetDailyResourceRequirements();

            // Добавляем требования для животных
            requirements[ResourceType.Grain] = AnimalCount * FeedConsumptionRate;
            requirements[ResourceType.Water] += AnimalCount * 50; // вода для животных

            return requirements;
        }
    }
}
