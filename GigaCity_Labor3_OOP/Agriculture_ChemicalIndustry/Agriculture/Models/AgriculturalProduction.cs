using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.Agriculture.Models
{
    /// <summary>
    /// Модель сельскохозяйственного производства
    /// </summary>
    public class AgriculturalProduction
    {
        public AgriculturalProductType ProductType { get; set; }
        public double YieldPerHectare { get; set; } // урожайность с гектара (тонн/га)
        public double AreaUsed { get; set; } // используемая площадь (гектары)
        public int GrowthDays { get; set; } // дней до созревания
        public int DaysPlanted { get; set; } // дней с посадки
        public double WaterRequirement { get; set; } // литров в день на гектар
        public double FertilizerRequirement { get; set; } // кг в день на гектар
        public double PesticideRequirement { get; set; } // кг в день на гектар
        public double LaborRequirement { get; set; } // часов труда на гектар в день

        /// <summary>
        /// Готова ли культура к сбору урожая
        /// </summary>
        public bool IsHarvestable => DaysPlanted >= GrowthDays;

        /// <summary>
        /// Прогресс роста (0-1)
        /// </summary>
        public double Progress => GrowthDays > 0 ? (double)DaysPlanted / GrowthDays : 0;

        /// <summary>
        /// Рассчитать ожидаемый урожай
        /// </summary>
        public double CalculateExpectedYield() => YieldPerHectare * AreaUsed;

        /// <summary>
        /// Получить ежедневные требования к ресурсам
        /// </summary>
        public Dictionary<ResourceType, double> GetDailyRequirements()
        {
            return new Dictionary<ResourceType, double>
            {
                [ResourceType.Water] = WaterRequirement * AreaUsed,
                [ResourceType.Fertilizers] = FertilizerRequirement * AreaUsed,
                [ResourceType.Pesticides] = PesticideRequirement * AreaUsed,
                [ResourceType.Labor] = LaborRequirement * AreaUsed
            };
        }

        /// <summary>
        /// Обновить состояние производства
        /// </summary>
        public void Update()
        {
            if (!IsHarvestable)
            {
                DaysPlanted++;
            }
        }
    }
}
