using CitySimulation.Infrastructure;
using CitySimulation.Models.ForeignRelations;
using CitySimulation.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace CitySimulation.ViewModels.ForeignRelations
{
    public class ForeignRelationsViewModel : ViewModelBase
    {
        private ObservableCollection<Country> _countries;
        private ObservableCollection<Immigrant> _immigrants;
        private ObservableCollection<TradeDeal> _tradeDeals;
        private Country _selectedCountry;
        private Immigrant _selectedImmigrant;
        private string _statusMessage;

        // Свойства для форм ввода
        private string _newCountryName;
        private int _relationChange;
        private string _newImmigrantName;
        private Country _newImmigrantCountry;
        private string _newImmigrantReason;
        private string _newTradeResource;
        private int _newTradeQuantity;
        private decimal _newTradePrice;

        public ForeignRelationsViewModel()
        {
            Countries = new ObservableCollection<Country>();
            Immigrants = new ObservableCollection<Immigrant>();
            TradeDeals = new ObservableCollection<TradeDeal>();

            InitializeTestData();

            // Команды для стран
            ChangeRelationCommand = new RelayCommand(ExecuteChangeRelation, CanChangeRelation);
            AddCountryCommand = new RelayCommand(ExecuteAddCountry, CanAddCountry);
            DeleteCountryCommand = new RelayCommand(ExecuteDeleteCountry, CanDeleteCountry);

            // Команды для иммигрантов
            AddImmigrantCommand = new RelayCommand(ExecuteAddImmigrant, CanAddImmigrant);
            DeleteImmigrantCommand = new RelayCommand(ExecuteDeleteImmigrant, CanDeleteImmigrant);

            // Команды для торговых сделок
            AddTradeDealCommand = new RelayCommand(ExecuteAddTradeDeal, CanAddTradeDeal);
        }

        private void InitializeTestData()
        {
            var country1 = new Country { Name = "Германия", RelationsLevel = 50 };
            var country2 = new Country { Name = "США", RelationsLevel = -20 };
            var country3 = new Country { Name = "Китай", RelationsLevel = 75 };

            Countries.Add(country1);
            Countries.Add(country2);
            Countries.Add(country3);

            // Устанавливаем значения по умолчанию
            NewImmigrantCountry = country1;
            NewImmigrantReason = "Работа";
            NewTradeResource = "Нефть";
            NewTradeQuantity = 100; // Значение по умолчанию
            NewTradePrice = 10.0m;  // Значение по умолчанию
        }

        // Основные коллекции
        public ObservableCollection<Country> Countries { get => _countries; set => SetProperty(ref _countries, value); }
        public ObservableCollection<Immigrant> Immigrants { get => _immigrants; set => SetProperty(ref _immigrants, value); }
        public ObservableCollection<TradeDeal> TradeDeals { get => _tradeDeals; set => SetProperty(ref _tradeDeals, value); }

        // Выбранные элементы
        public Country SelectedCountry { get => _selectedCountry; set => SetProperty(ref _selectedCountry, value); }
        public Immigrant SelectedImmigrant { get => _selectedImmigrant; set => SetProperty(ref _selectedImmigrant, value); }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        // Свойства для формы стран
        public string NewCountryName { get => _newCountryName; set => SetProperty(ref _newCountryName, value); }
        public int RelationChange { get => _relationChange; set => SetProperty(ref _relationChange, value); }

        // Свойства для формы иммигрантов
        public string NewImmigrantName { get => _newImmigrantName; set => SetProperty(ref _newImmigrantName, value); }
        public Country NewImmigrantCountry { get => _newImmigrantCountry; set => SetProperty(ref _newImmigrantCountry, value); }
        public string NewImmigrantReason { get => _newImmigrantReason; set => SetProperty(ref _newImmigrantReason, value); }

        // Свойства для формы торговых сделок
        public string NewTradeResource { get => _newTradeResource; set => SetProperty(ref _newTradeResource, value); }
        public int NewTradeQuantity { get => _newTradeQuantity; set => SetProperty(ref _newTradeQuantity, value); }
        public decimal NewTradePrice { get => _newTradePrice; set => SetProperty(ref _newTradePrice, value); }

        // Команды
        public ICommand ChangeRelationCommand { get; }
        public ICommand AddCountryCommand { get; }
        public ICommand DeleteCountryCommand { get; }
        public ICommand AddImmigrantCommand { get; }
        public ICommand DeleteImmigrantCommand { get; }
        public ICommand AddTradeDealCommand { get; }

        // Методы для стран
        private void ExecuteChangeRelation(object parameter)
        {
            if (SelectedCountry != null)
            {
                SelectedCountry.RelationsLevel += RelationChange;
                StatusMessage = $"Отношения с {SelectedCountry.Name} изменены на {RelationChange}. Новый уровень: {SelectedCountry.RelationsLevel}";
                RelationChange = 0;
            }
            else
            {
                StatusMessage = "Выберите страну для изменения отношений";
            }
        }

        private bool CanChangeRelation(object parameter) => SelectedCountry != null;

        private void ExecuteAddCountry(object parameter)
        {
            if (!string.IsNullOrWhiteSpace(NewCountryName))
            {
                var newCountry = new Country { Name = NewCountryName.Trim(), RelationsLevel = 0 };
                Countries.Add(newCountry);
                StatusMessage = $"Добавлена новая страна: {newCountry.Name}";
                NewCountryName = "";
            }
            else
            {
                StatusMessage = "Введите название страны";
            }
        }

        private bool CanAddCountry(object parameter) => !string.IsNullOrWhiteSpace(NewCountryName);

        private void ExecuteDeleteCountry(object parameter)
        {
            if (SelectedCountry != null)
            {
                var countryName = SelectedCountry.Name;
                Countries.Remove(SelectedCountry);
                StatusMessage = $"Страна {countryName} удалена";
            }
            else
            {
                StatusMessage = "Выберите страну для удаления";
            }
        }

        private bool CanDeleteCountry(object parameter) => SelectedCountry != null;

        // Методы для иммигрантов
        private void ExecuteAddImmigrant(object parameter)
        {
            if (!string.IsNullOrWhiteSpace(NewImmigrantName) && NewImmigrantCountry != null)
            {
                var newImmigrant = new Immigrant
                {
                    Name = NewImmigrantName.Trim(),
                    CountryOfOrigin = NewImmigrantCountry,
                    Reason = ConvertStringToImmigrationReason(NewImmigrantReason)
                };
                Immigrants.Add(newImmigrant);
                StatusMessage = $"Добавлен новый иммигрант: {newImmigrant.Name}";
                NewImmigrantName = "";
            }
            else
            {
                StatusMessage = "Заполните все поля для добавления иммигранта";
            }
        }

        private bool CanAddImmigrant(object parameter) => !string.IsNullOrWhiteSpace(NewImmigrantName) && NewImmigrantCountry != null;

        private void ExecuteDeleteImmigrant(object parameter)
        {
            if (SelectedImmigrant != null)
            {
                var immigrantName = SelectedImmigrant.Name;
                Immigrants.Remove(SelectedImmigrant);
                StatusMessage = $"Иммигрант {immigrantName} удален";
            }
            else
            {
                StatusMessage = "Выберите иммигранта для удаления";
            }
        }

        private bool CanDeleteImmigrant(object parameter) => SelectedImmigrant != null;

        // Методы для торговых сделок
        private void ExecuteAddTradeDeal(object parameter)
        {
            if (NewTradeQuantity > 0 && NewTradePrice > 0)
            {
                var newDeal = new TradeDeal
                {
                    Resource = ConvertStringToResourceType(NewTradeResource),
                    Quantity = NewTradeQuantity,
                    PricePerUnit = NewTradePrice
                };
                TradeDeals.Add(newDeal);
                StatusMessage = $"Добавлена новая сделка: {newDeal.Resource} x {newDeal.Quantity} по цене {newDeal.PricePerUnit}";
            }
            else
            {
                StatusMessage = "Количество и цена должны быть больше 0";
            }
        }

        private bool CanAddTradeDeal(object parameter) => NewTradeQuantity > 0 && NewTradePrice > 0;

        // Вспомогательные методы для конвертации
        private Enums.ImmigrationReason ConvertStringToImmigrationReason(string reason)
        {
            return reason switch
            {
                "Работа" => Enums.ImmigrationReason.Work,
                "Учеба" => Enums.ImmigrationReason.Study,
                "Беженец" => Enums.ImmigrationReason.Refugee,
                "Семья" => Enums.ImmigrationReason.Family,
                "Бизнес" => Enums.ImmigrationReason.Business,
                _ => Enums.ImmigrationReason.Work
            };
        }

        private Enums.ResourceType ConvertStringToResourceType(string resource)
        {
            return resource switch
            {
                "Нефть" => Enums.ResourceType.Oil,
                "Газ" => Enums.ResourceType.Gas,
                "Железо" => Enums.ResourceType.Iron,
                "Медь" => Enums.ResourceType.Copper,
                "Дерево" => Enums.ResourceType.Wood,
                "Электроэнергия" => Enums.ResourceType.Electricity,
                "Промтовары" => Enums.ResourceType.ConsumerGoods,
                _ => Enums.ResourceType.Oil
            };
        }
    }
}