using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.ChemicalIndustry.Services
{
    /// <summary>
    /// Сервис управления отходами
    /// </summary>
    public class WasteManagementService
    {
        private Dictionary<ChemicalPlant, double> _plantWaste;
        private double _totalWaste;
        private double _recyclingRate;

        public WasteManagementService()
        {
            _plantWaste = new Dictionary<ChemicalPlant, double>();
            _totalWaste = 0;
            _recyclingRate = 0.3; // 30% переработки по умолчанию
        }

        /// <summary>
        /// Обработать отходы завода
        /// </summary>
        public void ProcessWaste(ChemicalPlant plant)
        {
            var wasteProduced = plant.CurrentProduction * 0.05; // 5% от производства - отходы

            if (_plantWaste.ContainsKey(plant))
                _plantWaste[plant] += wasteProduced;
            else
                _plantWaste[plant] = wasteProduced;

            _totalWaste += wasteProduced;

            // Автоматическая переработка части отходов
            var recycledWaste = wasteProduced * _recyclingRate;
            _totalWaste -= recycledWaste;
            _plantWaste[plant] -= recycledWaste;
        }

        /// <summary>
        /// Обновить сервис
        /// </summary>
        public void Update()
        {
            // Естественное разложение отходов
            var naturalDecomposition = _totalWaste * 0.02;
            _totalWaste -= naturalDecomposition;

            foreach (var plant in _plantWaste.Keys.ToList())
            {
                _plantWaste[plant] = Math.Max(0, _plantWaste[plant] - naturalDecomposition / _plantWaste.Count);
            }
        }

        /// <summary>
        /// Улучшить систему переработки
        /// </summary>
        public void UpgradeRecycling()
        {
            _recyclingRate = Math.Min(0.8, _recyclingRate + 0.1);
        }

        /// <summary>
        /// Очистить отходы на конкретном заводе
        /// </summary>
        public void CleanWaste(ChemicalPlant plant, double amount)
        {
            if (_plantWaste.ContainsKey(plant))
            {
                _plantWaste[plant] = Math.Max(0, _plantWaste[plant] - amount);
                _totalWaste -= amount;
            }
        }

        /// <summary>
        /// Получить общее количество отходов
        /// </summary>
        public double GetTotalWaste()
        {
            return _totalWaste;
        }

        /// <summary>
        /// Получить отходы по заводам
        /// </summary>
        public Dictionary<ChemicalPlant, double> GetWasteByPlant()
        {
            return new Dictionary<ChemicalPlant, double>(_plantWaste);
        }
    }
}
