using System.Windows;
using System.Windows.Controls;
using CitySimulation.Models.Utilities;
using CitySimulation.ViewModels;
using System.Linq;

namespace CitySimulation.Views
{
    public partial class UtilitiesView : UserControl
    {
        private UtilitiesViewModel _viewModel;
        private ResidentialBuilding _selectedBuilding;

        public UtilitiesView()
        {
            InitializeComponent();
            _viewModel = new UtilitiesViewModel();
            LoadData();
        }

        private void LoadData()
        {
            BuildingsList.ItemsSource = _viewModel.Buildings;
            UpdateServiceInfo();
        }

        private void BuildingsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedBuilding = BuildingsList.SelectedItem as ResidentialBuilding;
            if (_selectedBuilding != null)
            {
                SelectedBuildingAddress.Text = _selectedBuilding.Address;
                BuildingAddress.Text = _selectedBuilding.Address;
                UpdateBuildingInfo();
                UpdateStatusMessage();
            }
        }

        private void ConnectUtilities_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBuilding != null)
            {
                if (_viewModel.CityService.ConnectBuildingToUtilities(_selectedBuilding))
                {
                    StatusText.Text = $"Коммунальные услуги подключены к {_selectedBuilding.Address}";
                }
                else
                {
                    StatusText.Text = $"Не удалось подключить все коммунальные услуги к {_selectedBuilding.Address}";
                }
                RefreshData();
                UpdateServiceInfo();
            }
        }

        private void PerformMaintenance_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CityService.PerformMaintenance();
            StatusText.Text = $"Техническое обслуживание выполнено. Бюджет: ${_viewModel.CityService.Budget:F2}";
            UpdateServiceInfo();
        }

        private void AddBudget_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CityService.AddBudget(50000);
            StatusText.Text = $"Бюджет увеличен. Текущий бюджет: ${_viewModel.CityService.Budget:F2}";
            UpdateServiceInfo();
        }

        private void UpdateResidents_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBuilding != null)
            {
                _selectedBuilding.UpdateResidents(_selectedBuilding.Residents + 10);
                StatusText.Text = $"Количество жителей в {_selectedBuilding.Address} увеличено до {_selectedBuilding.Residents}";
                UpdateBuildingInfo();
                RefreshData();
                UpdateServiceInfo();
            }
        }

        private void AddNewBuilding_Click(object sender, RoutedEventArgs e)
        {
            string address = NewBuildingAddress.Text.Trim();
            if (string.IsNullOrEmpty(address))
            {
                StatusText.Text = "Введите адрес здания!";
                return;
            }

            if (!int.TryParse(NewBuildingResidents.Text, out int residents) || residents <= 0)
            {
                StatusText.Text = "Введите корректное количество жителей!";
                return;
            }

            // Создаем новое здание
            var newBuilding = new ResidentialBuilding(address, residents);

            // Добавляем в коллекцию
            _viewModel.Buildings.Add(newBuilding);

            StatusText.Text = $"Добавлено новое здание: {address}";
            RefreshData();

            // Очищаем поля ввода
            NewBuildingAddress.Text = "ул. Новая, 1";
            NewBuildingResidents.Text = "50";
        }

        private void DeleteBuilding_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBuilding != null)
            {
                // Сохраняем адрес для сообщения
                string buildingAddress = _selectedBuilding.Address;

                // Отключаем здание от коммунальных услуг перед удалением
                _viewModel.CityService.DisconnectBuildingFromUtilities(_selectedBuilding);

                // Удаляем здание
                _viewModel.Buildings.Remove(_selectedBuilding);

                // Сбрасываем выбор
                _selectedBuilding = null;
                SelectedBuildingAddress.Text = "Выберите здание";
                BuildingAddress.Text = "Выберите здание";

                StatusText.Text = $"Здание '{buildingAddress}' удалено";
                RefreshData();
                UpdateServiceInfo();
            }
            else
            {
                StatusText.Text = "Выберите здание для удаления!";
            }
        }

        private void UpdateStatusMessage()
        {
            if (_selectedBuilding != null)
            {
                var utilityStatus = _selectedBuilding.HasUtilities ? "Подключены" : "Не подключены";
                StatusText.Text = $"{_selectedBuilding.Address}: {_selectedBuilding.Residents} жителей, Услуги: {utilityStatus}";
            }
        }

        private void UpdateBuildingInfo()
        {
            if (_selectedBuilding != null)
            {
                ElectricityText.Text = $"{_selectedBuilding.ElectricityConsumption:F2} кВт";
                WaterText.Text = $"{_selectedBuilding.WaterConsumption:F2} м³";
                GasText.Text = $"{_selectedBuilding.GasConsumption:F2} м³";
                SewageText.Text = $"{_selectedBuilding.SewageProduction:F2} м³";
            }
        }

        private void UpdateServiceInfo()
        {
            BudgetText.Text = $"Бюджет: ${_viewModel.CityService.Budget:F2}";
            NetworkStatusText.Text = _viewModel.CityService.GetNetworkStatus();
        }

        private void RefreshData()
        {
            BuildingsList.Items.Refresh();
        }
    }
}