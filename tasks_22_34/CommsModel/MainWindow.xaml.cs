using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using GigacityContracts;

namespace CommsModel
{
    public partial class MainWindow : Window, ICityModule
    {
        private ObservableCollection<ICell>? _cityCells;
        public ObservableCollection<CommNodeViewModel> Nodes { get; } = new();

        public string Name => "Communications Model";

        public MainWindow()
        {
            InitializeComponent();
            NodesList.ItemsSource = Nodes;
        }

        public void Initialize(ObservableCollection<ICell> cityCells)
        {
            _cityCells = cityCells;
            Nodes.Clear();
            int step = Math.Max(1, (int)Math.Sqrt(Math.Max(1, cityCells.Count))/10);
            for (int i=0;i<cityCells.Count;i+=step)
            {
                var c = cityCells[i];
                Nodes.Add(new CommNodeViewModel { X = c.X, Y = c.Y, Coverage = 0.0, Latency = 0 });
            }
            StatusText.Text = $"Initialized {Nodes.Count} nodes";
        }

        public void Show()
        {
            this.ShowDialog();
        }

        private void Simulate_Click(object sender, RoutedEventArgs e)
        {
            var rnd = new Random();
            foreach (var n in Nodes)
            {
                n.Coverage = Math.Round(50 + rnd.NextDouble()*50,2); // percent
                n.Latency = rnd.Next(5,200);
            }
            StatusText.Text = $"Simulated at {DateTime.Now:T}";
        }
    }

    public class CommNodeViewModel
    {
        public int X { get; set; }
        public int Y { get; set; }
        public double Coverage { get; set; }
        public int Latency { get; set; }
    }
}
