using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using GigaCity_Labor3_OOP.Models.ForeignRelations;
using GigaCity_Labor3_OOP.Models;

namespace GigaCity_Labor3_OOP.ViewModels
{
    public class ForeignRelationsViewModel : INotifyPropertyChanged
    {
        private readonly Random _random = new Random();
        private readonly PopulationManager _populationManager;
        private DispatcherTimer _immigrationTimer;
        private DispatcherTimer _tradeTimer;
        private DispatcherTimer _relationsTimer;
        private string _statusMessage;

        public ObservableCollection<Country> Countries { get; private set; }
        public ObservableCollection<Immigrant> Immigrants { get; private set; }
        public ObservableCollection<Import> Imports { get; private set; }
        public ObservableCollection<Export> Exports { get; private set; }
        public ObservableCollection<TradeDeal> TradeDeals { get; private set; }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public ForeignRelationsViewModel(PopulationManager populationManager = null)
        {
            _populationManager = populationManager;
            Countries = new ObservableCollection<Country>();
            Immigrants = new ObservableCollection<Immigrant>();
            Imports = new ObservableCollection<Import>();
            Exports = new ObservableCollection<Export>();
            TradeDeals = new ObservableCollection<TradeDeal>();

            InitializeDefaultCountries();
            StartTimers();
        }

        private void InitializeDefaultCountries()
        {
            var countries = new[]
            {
                new Country { Name = "Соседняя страна А", Code = "COUNTRY_A", RelationsLevel = 60 },
                new Country { Name = "Соседняя страна Б", Code = "COUNTRY_B", RelationsLevel = 45 },
                new Country { Name = "Соседняя страна В", Code = "COUNTRY_C", RelationsLevel = 75 },
                new Country { Name = "Соседняя страна Г", Code = "COUNTRY_D", RelationsLevel = 30 }
            };

            foreach (var country in countries)
            {
                Countries.Add(country);
            }
        }

        private void StartTimers()
        {
            // Таймер для миграции
            _immigrationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(15)
            };
            _immigrationTimer.Tick += ImmigrationTimer_Tick;
            _immigrationTimer.Start();

            // Таймер для торговых сделок
            _tradeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(20)
            };
            _tradeTimer.Tick += TradeTimer_Tick;
            _tradeTimer.Start();

            // Таймер для изменения отношений
            _relationsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _relationsTimer.Tick += RelationsTimer_Tick;
            _relationsTimer.Start();
        }

        private void ImmigrationTimer_Tick(object sender, EventArgs e)
        {
            // Случайная миграция с вероятностью 30%
            if (_random.Next(100) < 30 && Countries.Any())
            {
                ProcessImmigration();
            }
        }

        private void ProcessImmigration()
        {
            var country = Countries[_random.Next(Countries.Count)];

            int immigrationChance = Math.Max(0, country.RelationsLevel + 20);
            if (_random.Next(100) < immigrationChance)
            {
                var reasons = Enum.GetValues(typeof(ImmigrationReason)).Cast<ImmigrationReason>().ToArray();
                var reason = reasons[_random.Next(reasons.Length)];

                var immigrant = new Immigrant
                {
                    Name = $"Иммигрант #{_random.Next(1000, 9999)}",
                    CountryOfOrigin = country,
                    Reason = reason
                };

                Immigrants.Add(immigrant);
                StatusMessage = $"Новый иммигрант из {country.Name}: {reason}";

                // Добавляем иммигранта в население
                if (_populationManager != null)
                {
                    _populationManager.AddHuman(new Human(immigrant.Name));
                }
            }
        }

        private void TradeTimer_Tick(object sender, EventArgs e)
        {
            // Случайная торговая сделка с вероятностью 20%
            if (_random.Next(100) < 20 && Countries.Any())
            {
                ProcessTrade();
            }
        }

        private void ProcessTrade()
        {
            var country = Countries[_random.Next(Countries.Count)];

            // Импорт
            if (_random.Next(100) < 50)
            {
                var resourceTypes = Enum.GetValues(typeof(Models.ForeignRelations.ResourceType)).Cast<Models.ForeignRelations.ResourceType>().ToArray();
                var resourceType = resourceTypes[_random.Next(resourceTypes.Length)];

                var import = new Import
                {
                    SourceCountry = country,
                    ResourceType = resourceType,
                    Quantity = _random.Next(10, 100),
                    Cost = (decimal)(_random.Next(1000, 5000))
                };

                Imports.Add(import);
                StatusMessage = $"Импорт из {country.Name}: {resourceType} ({import.Quantity} ед.)";
            }

            // Экспорт
            if (_random.Next(100) < 50)
            {
                var resourceTypes = Enum.GetValues(typeof(Models.ForeignRelations.ResourceType)).Cast<Models.ForeignRelations.ResourceType>().ToArray();
                var resourceType = resourceTypes[_random.Next(resourceTypes.Length)];

                var export = new Export
                {
                    DestinationCountry = country,
                    ResourceType = resourceType,
                    Quantity = _random.Next(10, 100),
                    Revenue = (decimal)(_random.Next(1000, 5000))
                };

                Exports.Add(export);
                StatusMessage = $"Экспорт в {country.Name}: {resourceType} ({export.Quantity} ед.)";
            }
        }

        private void RelationsTimer_Tick(object sender, EventArgs e)
        {
            // Случайное изменение отношений со странами (50% шанс)
            if (_random.Next(100) < 50 && Countries.Any())
            {
                ChangeRandomCountryRelations();
            }
        }

        private void ChangeRandomCountryRelations()
        {
            var country = Countries[_random.Next(Countries.Count)];
            int change = _random.Next(-10, 11); // Изменение от -10 до +10
            country.RelationsLevel = Math.Max(0, Math.Min(100, country.RelationsLevel + change));

            string direction = change > 0 ? "улучшились" : "ухудшились";
            StatusMessage = $"Отношения с {country.Name} {direction} ({country.RelationsLevel})";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

