using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.Common.Interfaces
{
    /// <summary>
    /// Интерфейс для объектов, которые можно улучшать
    /// </summary>
    public interface IUpgradable
    {
        /// <summary>
        /// Текущий уровень улучшения
        /// </summary>
        int UpgradeLevel { get; }

        /// <summary>
        /// Максимальный уровень улучшения
        /// </summary>
        int MaxUpgradeLevel { get; }

        /// <summary>
        /// Стоимость улучшения до следующего уровня
        /// </summary>
        decimal GetUpgradeCost();

        /// <summary>
        /// Улучшить объект
        /// </summary>
        bool Upgrade();
    }
}
