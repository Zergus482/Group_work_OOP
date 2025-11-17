using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.ChemicalIndustry.Models
{
    /// <summary>
    /// Система управления загрязнением
    /// </summary>
    public class PollutionSystem
    {
        public Dictionary<string, double> PollutionSources { get; set; }
        public double TotalPollution { get; set; }
        public double PollutionCleanupRate { get; set; } = 0.1; // естественная очистка в день

        public PollutionSystem()
        {
            PollutionSources = new Dictionary<string, double>();
        }

        /// <summary>
        /// Добавить источник загрязнения
        /// </summary>
        public void AddPollution(string source, double amount)
        {
            if (PollutionSources.ContainsKey(source))
                PollutionSources[source] += amount;
            else
                PollutionSources[source] = amount;

            TotalPollution += amount;
        }

        /// <summary>
        /// Очистить загрязнение
        /// </summary>
        public void CleanPollution(double amount)
        {
            var cleanupPerSource = amount / PollutionSources.Count;

            foreach (var source in PollutionSources.Keys.ToList())
            {
                PollutionSources[source] = Math.Max(0, PollutionSources[source] - cleanupPerSource);
            }

            TotalPollution = Math.Max(0, TotalPollution - amount);
        }

        /// <summary>
        /// Обновить систему загрязнения
        /// </summary>
        public void Update()
        {
            // Естественная очистка
            CleanPollution(TotalPollution * PollutionCleanupRate);
        }

        /// <summary>
        /// Получить уровень загрязнения по источникам
        /// </summary>
        public Dictionary<string, double> GetPollutionBySource()
        {
            return new Dictionary<string, double>(PollutionSources);
        }
    }
}
