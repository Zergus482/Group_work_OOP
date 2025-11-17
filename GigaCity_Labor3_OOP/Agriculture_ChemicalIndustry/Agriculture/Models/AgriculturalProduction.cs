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
        public double YieldPerHectare { get; set; }
        public double AreaUsed { get; set; }
        public int GrowthDays { get; set; }
        public int DaysPlanted { get; set; }
        public double WaterRequirement { get; set; }
        public double FertilizerRequirement { get; set; }
        public double PesticideRequirement { get; set; }
        public double LaborRequirement { get; set; }

        public bool IsHarvestable
        {
            get { return DaysPlanted >= GrowthDays; }
        }

        public double Progress
        {
            get { return GrowthDays > 0 ? (double)DaysPlanted / GrowthDays : 0; }
        }

        public double CalculateExpectedYield()
        {
            return YieldPerHectare * AreaUsed;
        }

        public Dictionary<ResourceType, double> GetDailyRequirements()
        {
            return new Dictionary<ResourceType, double>
            {
                { ResourceType.Water, WaterRequirement * AreaUsed },
                { ResourceType.Fertilizers, FertilizerRequirement * AreaUsed },
                { ResourceType.Pesticides, PesticideRequirement * AreaUsed },
                { ResourceType.Labor, LaborRequirement * AreaUsed }
            };
        }

        public void Update()
        {
            if (!IsHarvestable)
            {
                DaysPlanted++;
            }
        }
    }
}
