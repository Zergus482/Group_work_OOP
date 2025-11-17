using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.Agriculture.Models
{
    /// <summary>
    /// Специализированная теплица
    /// </summary>
    public class Greenhouse : Farm
    {
        public double Temperature { get; set; } = 25.0;
        public double Humidity { get; set; } = 60.0;
        public bool HasArtificialLighting { get; set; }
        public bool HasClimateControl { get; set; }

        public Greenhouse(string name, double area, int workerCapacity, decimal initialBudget)
            : base(name, FarmType.Greenhouse, area, workerCapacity, initialBudget)
        {
            HasArtificialLighting = true;
            HasClimateControl = true;
            Efficiency = 0.9; // Теплицы более эффективны
            DailyOperatingCost = Area * 300; // Высокие операционные затраты
        }

        /// <summary>
        /// Установить температуру
        /// </summary>
        public void SetTemperature(double temperature)
        {
            Temperature = temperature;
            UpdateEfficiencyBasedOnClimate();
        }

        /// <summary>
        /// Установить влажность
        /// </summary>
        public void SetHumidity(double humidity)
        {
            Humidity = humidity;
            UpdateEfficiencyBasedOnClimate();
        }

        private void UpdateEfficiencyBasedOnClimate()
        {
            // Идеальные условия: 22-28°C, 50-70% влажности
            var tempEfficiency = 1.0 - Math.Abs(Temperature - 25) * 0.02;
            var humidityEfficiency = 1.0 - Math.Abs(Humidity - 60) * 0.01;

            Efficiency = Math.Max(0.5, Math.Min(0.95, tempEfficiency * humidityEfficiency));
        }

        public override bool Upgrade()
        {
            if (base.Upgrade())
            {
                // Дополнительные бонусы для теплицы при улучшении
                if (UpgradeLevel >= 3)
                    HasClimateControl = true;
                if (UpgradeLevel >= 4)
                    HasArtificialLighting = true;

                return true;
            }
            return false;
        }
    }
}
