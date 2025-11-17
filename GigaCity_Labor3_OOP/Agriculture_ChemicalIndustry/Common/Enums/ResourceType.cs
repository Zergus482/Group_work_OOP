using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.Common.Enums
{
    /// <summary>
    /// Типы ресурсов в системе
    /// </summary>
    public enum ResourceType
    {
        // Энергетические ресурсы
        Electricity,
        Water,
        Labor,

        // Сельскохозяйственная продукция
        Grain,
        Vegetables,
        Fruits,
        Meat,
        Dairy,
        Poultry,
        Fish,
        Eggs,
        Wool,

        // Химическая продукция
        Fertilizers,
        Pesticides,
        Herbicides,
        Pharmaceuticals,
        Plastics,
        Petrochemicals,
        BasicChemicals,
        SyntheticMaterials,

        // Сырьевые ресурсы
        Oil,
        Gas,
        Minerals,
        Chemicals,
        RawMaterials,

        // Побочные продукты
        Waste,
        Pollution,
        OrganicWaste
    }
}
