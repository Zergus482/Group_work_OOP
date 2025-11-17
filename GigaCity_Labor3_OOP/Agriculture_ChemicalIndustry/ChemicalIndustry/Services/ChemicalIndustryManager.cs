using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.ChemicalIndustry.Services
{
    /// <summary>
    /// Менеджер химической промышленности
    /// </summary>
    public class ChemicalIndustryManager
    {
        private OrganizationManager _organizationManager;
        private List<ChemicalPlant> _plants;
        private WasteManagementService _wasteService;
        private QualityControlService _qualityService;

        public IEnumerable<ChemicalPlant> Plants => _plants;
        public decimal TotalRevenue => _plants.Sum(p => p.Revenue);
        public decimal TotalCosts => _plants.Sum(p => p.Costs);
        public double TotalPollution => _plants.Sum(p => p.PollutionLevel);

        public ChemicalIndustryManager(OrganizationManager organizationManager)
        {
            _organizationManager = organizationManager;
            _plants = new List<ChemicalPlant>();
            _wasteService = new WasteManagementService();
            _qualityService = new QualityControlService();
        }

        /// <summary>
        /// Добавить химический завод
        /// </summary>
        public void AddPlant(ChemicalPlant plant)
        {
            _plants.Add(plant);
            _organizationManager.RegisterOrganization(plant);
        }

        /// <summary>
        /// Удалить химический завод
        /// </summary>
        public bool RemovePlant(ChemicalPlant plant)
        {
            return _plants.Remove(plant);
        }

        /// <summary>
        /// Обработать ежедневное производство
        /// </summary>
        public void ProcessProduction(Dictionary<ResourceType, double> cityResources)
        {
            foreach (var plant in _plants)
            {
                plant.ProcessProduction(cityResources);

                // Обновляем сервисы
                _wasteService.ProcessWaste(plant);
                _qualityService.CheckQuality(plant);

                // Автоматически выплачиваем зарплаты
                if (plant.Employees.Any() && plant.Budget > 1000)
                {
                    plant.PaySalaries();
                }
            }

            // Обновляем управление отходами
            _wasteService.Update();
        }

        /// <summary>
        /// Получить заводы по типу продукции
        /// </summary>
        public List<ChemicalPlant> GetPlantsByProductType(ChemicalProductType productType)
        {
            return _plants.Where(p => p.MainProductType == productType).ToList();
        }

        /// <summary>
        /// Получить заводы с высоким уровнем загрязнения
        /// </summary>
        public List<ChemicalPlant> GetHighPollutionPlants(double threshold = 500)
        {
            return _plants.Where(p => p.PollutionLevel > threshold).ToList();
        }

        /// <summary>
        /// Получить общую статистику химической промышленности
        /// </summary>
        public ChemicalIndustryStatistics GetStatistics()
        {
            return new ChemicalIndustryStatistics
            {
                TotalPlants = _plants.Count,
                TotalProductionCapacity = _plants.Sum(p => p.ProductionCapacity),
                TotalCurrentProduction = _plants.Sum(p => p.CurrentProduction),
                TotalRevenue = TotalRevenue,
                TotalCosts = TotalCosts,
                TotalProfit = TotalRevenue - TotalCosts,
                TotalPollution = TotalPollution,
                ProductTypes = _plants.GroupBy(p => p.MainProductType)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        /// <summary>
        /// Очистить загрязнение на всех заводах
        /// </summary>
        public void CleanPollution(double amount)
        {
            var amountPerPlant = amount / _plants.Count;
            foreach (var plant in _plants)
            {
                plant.CleanPollution(amountPerPlant);
            }
        }
    }

    /// <summary>
    /// Статистика химической промышленности
    /// </summary>
    public class ChemicalIndustryStatistics
    {
        public int TotalPlants { get; set; }
        public double TotalProductionCapacity { get; set; }
        public double TotalCurrentProduction { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCosts { get; set; }
        public decimal TotalProfit { get; set; }
        public double TotalPollution { get; set; }
        public Dictionary<ChemicalProductType, int> ProductTypes { get; set; } = new Dictionary<ChemicalProductType, int>();
    }
}
