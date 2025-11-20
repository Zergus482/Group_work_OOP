using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using GigacityContracts;

namespace EnergyModel
{
    public partial class MainWindow : Window, ICityModule
    {
        private ObservableCollection<ICell>? _cityCells;
        private DispatcherTimer? _timer;
        public ObservableCollection<EnergyCellViewModel> Cells { get; } = new();

        public string Name => "Energy Model";

        public MainWindow()
        {
            InitializeComponent();
            CellsList.ItemsSource = Cells;
        }

        public void Initialize(ObservableCollection<ICell> cityCells)
        {
            _cityCells = cityCells;
            Cells.Clear();
            foreach (var c in cityCells)
            {
                Cells.Add(new EnergyCellViewModel { X = c.X, Y = c.Y, PowerSupply = 0.0, Consumption = 0.0 });
            }
            StatusText.Text = $"Initialized {Cells.Count} cells";
        }

        public void Show()
        {
            this.ShowDialog();
        }

        private void StartSimulation_Click(object sender, RoutedEventArgs e)
        {
            if (_timer != null) return;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            var rnd = new Random();
            _timer.Tick += (s, a) =>
            {
                foreach (var cell in Cells)
                {
                    cell.PowerSupply = Math.Max(0, cell.PowerSupply + rnd.NextDouble()*2 - 0.5);
                    cell.Consumption = Math.Max(0, cell.Consumption + rnd.NextDouble()*1.5 - 0.4);
                }
                StatusText.Text = $"Running — tick {DateTime.Now:T}";
            };
            _timer.Start();
        }

        private void StopSimulation_Click(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            _timer = null;
            StatusText.Text = "Stopped";
        }
    }

    public class EnergyCellViewModel
    {
        public int X { get; set; }
        public int Y { get; set; }
        public double PowerSupply { get; set; }
        public double Consumption { get; set; }
    }
}
