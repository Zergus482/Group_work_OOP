using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.Agriculture.Services
{
    /// <summary>
    /// Сервис сезонных эффектов
    /// </summary>
    public class SeasonalEffectsService
    {
        private int _currentDay = 0;
        private readonly string[] _seasons = { "Spring", "Summer", "Autumn", "Winter" };

        public string CurrentSeason => _seasons[(_currentDay / 90) % 4]; // 90 дней на сезон

        /// <summary>
        /// Получить сезонный модификатор эффективности
        /// </summary>
        public double GetSeasonalModifier()
        {
            return CurrentSeason switch
            {
                "Spring" => 0.9,
                "Summer" => 1.0,
                "Autumn" => 0.8,
                "Winter" => 0.5,
                _ => 1.0
            };
        }

        /// <summary>
        /// Обновить сервис (вызывается ежедневно)
        /// </summary>
        public void Update()
        {
            _currentDay++;
        }

        /// <summary>
        /// Получить текущий день года
        /// </summary>
        public int GetCurrentDayOfYear()
        {
            return _currentDay % 360;
        }
    }
}
