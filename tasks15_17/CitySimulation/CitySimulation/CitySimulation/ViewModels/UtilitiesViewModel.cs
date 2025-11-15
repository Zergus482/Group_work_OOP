using CitySimulation.Models.Utilities;
using System.Collections.ObjectModel;
using System.Linq;

namespace CitySimulation.ViewModels
{
    public class UtilitiesViewModel : BaseViewModel
    {
        private ResidentialBuilding _selectedBuilding;
        private string _statusMessage;

        public ObservableCollection<ResidentialBuilding> Buildings { get; }
        public UtilityService CityService { get; }

        public RelayCommand ConnectUtilitiesCommand { get; }
        public RelayCommand PerformMaintenanceCommand { get; }
        public RelayCommand AddBudgetCommand { get; }
        public RelayCommand UpdateResidentsCommand { get; }

        public UtilitiesViewModel()
        {
            Buildings = new ObservableCollection<ResidentialBuilding>();
            CityService = new UtilityService("Городская служба ЖКХ");

            ConnectUtilitiesCommand = new RelayCommand(ConnectUtilities);
            PerformMaintenanceCommand = new RelayCommand(PerformMaintenance);
            AddBudgetCommand = new RelayCommand(AddBudget);
            UpdateResidentsCommand = new RelayCommand(UpdateResidents);

            InitializeData();
        }

        private void InitializeData()
        {
            // Создаем жилые здания
            var buildings = new[]
            {
                new ResidentialBuilding("ул. Центральная, 1", 50),
                new ResidentialBuilding("ул. Центральная, 2", 75),
                new ResidentialBuilding("ул. Лесная, 15", 30),
                new ResidentialBuilding("ул. Школьная, 8", 100),
                new ResidentialBuilding("пр. Победы, 25", 60)
            };

            foreach (var building in buildings)
            {
                Buildings.Add(building);
            }

            SelectedBuilding = Buildings.FirstOrDefault();
            UpdateUtilitiesProduction();
        }

        public ResidentialBuilding SelectedBuilding
        {
            get => _selectedBuilding;
            set
            {
                SetProperty(ref _selectedBuilding, value);
                UpdateStatusMessage();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private void ConnectUtilities()
        {
            if (SelectedBuilding != null)
            {
                if (CityService.ConnectBuildingToUtilities(SelectedBuilding))
                {
                    StatusMessage = $"Коммунальные услуги подключены к {SelectedBuilding.Address}";
                }
                else
                {
                    StatusMessage = $"Не удалось подключить все коммунальные услуги к {SelectedBuilding.Address}";
                }
                OnPropertyChanged(nameof(SelectedBuilding));
                OnPropertyChanged(nameof(CityService));
            }
        }

        private void PerformMaintenance()
        {
            CityService.PerformMaintenance();
            StatusMessage = "Техническое обслуживание выполнено. " +
                          $"Бюджет: ${CityService.Budget:F2}";
            OnPropertyChanged(nameof(CityService));
        }

        private void AddBudget()
        {
            CityService.AddBudget(50000);
            StatusMessage = $"Бюджет увеличен. Текущий бюджет: ${CityService.Budget:F2}";
            OnPropertyChanged(nameof(CityService));
        }

        private void UpdateResidents()
        {
            if (SelectedBuilding != null)
            {
                SelectedBuilding.UpdateResidents(SelectedBuilding.Residents + 10);
                StatusMessage = $"Количество жителей в {SelectedBuilding.Address} увеличено до {SelectedBuilding.Residents}";
                OnPropertyChanged(nameof(SelectedBuilding));
                OnPropertyChanged(nameof(CityService));
            }
        }

        private void UpdateUtilitiesProduction()
        {
            // Устанавливаем производство для каждой сети
            CityService.Networks[UtilityType.Electricity].UpdateProduction(8000);
            CityService.Networks[UtilityType.Water].UpdateProduction(4500);
            CityService.Networks[UtilityType.Gas].UpdateProduction(2800);
            CityService.Networks[UtilityType.Sewage].UpdateProduction(1800);
        }

        private void UpdateStatusMessage()
        {
            if (SelectedBuilding != null)
            {
                var utilityStatus = SelectedBuilding.HasUtilities ? "Подключены" : "Не подключены";
                StatusMessage = $"{SelectedBuilding.Address}: {SelectedBuilding.Residents} жителей, " +
                              $"Услуги: {utilityStatus}";
            }
        }
    }
}
