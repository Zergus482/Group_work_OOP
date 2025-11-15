using System;
using System.Collections.Generic;
using System.Linq;

namespace TheFinancialSystem
{
    public class OrganizationManager
    {
        private readonly List<Organization> _organizations = new List<Organization>();
        private readonly Random _random = new Random();

        public IReadOnlyList<Organization> Organizations => _organizations;

        public OrganizationManager()
        {
            InitializeOrganizations();
        }

        private void InitializeOrganizations()
        {
            // Создаем различные типы организаций
            _organizations.Add(new Organization("Городской университет", "Образование", 500_000m));
            _organizations.Add(new Organization("Завод 'Прогресс'", "Промышленность", 1_000_000m));
            _organizations.Add(new Organization("Технопарк 'Инновация'", "Технологии", 750_000m));
            _organizations.Add(new Organization("Торговый центр 'Городской'", "Торговля", 600_000m));
            _organizations.Add(new Organization("Больница 'Центральная'", "Здравоохранение", 800_000m));
            _organizations.Add(new Organization("Строительная компания 'Градстрой'", "Строительство", 900_000m));
            _organizations.Add(new Organization("Банк 'Городской'", "Финансы", 1_200_000m));
            _organizations.Add(new Organization("Транспортная компания 'ТрансГород'", "Транспорт", 700_000m));

            // Устанавливаем начальные параметры для каждой организации
            foreach (var org in _organizations)
            {
                // Генерируем случайные доходы и расходы
                org.Revenue = _random.Next(100_000, 500_000);
                org.Costs = _random.Next(80_000, 400_000);
            }
        }

        /// <summary>
        /// Получить организации с наибольшим бюджетом
        /// </summary>
        public List<Organization> GetTopOrganizations(int count = 5)
        {
            return _organizations
                .OrderByDescending(o => o.Budget)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Обновить финансовое состояние организаций
        /// </summary>
        public void UpdateOrganizations()
        {
            foreach (var org in _organizations)
            {
                // Добавляем доходы
                org.ReceiveMoney(org.Revenue, "Доходы от деятельности");

                // Вычитаем расходы
                org.SpendMoney(org.Costs, "Операционные расходы");

                // Добавляем случайные колебания
                var randomChange = _random.Next(-10_000, 20_000);
                if (randomChange > 0)
                    org.ReceiveMoney(randomChange, "Дополнительный доход");
                else
                    org.SpendMoney(-randomChange, "Непредвиденные расходы");
            }
        }
    }
}