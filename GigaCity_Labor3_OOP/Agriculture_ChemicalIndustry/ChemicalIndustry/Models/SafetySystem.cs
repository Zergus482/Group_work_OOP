using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.ChemicalIndustry.Models
{
    /// <summary>
    /// Система безопасности
    /// </summary>
    public class SafetySystem
    {
        public double SafetyLevel { get; set; } = 0.9;
        public double MaintenanceLevel { get; set; } = 0.8;
        public double TrainingLevel { get; set; } = 0.7;
        public DateTime LastInspection { get; set; }
        public int DaysSinceLastAccident { get; set; } = 0;

        public SafetySystem()
        {
            LastInspection = DateTime.Now;
        }

        /// <summary>
        /// Рассчитать общий уровень безопасности
        /// </summary>
        public double CalculateOverallSafety()
        {
            return (SafetyLevel * 0.4) + (MaintenanceLevel * 0.3) + (TrainingLevel * 0.3);
        }

        /// <summary>
        /// Провести проверку безопасности
        /// </summary>
        public void ConductInspection()
        {
            LastInspection = DateTime.Now;

            // Улучшаем уровень безопасности после проверки
            SafetyLevel = Math.Min(0.95, SafetyLevel + 0.05);
        }

        /// <summary>
        /// Обновить систему безопасности
        /// </summary>
        public void Update()
        {
            DaysSinceLastAccident++;

            // Постепенное ухудшение без обслуживания
            var daysSinceInspection = (DateTime.Now - LastInspection).Days;
            if (daysSinceInspection > 30)
            {
                var degradation = (daysSinceInspection - 30) * 0.001;
                SafetyLevel = Math.Max(0.5, SafetyLevel - degradation);
                MaintenanceLevel = Math.Max(0.5, MaintenanceLevel - degradation);
            }
        }

        /// <summary>
        /// Произошел несчастный случай
        /// </summary>
        public void AccidentOccurred()
        {
            DaysSinceLastAccident = 0;
            SafetyLevel = Math.Max(0.5, SafetyLevel - 0.1);
        }

        /// <summary>
        /// Инвестировать в безопасность
        /// </summary>
        public void InvestInSafety(decimal investment, string area)
        {
            var improvement = (double)(investment / 100000m) * 0.1;

            switch (area.ToLower())
            {
                case "safety":
                    SafetyLevel = Math.Min(0.99, SafetyLevel + improvement);
                    break;
                case "maintenance":
                    MaintenanceLevel = Math.Min(0.99, MaintenanceLevel + improvement);
                    break;
                case "training":
                    TrainingLevel = Math.Min(0.99, TrainingLevel + improvement);
                    break;
            }
        }
    }
}
