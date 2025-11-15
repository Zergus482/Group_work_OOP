using System.Windows;
using System.Windows.Controls;
using CitySimulation.Models.Resources;
using CitySimulation.ViewModels;
using System.Linq;
using System.Collections.ObjectModel;

namespace CitySimulation.Views
{
    public partial class ResourcesView : UserControl
    {
        private ResourcesViewModel _viewModel;
        private ExtractionFacility _selectedFacility;

        public ResourcesView()
        {
            InitializeComponent();
            _viewModel = new ResourcesViewModel();
            LoadData();
            InitializeComboBox();
        }

        private void LoadData()
        {
            FacilitiesList.ItemsSource = _viewModel.Facilities;
            DepositsList.ItemsSource = _viewModel.Deposits;
            StorageList.ItemsSource = _viewModel.Storage.Storage;

            StorageCapacity.Text = $"Вместимость: {_viewModel.Storage.Capacity}";
            UpdateStorageUsed();
        }

        private void InitializeComboBox()
        {
            ResourceTypeComboBox.Items.Clear();
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                ResourceTypeComboBox.Items.Add(type.ToRussianString());
            }
            ResourceTypeComboBox.SelectedIndex = 0;
        }

        private void FacilitiesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedFacility = FacilitiesList.SelectedItem as ExtractionFacility;
            if (_selectedFacility != null)
            {
                SelectedFacilityName.Text = _selectedFacility.Name;
                UpdateStatusMessage();
            }
        }

        private void StartExtraction_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFacility != null)
            {
                _selectedFacility.StartExtraction();
                UpdateStatusMessage();
                RefreshData();
            }
        }

        private void StopExtraction_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFacility != null)
            {
                _selectedFacility.StopExtraction();
                UpdateStatusMessage();
                RefreshData();
            }
        }

        private void HireWorker_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFacility != null)
            {
                _selectedFacility.HireWorker();
                UpdateStatusMessage();
                RefreshData();
            }
        }

        private void FireWorker_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFacility != null)
            {
                _selectedFacility.FireWorker();
                UpdateStatusMessage();
                RefreshData();
            }
        }

        private void CollectResources_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFacility != null && _selectedFacility.Deposit.IsActive)
            {
                var extracted = _selectedFacility.DailyProduction;
                if (_viewModel.Storage.AddResource(_selectedFacility.Deposit.Type, extracted))
                {
                    var revenue = _viewModel.Market.CalculateRevenue(_selectedFacility.Deposit.Type, extracted);
                    StatusText.Text = $"Добыто {extracted:F2} ед. {_selectedFacility.Deposit.Type}. Доход: ${revenue:F2}";
                }
                else
                {
                    StatusText.Text = "Хранилище переполнено!";
                }
                UpdateStorageUsed();
                RefreshData();
            }
        }

        private void AddNewFacility_Click(object sender, RoutedEventArgs e)
        {
            string facilityName = NewFacilityName.Text.Trim();
            if (string.IsNullOrEmpty(facilityName))
            {
                StatusText.Text = "Введите название объекта!";
                return;
            }

            if (ResourceTypeComboBox.SelectedItem == null)
            {
                StatusText.Text = "Выберите тип ресурса!";
                return;
            }

            string selectedType = ResourceTypeComboBox.SelectedItem.ToString();
            ResourceType resourceType = GetResourceTypeFromRussian(selectedType);

            var newDeposit = new ResourceDeposit(
                resourceType,
                $"{facilityName} месторождение",
                100000,
                100
            );

            var newFacility = new ExtractionFacility(facilityName, newDeposit);

            _viewModel.Deposits.Add(newDeposit);
            _viewModel.Facilities.Add(newFacility);

            StatusText.Text = $"Добавлен новый объект: {facilityName}";
            RefreshData();

            NewFacilityName.Text = "Новый объект";
        }

        private void DeleteFacility_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFacility != null)
            {
                // Сохраняем имя для сообщения
                string facilityName = _selectedFacility.Name;

                // Удаляем связанное месторождение
                var depositToRemove = _selectedFacility.Deposit;
                _viewModel.Deposits.Remove(depositToRemove);

                // Удаляем объект добычи
                _viewModel.Facilities.Remove(_selectedFacility);

                // Сбрасываем выбор
                _selectedFacility = null;
                SelectedFacilityName.Text = "Выберите объект";

                StatusText.Text = $"Объект '{facilityName}' удален";
                RefreshData();
            }
            else
            {
                StatusText.Text = "Выберите объект для удаления!";
            }
        }

        private ResourceType GetResourceTypeFromRussian(string russianName)
        {
            switch (russianName)
            {
                case "Нефть": return ResourceType.Oil;
                case "Газ": return ResourceType.Gas;
                case "Уголь": return ResourceType.Coal;
                case "Железо": return ResourceType.Iron;
                case "Медь": return ResourceType.Copper;
                case "Золото": return ResourceType.Gold;
                case "Вода": return ResourceType.Water;
                default: return ResourceType.Oil;
            }
        }

        private void UpdateStatusMessage()
        {
            if (_selectedFacility != null)
            {
                StatusText.Text = $"{_selectedFacility.Name}: {_selectedFacility.Deposit.Workers} работников, " +
                                $"Добыча: {_selectedFacility.DailyProduction:F2}/день";
            }
        }

        private void UpdateStorageUsed()
        {
            StorageUsed.Text = $"Занято: {_viewModel.Storage.GetTotalStored():F2}";
        }

        private void RefreshData()
        {
            FacilitiesList.Items.Refresh();
            DepositsList.Items.Refresh();
            StorageList.Items.Refresh();
        }
    }
}