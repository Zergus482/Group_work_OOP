using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.Agriculture.Services
{
    /// <summary>
    /// Менеджер сельского хозяйства
    /// </summary>
    public class AgricultureManager
    {
        private OrganizationManager _organizationManager;
        private List<Farm> _farms;
        private SeasonalEffectsService _seasonalService;

        public IEnumerable<Farm> Farms => _farms;
        public decimal TotalRevenue => _farms.Sum(f => f.Revenue);
        public decimal TotalCosts => _farms.Sum(f => f.Costs);

        public AgricultureManager(OrganizationManager organizationManager)
        {
            _organizationManager = organizationManager;
            _farms = new List<Farm>();
            _seasonalService = new SeasonalEffectsService();
        }

        /// <summary>
        /// Добавить ферму
        /// </summary>
        public void AddFarm(Farm farm)
        {
            _farms.Add(farm);
            _organizationManager.RegisterOrganization(farm);
        }

        /// <summary>
        /// Удалить ферму
        /// </summary>
        public bool RemoveFarm(Farm farm)
        {
            return _farms.Remove(farm);
        }

        /// <summary>
        /// Обработать ежедневное обновление ферм
        /// </summary>
        public void ProcessDailyUpdate(Dictionary<ResourceType, double> cityResources)
        {
            var seasonalModifier = _seasonalService.GetSeasonalModifier();

            foreach (var farm in _farms)
            {
                // Применяем сезонные модификаторы
                if (farm is not Greenhouse) // Теплицы не зависят от сезона
                {
                    farm.Efficiency *= seasonalModifier;
                }

                // Проверяем доступность ресурсов
                if (farm.CanOperate(cityResources))
                {
                    farm.ProcessProduction();
                    farm.ConsumeResources(cityResources);

                    // Автоматически выплачиваем зарплаты
                    if (farm.Employees.Any() && farm.Budget > 1000)
                    {
                        farm.PaySalaries();
                    }
                }
            }

            // Обновляем сезонный сервис
            _seasonalService.Update();
        }

        /// <summary>
        /// Получить фермы по типу
        /// </summary>
        public List<Farm> GetFarmsByType(FarmType farmType)
        {
            return _farms.Where(f => f.Type == farmType).ToList();
        }

        /// <summary>
        /// Получить общую статистику сельского хозяйства
        /// </summary>
        public AgricultureStatistics GetStatistics()
        {
            return new AgricultureStatistics
            {
                TotalFarms = _farms.Count,
                TotalArea = _farms.Sum(f => f.Area),
                TotalRevenue = TotalRevenue,
                TotalCosts = TotalCosts,
                TotalProfit = TotalRevenue - TotalCosts,
                FarmTypes = _farms.GroupBy(f => f.Type)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        /// <summary>
        /// Найти фермы, готовые к сбору урожая
        /// </summary>
        public List<Farm> GetFarmsReadyForHarvest()
        {
            return _farms.Where(f => f.CurrentProductions.Any(p => p.IsHarvestable)).ToList();
        }
    }

    /// <summary>
    /// Статистика сельского хозяйства
    /// </summary>
    public class AgricultureStatistics
    {
        public int TotalFarms { get; set; }
        public double TotalArea { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCosts { get; set; }
        public decimal TotalProfit { get; set; }
        public Dictionary<FarmType, int> FarmTypes { get; set; } = new Dictionary<FarmType, int>();
    }
}
