using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.Agriculture.Services
{
    public class IndustryIntegrationService
    {
        private AgricultureManager _agricultureManager;
        private ChemicalIndustryManager _chemicalManager;
        private OrganizationManager _organizationManager;
        private Dictionary<ResourceType, double> _cityResources;

        public IndustryIntegrationService(OrganizationManager organizationManager)
        {
            _organizationManager = organizationManager;
            _agricultureManager = new AgricultureManager(organizationManager);
            _chemicalManager = new ChemicalIndustryManager(organizationManager);
            InitializeCityResources();
        }

        private void InitializeCityResources()
        {
            _cityResources = new Dictionary<ResourceType, double>
            {
                [ResourceType.Electricity] = 10000,
                [ResourceType.Water] = 50000,
                [ResourceType.Oil] = 5000,
                [ResourceType.Chemicals] = 3000
            };
        }

        public void ProcessDailyIndustries()
        {
            // Обрабатываем сельское хозяйство
            _agricultureManager.ProcessDay();

            // Обрабатываем химическую промышленность
            _chemicalManager.ProcessProduction(_cityResources);

            // Обновляем городские ресурсы (регенерация)
            RegenerateCityResources();
        }

        private void RegenerateCityResources()
        {
            // Базовая регенерация ресурсов
            _cityResources[ResourceType.Electricity] += 1000;
            _cityResources[ResourceType.Water] += 5000;

            // Ограничиваем максимальные значения
            foreach (var resource in _cityResources.Keys.ToList())
            {
                if (_cityResources[resource] > 100000)
                    _cityResources[resource] = 100000;
            }
        }

        public Dictionary<string, decimal> GetIndustryFinancialReport()
        {
            return new Dictionary<string, decimal>
            {
                ["AgricultureRevenue"] = _agricultureManager.GetTotalAgriculturalRevenue(),
                ["AgricultureCosts"] = _agricultureManager.GetTotalAgriculturalCosts(),
                ["ChemicalRevenue"] = _chemicalManager.GetTotalChemicalRevenue(),
                ["ChemicalCosts"] = _chemicalManager.GetTotalChemicalCosts()
            };
        }
    }
}
