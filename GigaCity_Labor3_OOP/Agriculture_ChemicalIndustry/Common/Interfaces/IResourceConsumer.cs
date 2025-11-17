using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.Common.Interfaces
{
    /// <summary>
    /// Интерфейс для объектов, потребляющих ресурсы
    /// </summary>
    public interface IResourceConsumer
    {
        /// <summary>
        /// Получить ежедневные требования к ресурсам
        /// </summary>
        Dictionary<ResourceType, double> GetDailyResourceRequirements();

        /// <summary>
        /// Потреблять ресурсы
        /// </summary>
        void ConsumeResources(Dictionary<ResourceType, double> resources);

        /// <summary>
        /// Может ли объект работать с доступными ресурсами
        /// </summary>
        bool CanOperate(Dictionary<ResourceType, double> availableResources);
    }
}
