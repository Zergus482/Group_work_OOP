using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.ChemicalIndustry.Services
{
    /// <summary>
    /// Сервис контроля качества
    /// </summary>
    public class QualityControlService
    {
        private Dictionary<ChemicalPlant, double> _plantQuality;
        private Random _random;

        public QualityControlService()
        {
            _plantQuality = new Dictionary<ChemicalPlant, double>();
            _random = new Random();
        }

        /// <summary>
        /// Проверить качество продукции завода
        /// </summary>
        public void CheckQuality(ChemicalPlant plant)
        {
            var baseQuality = plant.SafetyLevel * 0.8 + plant.ProductionEfficiency * 0.2;

            // Случайные колебания качества
            var randomFactor = (_random.NextDouble() - 0.5) * 0.1;
            var quality = Math.Max(0.5, Math.Min(1.0, baseQuality + randomFactor));

            _plantQuality[plant] = quality;

            // Качество влияет на доход
            plant.Revenue = (decimal)((double)plant.Revenue * quality);
        }

        /// <summary>
        /// Получить качество продукции завода
        /// </summary>
        public double GetPlantQuality(ChemicalPlant plant)
        {
            return _plantQuality.ContainsKey(plant) ? _plantQuality[plant] : 0.8;
        }

        /// <summary>
        /// Провести аудит качества
        /// </summary>
        public QualityAuditResult ConductQualityAudit(ChemicalPlant plant)
        {
            var quality = GetPlantQuality(plant);
            var issues = new List<string>();

            if (quality < 0.7)
                issues.Add("Низкое качество продукции");
            if (plant.SafetyLevel < 0.8)
                issues.Add("Недостаточный уровень безопасности");
            if (plant.PollutionLevel > plant.MaxPollutionLevel * 0.5)
                issues.Add("Высокий уровень загрязнения");

            return new QualityAuditResult
            {
                Plant = plant,
                QualityScore = quality,
                Issues = issues,
                Passed = quality >= 0.8 && issues.Count == 0
            };
        }

        /// <summary>
        /// Получить среднее качество по всем заводам
        /// </summary>
        public double GetAverageQuality()
        {
            if (_plantQuality.Count == 0)
                return 0.8;

            return _plantQuality.Values.Average();
        }
    }

    /// <summary>
    /// Результат аудита качества
    /// </summary>
    public class QualityAuditResult
    {
        public ChemicalPlant Plant { get; set; }
        public double QualityScore { get; set; }
        public List<string> Issues { get; set; }
        public bool Passed { get; set; }

        public QualityAuditResult()
        {
            Issues = new List<string>();
        }
    }
}
