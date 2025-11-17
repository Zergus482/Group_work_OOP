using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.Common.Interfaces
{
    /// <summary>
    /// Интерфейс для объектов, создающих загрязнение
    /// </summary>
    public interface IPollutable
    {
        /// <summary>
        /// Уровень загрязнения (0-1)
        /// </summary>
        double PollutionLevel { get; set; }

        /// <summary>
        /// Максимальный допустимый уровень загрязнения
        /// </summary>
        double MaxPollutionLevel { get; }

        /// <summary>
        /// Производить загрязнение
        /// </summary>
        void GeneratePollution();

        /// <summary>
        /// Очистить загрязнение
        /// </summary>
        void CleanPollution(double amount);
    }
}
