using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.Agriculture.Models
{
    /// <summary>
    /// Плантация для выращивания многолетних культур
    /// </summary>
    public class Plantation : Farm
    {
        public bool HasIrrigationSystem { get; set; }
        public int YearsEstablished { get; set; }

        public Plantation(string name, double area, int workerCapacity, decimal initialBudget)
            : base(name, FarmType.Plantation, area, workerCapacity, initialBudget)
        {
            HasIrrigationSystem = true;
            YearsEstablished = 1;
        }

        /// <summary>
        /// Обновить плантацию (вызывается ежегодно)
        /// </summary>
        public void UpdatePlantation()
        {
            YearsEstablished++;

            // Плантации становятся более продуктивными с возрастом
            if (YearsEstablished > 5)
            {
                Efficiency = Math.Min(0.95, 0.8 + (YearsEstablished - 5) * 0.03);
            }
        }

        public override bool Upgrade()
        {
            if (base.Upgrade())
            {
                if (UpgradeLevel >= 2)
                    HasIrrigationSystem = true;
                return true;
            }
            return false;
        }
    }
}
