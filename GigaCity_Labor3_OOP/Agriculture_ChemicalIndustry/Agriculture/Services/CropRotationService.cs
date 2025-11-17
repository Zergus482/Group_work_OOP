using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.Agriculture.Services
{
    /// <summary>
    /// Сервис севооборота
    /// </summary>
    public class CropRotationService
    {
        private Dictionary<AgriculturalProductType, AgriculturalProductType[]> _rotationPlans;

        public CropRotationService()
        {
            InitializeRotationPlans();
        }

        private void InitializeRotationPlans()
        {
            _rotationPlans = new Dictionary<AgriculturalProductType, AgriculturalProductType[]>
            {
                [AgriculturalProductType.Wheat] = new[] { AgriculturalProductType.Soybeans, AgriculturalProductType.Corn },
                [AgriculturalProductType.Corn] = new[] { AgriculturalProductType.Soybeans, AgriculturalProductType.Wheat },
                [AgriculturalProductType.Potatoes] = new[] { AgriculturalProductType.Carrots, AgriculturalProductType.Tomatoes }
            };
        }

        /// <summary>
        /// Получить рекомендуемые культуры для севооборота
        /// </summary>
        public AgriculturalProductType[] GetRecommendedRotation(AgriculturalProductType currentCrop)
        {
            return _rotationPlans.ContainsKey(currentCrop) ?
                _rotationPlans[currentCrop] :
                new AgriculturalProductType[0];
        }

        /// <summary>
        /// Проверить, хороший ли севооборот
        /// </summary>
        public bool IsGoodRotation(AgriculturalProductType previousCrop, AgriculturalProductType nextCrop)
        {
            if (_rotationPlans.ContainsKey(previousCrop))
            {
                foreach (var recommended in _rotationPlans[previousCrop])
                {
                    if (recommended == nextCrop)
                        return true;
                }
            }
            return false;
        }
    }
}
