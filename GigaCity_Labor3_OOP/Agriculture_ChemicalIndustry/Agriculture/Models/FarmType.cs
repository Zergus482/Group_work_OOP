using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.Agriculture.Models
{
    /// <summary>
    /// Типы ферм
    /// </summary>
    public enum FarmType
    {
        FieldCrops,     // Полевые культуры
        Greenhouse,     // Теплица
        Plantation,     // Плантация
        Livestock,      // Животноводство
        PoultryFarm,    // Птицеферма
        FishFarm,       // Рыбоводство
        DairyFarm,      // Молочная ферма
        MixedFarm       // Смешанное хозяйство
    }
}
