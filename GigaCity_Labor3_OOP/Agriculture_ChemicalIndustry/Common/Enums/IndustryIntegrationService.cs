using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.Common.Enums
{
    /// <summary>
    /// Сервис интеграции отраслей
    /// </summary>
    public class IndustryIntegrationService
    {
        private AgricultureManager _agricultureManager;
        private ChemicalIndustryManager _chemicalManager;
        private OrganizationManager _organizationManager;
        private Dictionary<ResourceType, double> _cityResources;
        private Dictionary<ResourceType, double> _cityProduction;

        public AgricultureManager AgricultureManager => _agricultureManager;
        public ChemicalIndustryManager ChemicalManager => _chemicalManager;

        public IndustryIntegrationService(OrganizationManager organizationManager)
        {
            _organizationManager = organizationManager;
            _agricultureManager = new AgricultureManager(organizationManager);
            _chemicalManager = new ChemicalIndustryManager(organizationManager);
            _cityResources = new Dictionary<ResourceType, double>();
            _cityProduction = new Dictionary<ResourceType, double>();

            InitializeCityResources();
        }

        private void InitializeCityResources()
        {
            // Инициализация базовых ресурсов города
            _cityResources[ResourceType.Electricity] = 100000;
            _cityResources[ResourceType.Water] = 500000;
            _cityResources[ResourceType.Labor] = 10000;
            _cityResources[ResourceType.Oil] = 50000;
            _cityResources[ResourceType.Gas] = 40000;
            _cityResources[ResourceType.Chemicals] = 30000;
            _cityResources[ResourceType.RawMaterials] = 60000;

            // Инициализация производства
            foreach (ResourceType resource in Enum.GetValues(typeof(ResourceType)))
            {
                _cityProduction[resource] = 0;
            }
        }

        /// <summary>
        /// Обработать ежедневное обновление всех отраслей
        /// </summary>
        public void ProcessDailyIndustries()
        {
            // Собираем производство от всех отраслей
            UpdateCityProduction();

            // Обрабатываем сельское хозяйство
            _agricultureManager.ProcessDailyUpdate(_cityResources);

            // Обрабатываем химическую промышленность
            _chemicalManager.ProcessProduction(_cityResources);

            // Обновляем городские ресурсы
            UpdateCityResources();

            // Регенерируем базовые ресурсы
            RegenerateBaseResources();
        }

        private void UpdateCityProduction()
        {
            // Собираем производство от сельского хозяйства
            foreach (var farm in _agricultureManager.Farms)
            {
                var production = farm.GetDailyProduction();
                foreach (var item in production)
                {
                    _cityProduction[item.Key] += item.Value;
                }
            }

            // Собираем производство от химической промышленности
            foreach (var plant in _chemicalManager.Plants)
            {
                var production = plant.GetDailyProduction();
                foreach (var item in production)
                {
                    _cityProduction[item.Key] += item.Value;
                }
            }
        }

        private void UpdateCityResources()
        {
            // Добавляем произведенные ресурсы
            foreach (var production in _cityProduction)
            {
                if (_cityResources.ContainsKey(production.Key))
                    _cityResources[production.Key] += production.Value;
                else
                    _cityResources[production.Key] = production.Value;
            }

            // Очищаем производство для следующего дня
            foreach (var resource in _cityProduction.Keys.ToList())
            {
                _cityProduction[resource] = 0;
            }
        }

        private void RegenerateBaseResources()
        {
            // Базовая регенерация ресурсов
            _cityResources[ResourceType.Electricity] += 10000;
            _cityResources[ResourceType.Water] += 50000;
            _cityResources[ResourceType.Labor] += 1000;

            // Ограничиваем максимальные значения
            foreach (var resource in _cityResources.Keys.ToList())
            {
                if (_cityResources[resource] > 1000000)
                    _cityResources[resource] = 1000000;
            }
        }

        /// <summary>
        /// Получить финансовый отчет по отраслям
        /// </summary>
        public IndustryFinancialReport GetFinancialReport()
        {
            var agriStats = _agricultureManager.GetStatistics();
            var chemStats = _chemicalManager.GetStatistics();

            return new IndustryFinancialReport
            {
                AgricultureRevenue = agriStats.TotalRevenue,
                AgricultureCosts = agriStats.TotalCosts,
                AgricultureProfit = agriStats.TotalProfit,
                ChemicalRevenue = chemStats.TotalRevenue,
                ChemicalCosts = chemStats.TotalCosts,
                ChemicalProfit = chemStats.TotalProfit,
                TotalRevenue = agriStats.TotalRevenue + chemStats.TotalRevenue,
                TotalCosts = agriStats.TotalCosts + chemStats.TotalCosts,
                TotalProfit = agriStats.TotalProfit + chemStats.TotalProfit,
                ReportDate = DateTime.Now
            };
        }

        /// <summary>
        /// Получить отчет по ресурсам
        /// </summary>
        public ResourceReport GetResourceReport()
        {
            return new ResourceReport
            {
                ResourceLevels = new Dictionary<ResourceType, double>(_cityResources),
                ProductionLevels = new Dictionary<ResourceType, double>(_cityProduction),
                TotalResourceConsumption = _cityResources.Values.Sum(),
                TotalProduction = _cityProduction.Values.Sum()
            };
        }

        /// <summary>
        /// Добавить ресурсы в город
        /// </summary>
        public void AddResources(ResourceType resourceType, double amount)
        {
            if (_cityResources.ContainsKey(resourceType))
                _cityResources[resourceType] += amount;
            else
                _cityResources[resourceType] = amount;
        }

        /// <summary>
        /// Потребить ресурсы города
        /// </summary>
        public bool ConsumeResources(ResourceType resourceType, double amount)
        {
            if (_cityResources.ContainsKey(resourceType) && _cityResources[resourceType] >= amount)
            {
                _cityResources[resourceType] -= amount;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Финансовый отчет отраслей
    /// </summary>
    public class IndustryFinancialReport
    {
        public decimal AgricultureRevenue { get; set; }
        public decimal AgricultureCosts { get; set; }
        public decimal AgricultureProfit { get; set; }
        public decimal ChemicalRevenue { get; set; }
        public decimal ChemicalCosts { get; set; }
        public decimal ChemicalProfit { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCosts { get; set; }
        public decimal TotalProfit { get; set; }
        public DateTime ReportDate { get; set; }
    }

    /// <summary>
    /// Отчет по ресурсам
    /// </summary>
    public class ResourceReport
    {
        public Dictionary<ResourceType, double> ResourceLevels { get; set; }
        public Dictionary<ResourceType, double> ProductionLevels { get; set; }
        public double TotalResourceConsumption { get; set; }
        public double TotalProduction { get; set; }

        public ResourceReport()
        {
            ResourceLevels = new Dictionary<ResourceType, double>();
            ProductionLevels = new Dictionary<ResourceType, double>();
        }
    }
}
