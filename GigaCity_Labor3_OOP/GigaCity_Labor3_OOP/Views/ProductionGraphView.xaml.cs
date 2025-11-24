using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using GigaCity_Labor3_OOP.Models.Economy;
using GigaCity_Labor3_OOP.ViewModels.Economy;

namespace GigaCity_Labor3_OOP.Views
{
    public partial class ProductionGraphView : UserControl
    {
        private readonly Dictionary<ProductType, Polyline> _graphLines = new();
        private readonly Dictionary<ProductType, Brush> _lineColors = new()
        {
            { ProductType.Steel, Brushes.SteelBlue },
            { ProductType.EngineeredWood, Brushes.SaddleBrown },
            { ProductType.Fuel, Brushes.Orange },
            { ProductType.Chemicals, Brushes.Purple },
            { ProductType.Conductors, Brushes.Gold },
            { ProductType.Coolant, Brushes.LightBlue }
        };

        private EconomySimulationViewModel? _economySimulation;
        private DispatcherTimer? _updateTimer;
        private readonly List<ProductionDataPoint> _productionHistory = new();
        private const int MaxHistoryPoints = 100;

        public ProductionGraphView()
        {
            InitializeComponent();
            Loaded += ProductionGraphView_Loaded;
            Unloaded += ProductionGraphView_Unloaded;
        }

        private void ProductionGraphView_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeGraphLines();
            UpdateGraph();
        }

        private void ProductionGraphView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer = null;
            }
        }

        public void SetEconomySimulation(EconomySimulationViewModel economySimulation)
        {
            _economySimulation = economySimulation;

            // Создаем таймер для обновления данных
            if (_updateTimer == null)
            {
                _updateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                _updateTimer.Tick += UpdateProductionData;
                _updateTimer.Start();
            }
        }

        private void InitializeGraphLines()
        {
            foreach (var productType in Enum.GetValues<ProductType>())
            {
                if (_lineColors.ContainsKey(productType))
                {
                    var line = new Polyline
                    {
                        Stroke = _lineColors[productType],
                        StrokeThickness = 2,
                        Points = new PointCollection()
                    };
                    _graphLines[productType] = line;
                    ProductionCanvas.Children.Add(line);
                }
            }
        }

        private void ProductionCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateGraph();
        }

        private void UpdateProductionData(object? sender, EventArgs e)
        {
            if (_economySimulation == null) return;

            var allProducts = _economySimulation.GetAllProducedProducts();
            var dataPoint = new ProductionDataPoint
            {
                Time = DateTime.Now,
                Production = new Dictionary<ProductType, double>(allProducts)
            };

            _productionHistory.Add(dataPoint);

            // Ограничиваем размер истории
            if (_productionHistory.Count > MaxHistoryPoints)
            {
                _productionHistory.RemoveAt(0);
            }

            UpdateGraph();
        }

        private void UpdateGraph()
        {
            if (_productionHistory == null || _productionHistory.Count == 0)
            {
                // Очищаем графики если нет данных
                foreach (var line in _graphLines.Values)
                {
                    line.Points.Clear();
                }
                return;
            }

            var canvasWidth = ProductionCanvas.ActualWidth > 0 ? ProductionCanvas.ActualWidth : 600;
            var canvasHeight = ProductionCanvas.ActualHeight > 0 ? ProductionCanvas.ActualHeight : 300;
            var padding = 40;
            var graphWidth = Math.Max(0, canvasWidth - 2 * padding);
            var graphHeight = Math.Max(0, canvasHeight - 2 * padding);

            // Обновляем оси
            if (ProductionCanvas.Children.OfType<Line>().FirstOrDefault(l => l.Name == "YAxis") is Line yAxis)
            {
                yAxis.Y2 = canvasHeight - padding;
            }
            if (ProductionCanvas.Children.OfType<Line>().FirstOrDefault(l => l.Name == "XAxis") is Line xAxis)
            {
                xAxis.X2 = canvasWidth - padding;
                xAxis.Y1 = canvasHeight - padding;
                xAxis.Y2 = canvasHeight - padding;
            }
            
            // Обновляем подпись оси X
            if (ProductionCanvas.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Name == "XAxisLabel") is TextBlock xLabel)
            {
                Canvas.SetLeft(xLabel, (canvasWidth - padding) / 2 - 20);
                Canvas.SetTop(xLabel, canvasHeight - padding + 10);
            }

            // Находим максимальное значение для нормализации
            double maxValue = 1;
            foreach (var dataPoint in _productionHistory)
            {
                foreach (var production in dataPoint.Production.Values)
                {
                    if (production > maxValue) maxValue = production;
                }
            }
            if (maxValue <= 0) maxValue = 1;

            // Обновляем линии для каждого типа продукта
            foreach (var kvp in _graphLines)
            {
                var productType = kvp.Key;
                var line = kvp.Value;
                var points = new PointCollection();

                if (_productionHistory.Count > 0)
                {
                    for (int i = 0; i < _productionHistory.Count; i++)
                    {
                        var dataPoint = _productionHistory[i];
                        var value = dataPoint.Production.TryGetValue(productType, out var val) ? val : 0;
                        var normalizedValue = value / maxValue;

                        var x = padding + (_productionHistory.Count > 1
                            ? (i / (double)(_productionHistory.Count - 1)) * graphWidth
                            : 0);
                        var y = canvasHeight - padding - (normalizedValue * graphHeight);

                        points.Add(new Point(x, y));
                    }
                }

                line.Points = points;
            }
        }

        private class ProductionDataPoint
        {
            public DateTime Time { get; set; }
            public Dictionary<ProductType, double> Production { get; set; } = new();
        }
    }
}

