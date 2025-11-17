using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.Common.Interfaces
{
    /// <summary>
    /// Интерфейс для объектов, производящих ресурсы
    /// </summary>
    public interface IResourceProducer
    {
        /// <summary>
        /// Получить ежедневное производство ресурсов
        /// </summary>
        Dictionary<ResourceType, double> GetDailyProduction();

        /// <summary>
        /// Произвести ресурсы
        /// </summary>
        void ProduceResources();
    }
}
