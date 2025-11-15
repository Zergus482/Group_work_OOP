using CitySimulation.Models.Resources;
using System.Collections.ObjectModel;
using System.Linq;

namespace CitySimulation.ViewModels
{
    public class ResourcesViewModel : BaseViewModel
    {
        private ExtractionFacility _selectedFacility;
        private string _statusMessage;

        public ObservableCollection<ExtractionFacility> Facilities { get; }
        public ObservableCollection<ResourceDeposit> Deposits { get; }
        public ResourceStorage Storage { get; }
        public ResourceMarket Market { get; }

        public RelayCommand StartExtractionCommand { get; }
        public RelayCommand StopExtractionCommand { get; }
        public RelayCommand HireWorkerCommand { get; }
        public RelayCommand FireWorkerCommand { get; }
        public RelayCommand CollectResourcesCommand { get; }

        public ResourcesViewModel()
        {
            Facilities = new ObservableCollection<ExtractionFacility>();
            Deposits = new ObservableCollection<ResourceDeposit>();
            Storage = new ResourceStorage();
            Market = new ResourceMarket();

            StartExtractionCommand = new RelayCommand(StartExtraction);
            StopExtractionCommand = new RelayCommand(StopExtraction);
            HireWorkerCommand = new RelayCommand(HireWorker);
            FireWorkerCommand = new RelayCommand(FireWorker);
            CollectResourcesCommand = new RelayCommand(CollectResources);

            InitializeData();
        }

        private void InitializeData()
        {
            // Создаем месторождения
            var deposits = new[]
            {
                new ResourceDeposit(ResourceType.Oil, "Северное нефтяное месторождение", 500000, 250),
                new ResourceDeposit(ResourceType.Gas, "Восточное газовое месторождение", 300000, 180),
                new ResourceDeposit(ResourceType.Coal, "Западный угольный разрез", 800000, 400),
                new ResourceDeposit(ResourceType.Iron, "Центральное железорудное месторождение", 200000, 120),
                new ResourceDeposit(ResourceType.Copper, "Южный медный рудник", 150000, 80)
            };

            foreach (var deposit in deposits)
            {
                Deposits.Add(deposit);
            }

            // Создаем объекты добычи
            var facilities = new[]
            {
                new ExtractionFacility("Нефтедобывающий комплекс №1", deposits[0]),
                new ExtractionFacility("Газодобывающая станция №1", deposits[1]),
                new ExtractionFacility("Угольный карьер №1", deposits[2])
            };

            foreach (var facility in facilities)
            {
                Facilities.Add(facility);
            }

            SelectedFacility = Facilities.FirstOrDefault();
        }

        public ExtractionFacility SelectedFacility
        {
            get => _selectedFacility;
            set
            {
                SetProperty(ref _selectedFacility, value);
                UpdateStatusMessage();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private void StartExtraction()
        {
            if (SelectedFacility != null)
            {
                SelectedFacility.StartExtraction();
                StatusMessage = $"Добыча на {SelectedFacility.Name} начата";
                OnPropertyChanged(nameof(SelectedFacility));
            }
        }

        private void StopExtraction()
        {
            if (SelectedFacility != null)
            {
                SelectedFacility.StopExtraction();
                StatusMessage = $"Добыча на {SelectedFacility.Name} остановлена";
                OnPropertyChanged(nameof(SelectedFacility));
            }
        }

        private void HireWorker()
        {
            if (SelectedFacility != null)
            {
                SelectedFacility.HireWorker();
                StatusMessage = $"Новый работник нанят на {SelectedFacility.Name}";
                OnPropertyChanged(nameof(SelectedFacility));
            }
        }

        private void FireWorker()
        {
            if (SelectedFacility != null)
            {
                SelectedFacility.FireWorker();
                StatusMessage = $"Работник уволен с {SelectedFacility.Name}";
                OnPropertyChanged(nameof(SelectedFacility));
            }
        }

        private void CollectResources()
        {
            if (SelectedFacility != null && SelectedFacility.Deposit.IsActive)
            {
                var extracted = SelectedFacility.DailyProduction;
                if (Storage.AddResource(SelectedFacility.Deposit.Type, extracted))
                {
                    var revenue = Market.CalculateRevenue(SelectedFacility.Deposit.Type, extracted);
                    StatusMessage = $"Добыто {extracted:F2} ед. {SelectedFacility.Deposit.Type}. Доход: ${revenue:F2}";
                }
                else
                {
                    StatusMessage = "Хранилище переполнено!";
                }
                OnPropertyChanged(nameof(Storage));
            }
        }

        private void UpdateStatusMessage()
        {
            if (SelectedFacility != null)
            {
                StatusMessage = $"{SelectedFacility.Name}: {SelectedFacility.Deposit.Workers} работников, " +
                              $"Добыча: {SelectedFacility.DailyProduction:F2}/день";
            }
        }
    }
}
