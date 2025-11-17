using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.Agriculture.Services
{
    /// <summary>
    /// Сервис орошения
    /// </summary>
    public class IrrigationService
    {
        public double WaterEfficiency { get; set; } = 0.7;

        /// <summary>
        /// Рассчитать потребность в воде для фермы
        /// </summary>
        public double CalculateWaterRequirement(double area, string cropType, string season)
        {
            var baseRequirement = area * 1000; // базовое требование: 1000 л/га

            // Модификаторы по типу культуры
            var cropModifier = cropType.ToLower() switch
            {
                "rice" => 1.5,
                "corn" => 1.2,
                "wheat" => 1.0,
                "vegetables" => 1.3,
                _ => 1.0
            };

            // Модификаторы по сезону
            var seasonModifier = season.ToLower() switch
            {
                "summer" => 1.4,
                "spring" => 1.1,
                "autumn" => 0.9,
                "winter" => 0.5,
                _ => 1.0
            };

            return baseRequirement * cropModifier * seasonModifier * (1 / WaterEfficiency);
        }

        /// <summary>
        /// Улучшить эффективность орошения
        /// </summary>
        public void UpgradeIrrigation()
        {
            WaterEfficiency = Math.Min(0.95, WaterEfficiency + 0.1);
        }
    }
}
