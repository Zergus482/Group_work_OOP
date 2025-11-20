using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using GigacityContracts;

namespace EnergyModel
{
    public partial class MainWindow : Window, ICityModule
    {
        private ObservableCollection<ICell>? _cityCells;
        private DispatcherTimer? _timer;
        private EnergySimulationService? _simulationService;
        public ObservableCollection<EnergyCellViewModel> Cells { get; } = new();

        public new string Name => "Energy Model";
        
        public event EventHandler<EnergyDataUpdatedEventArgs>? EnergyDataUpdated;

        public MainWindow()
        {
            InitializeComponent();
            CellsList.ItemsSource = Cells;
        }

        public void Initialize(ObservableCollection<ICell> cityCells)
        {
            _cityCells = cityCells;
            _simulationService = new EnergySimulationService();
            _simulationService.Initialize(cityCells);
            
            Cells.Clear();
            foreach (var c in cityCells)
            {
                Cells.Add(new EnergyCellViewModel
                {
                    X = c.X,
                    Y = c.Y,
                    ResourceType = c.ResourceType,
                    TerrainType = c.TerrainType,
                    Generation = 0.0,
                    Consumption = 0.0,
                    Net = 0.0,
                    Status = "OK"
                });
            }
            StatusText.Text = $"Инициализировано {Cells.Count} ячеек";
        }

        public new void Show()
        {
            base.Show();
        }
        
        public void StartAutoSimulation()
        {
            if (_timer != null) return;
            if (_simulationService == null) return;
            
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, a) =>
            {
                _simulationService.SimulateStep();
                UpdateCellsFromService();
                
                TotalSupplyText.Text = $"Общая генерация: {_simulationService.GetTotalGeneration():F2} МВт";
                TotalConsumptionText.Text = $"Общее потребление: {_simulationService.GetTotalConsumption():F2} МВт";
                DeficitText.Text = $"Ячеек с дефицитом: {_simulationService.GetDeficitCells()}";
                SurplusText.Text = $"Ячеек с избытком: {_simulationService.GetSurplusCells()}";

                StatusText.Text = $"Работает — {DateTime.Now:T}";
                
                // Уведомляем главное окно об обновлении данных
                EnergyDataUpdated?.Invoke(this, new EnergyDataUpdatedEventArgs(_simulationService));
            };
            _timer.Start();
            StatusText.Text = "Автоматическая симуляция запущена";
        }

        
        private void UpdateCellsFromService()
        {
            if (_simulationService == null) return;
            
            foreach (var serviceData in _simulationService.GetAllCells())
            {
                var viewModel = Cells.FirstOrDefault(c => c.X == serviceData.X && c.Y == serviceData.Y);
                if (viewModel != null)
                {
                    viewModel.Generation = serviceData.Generation;
                    viewModel.Consumption = serviceData.Consumption;
                    viewModel.Net = serviceData.NetEnergy;
                    viewModel.Status = GetStatusName(serviceData.Status);
                }
            }
        }
        
        private string GetStatusName(EnergyStatus status)
        {
            return status switch
            {
                EnergyStatus.Critical => "Критический",
                EnergyStatus.Deficient => "Дефицит",
                EnergyStatus.Normal => "Норма",
                EnergyStatus.Surplus => "Избыток",
                _ => "Неизвестно"
            };
        }

        
        public EnergySimulationService? GetSimulationService()
        {
            return _simulationService;
        }
    }

    public class EnergyCellViewModel : INotifyPropertyChanged
    {
        public int X { get; set; }
        public int Y { get; set; }
        public byte ResourceType { get; set; }
        public byte TerrainType { get; set; }

        private double _generation;
        private double _consumption;
        private double _net;
        private string _status = "Idle";

        public double Generation { get => _generation; set { _generation = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Generation))); } }
        public double Consumption { get => _consumption; set { _consumption = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Consumption))); } }
        public double Net { get => _net; set { _net = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Net))); } }
        public string Status { get => _status; set { _status = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status))); } }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }
    
    public class EnergyDataUpdatedEventArgs : EventArgs
    {
        public EnergySimulationService Service { get; }
        
        public EnergyDataUpdatedEventArgs(EnergySimulationService service)
        {
            Service = service;
        }
    }
}

