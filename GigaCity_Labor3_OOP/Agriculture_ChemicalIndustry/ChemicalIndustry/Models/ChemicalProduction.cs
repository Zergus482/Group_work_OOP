using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.ChemicalIndustry.Models
{
    /// <summary>
    /// Модель химического производства
    /// </summary>
    public class ChemicalProduction
    {
        public ChemicalProductType ProductType { get; set; }
        public double ProductionRate { get; set; } // тонн в день
        public Dictionary<ResourceType, double> InputMaterials { get; set; }
        public Dictionary<ResourceType, double> OutputProducts { get; set; }
        public Dictionary<ResourceType, double> WasteProducts { get; set; }
        public double Quality { get; set; } = 1.0;
        public double EnergyConsumption { get; set; } // МДж на тонну

        public ChemicalProduction()
        {
            InputMaterials = new Dictionary<ResourceType, double>();
            OutputProducts = new Dictionary<ResourceType, double>();
            WasteProducts = new Dictionary<ResourceType, double>();
        }

        /// <summary>
        /// Может ли производство работать с доступными ресурсами
        /// </summary>
        public bool CanProduce(Dictionary<ResourceType, double> availableResources)
        {
            foreach (var input in InputMaterials)
            {
                if (!availableResources.ContainsKey(input.Key) ||
                    availableResources[input.Key] < input.Value * ProductionRate)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Рассчитать эффективность производства
        /// </summary>
        public double CalculateEfficiency(Dictionary<ResourceType, double> availableResources)
        {
            var efficiency = 1.0;

            foreach (var input in InputMaterials)
            {
                if (availableResources.ContainsKey(input.Key))
                {
                    var available = availableResources[input.Key];
                    var required = input.Value * ProductionRate;
                    if (available < required)
                    {
                        efficiency = Math.Min(efficiency, available / required);
                    }
                }
                else
                {
                    efficiency = 0;
                }
            }

            return efficiency;
        }
    }
}
