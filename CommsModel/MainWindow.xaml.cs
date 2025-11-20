using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using GigacityContracts;

namespace CommsModel
{
    public partial class MainWindow : Window, ICityModule
    {
        private ObservableCollection<ICell>? _cityCells;
        private DispatcherTimer? _timer;
        private CommunicationsSimulationService? _simulationService;
        public ObservableCollection<CommNodeViewModel> Nodes { get; } = new();

        public new string Name => "Communications Model";
        
        public event EventHandler<CommDataUpdatedEventArgs>? CommDataUpdated;

        public MainWindow()
        {
            InitializeComponent();
            NodesList.ItemsSource = Nodes;
        }

        public void Initialize(ObservableCollection<ICell> cityCells)
        {
            _cityCells = cityCells;
            _simulationService = new CommunicationsSimulationService();
            _simulationService.Initialize(cityCells);
            
            Nodes.Clear();
            
            // Добавляем вышки связи
            foreach (var tower in _simulationService.GetTowers())
            {
                Nodes.Add(new CommNodeViewModel 
                { 
                    X = tower.X, 
                    Y = tower.Y, 
                    Coverage = 0, 
                    Latency = 0, 
                    LoadPercent = 0,
                    TowerType = tower.Type.ToString(),
                    Range = tower.Range
                });
            }

            StatusText.Text = $"Инициализировано {Nodes.Count} вышек";
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
            _timer.Tick += (s, a) => SimulateOnce();
            _timer.Start();
            StatusText.Text = "Автоматическая симуляция запущена";
        }

        private void Simulate_Click(object sender, RoutedEventArgs e)
        {
            SimulateOnce();
            StatusText.Text = $"Симуляция выполнена в {DateTime.Now:T}";
        }

        private void AutoRun_Click(object sender, RoutedEventArgs e)
        {
            if (_timer != null) return;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, a) => SimulateOnce();
            _timer.Start();
            StatusText.Text = "Автоматический режим";
        }

        private void StopAuto_Click(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            _timer = null;
            StatusText.Text = "Остановлено";
        }

        private void SimulateOnce()
        {
            if (_simulationService == null) return;
            
            _simulationService.SimulateStep();
            UpdateNodesFromService();
            
            AvgCellularCoverage.Text = $"Средняя сотовая связь: {_simulationService.GetAverageCellularCoverage():F1}%";
            AvgInternetCoverage.Text = $"Средний интернет: {_simulationService.GetAverageInternetCoverage():F1}%";
            AvgLatency.Text = $"Средняя задержка: {_simulationService.GetAllCells().Average(c => c.Latency):F0} мс";
            NoCoverageText.Text = $"Без покрытия: {_simulationService.GetCellsWithoutCoverage()} ячеек";
            OverloadedText.Text = $"Перегруженных вышек: {_simulationService.GetOverloadedTowers()}";
            
            // Уведомляем главное окно об обновлении данных
            CommDataUpdated?.Invoke(this, new CommDataUpdatedEventArgs(_simulationService));
        }
        
        private void UpdateNodesFromService()
        {
            if (_simulationService == null) return;
            
            var towers = _simulationService.GetTowers().ToList();
            foreach (var node in Nodes)
            {
                var tower = towers.FirstOrDefault(t => t.X == node.X && t.Y == node.Y);
                if (tower != null)
                {
                    node.LoadPercent = Math.Round((double)tower.CurrentLoad / tower.Capacity * 100, 1);
                }
            }
        }
        
        public CommunicationsSimulationService? GetSimulationService()
        {
            return _simulationService;
        }
    }

    public class CommNodeViewModel : INotifyPropertyChanged
    {
        public int X { get; set; }
        public int Y { get; set; }
        private double _coverage;
        private int _latency;
        private double _loadPercent;
        private string _towerType = "";
        private double _range;

        public double Coverage { get => _coverage; set { _coverage = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Coverage))); } }
        public int Latency { get => _latency; set { _latency = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Latency))); } }
        public double LoadPercent { get => _loadPercent; set { _loadPercent = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LoadPercent))); } }
        public string TowerType 
        { 
            get => _towerType == "Cellular" ? "Мобильная вышка" : _towerType == "Internet" ? "Интернет-провайдер" : _towerType; 
            set { _towerType = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TowerType))); } 
        }
        public double Range { get => _range; set { _range = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Range))); } }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }
    
    public class CommDataUpdatedEventArgs : EventArgs
    {
        public CommunicationsSimulationService Service { get; }
        
        public CommDataUpdatedEventArgs(CommunicationsSimulationService service)
        {
            Service = service;
        }
    }
}
