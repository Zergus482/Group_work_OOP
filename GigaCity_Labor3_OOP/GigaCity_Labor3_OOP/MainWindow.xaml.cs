using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Threading;
using GigaCity_Labor3_OOP.Models;
using GigaCity_Labor3_OOP.Models.Economy;
using GigaCity_Labor3_OOP.Models.EmergencyService;
using GigaCity_Labor3_OOP.ViewModels;
using GigaCity_Labor3_OOP.ViewModels.Economy;
using BuildMenuEntry = GigaCity_Labor3_OOP.ViewModels.Economy.BuildMenuEntry;
using TheFinancialSystem;
using TheFinancialSystem.ViewModels;
using System.Linq;
using GigaCity_Labor3_OOP.Views;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace GigaCity_Labor3_OOP
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; private set; }

        private Point _lastMousePosition;
        private bool _isPanning = false;
        private EconomyFacilityViewModel? _draggedFacility;
        private Point _dragStartPoint;
        private const double GridCellSize = 15;
        private readonly Random _random = new Random();

        // Цвета для типов местности
        private readonly Dictionary<byte, Color> _terrainColors = new Dictionary<byte, Color>
        {
            { 1, Color.FromRgb(144, 238, 144) }, // Meadows
            { 2, Color.FromRgb(34, 139, 34) },   // Forest
            { 3, Color.FromRgb(139, 69, 19) },   // Mountains
            { 4, Color.FromRgb(30, 144, 255) },  // Water
            { 5, Color.FromRgb(105, 105, 105) }, // City
            { 6, Color.FromRgb(255, 200, 0) },   // Educational
            { 7, Color.FromRgb(0, 150, 255) },   // Airport - синий
            { 8, Color.FromRgb(139, 90, 43) }    // Port - коричневый
        };

        private readonly Color _roadColor = Color.FromRgb(64, 64, 64);
        private readonly Color _parkColor = Color.FromRgb(0x8E, 0xDB, 0x12);
        private readonly Color _bikePathColor = Color.FromRgb(0xE6, 0xE8, 0x84);

        // Коллекция визуальных элементов самолетов
        private readonly Dictionary<Plane, Polygon> _planeVisuals = new Dictionary<Plane, Polygon>();

        // Коллекция визуальных элементов больниц
        private readonly Dictionary<MedicalPoint, Canvas> _medicalPointVisuals = new Dictionary<MedicalPoint, Canvas>();

        // Коллекция визуальных элементов машин скорой помощи
        private readonly Dictionary<AmbulanceVehicle, Canvas> _ambulanceVisuals = new Dictionary<AmbulanceVehicle, Canvas>();

        // Коллекция визуальных элементов вызовов
        private readonly Dictionary<MedicalEmergencyCall, Ellipse> _callVisuals = new Dictionary<MedicalEmergencyCall, Ellipse>();

        // Коллекция визуальных элементов жилых домов
        private readonly Dictionary<ResidentialBuilding, Canvas> _buildingVisuals = new Dictionary<ResidentialBuilding, Canvas>();

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            DataContext = ViewModel;

            // Подписываемся на изменение свойств
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            // Подписываемся на изменения коллекции самолетов
            ViewModel.Planes.CollectionChanged += Planes_CollectionChanged;

            // Инициализируем визуализацию кораблей
            InitializeShipVisuals();

            // Инициализируем визуализацию медицинских служб
            InitializeMedicalVisuals();

            // Инициализируем визуализацию жилых домов
            InitializeResidentialVisuals();

            // Инициализируем карту
            InitializeMap();

            // Обновляем тексты
            //UpdateYearText();
            //UpdatePopulationText();

            // Центрируем карту после загрузки
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CenterMap();
        }

        private void InitializeMap()
        {
            // Очищаем ItemsControl
            MapItemsControl.Items.Clear();

            int airportCount = 0;
            // Создаем прямоугольники для каждой ячейки (увеличенные в 1.5 раза)
            foreach (var cell in ViewModel.Map.Cells)
            {
                var rectangle = new Rectangle
                {
                    Width = 15,  // Было 10, стало 15 (увеличение в 1.5 раза)
                    Height = 15, // Было 10, стало 15 (увеличение в 1.5 раза)
                    ToolTip = cell.ToolTip,
                    DataContext = cell
                };

                // Устанавливаем цвет
                if (_terrainColors.TryGetValue(cell.TerrainType, out var color))
                {
                    Color fillColor;

                    // ИСПРАВЛЕНИЕ: Координаты в клетках поменяны местами, поэтому при проверке тоже меняем местами
                    if (ViewModel.Map.IsRoad(cell.Y, cell.X))
                    {
                        fillColor = _roadColor;
                    }
                    else if (ViewModel.Map.IsPark(cell.Y, cell.X))
                    {
                        fillColor = _parkColor;
                    }
                    else if (ViewModel.Map.IsBikePath(cell.Y, cell.X))
                    {
                        fillColor = _bikePathColor;
                    }
                    else
                    {
                        fillColor = color;
                    }

                    rectangle.Fill = new SolidColorBrush(fillColor);
                    rectangle.Stroke = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                    rectangle.StrokeThickness = 0.8;

                    // Для аэропортов делаем более заметную границу
                    if (cell.TerrainType == 7) // Airport
                    {
                        rectangle.Stroke = new SolidColorBrush(Colors.White);
                        rectangle.StrokeThickness = 2;
                        airportCount++;
                        System.Diagnostics.Debug.WriteLine($"Аэропорт найден: X={cell.X}, Y={cell.Y}");
                    }
                    
                    // Для портов делаем более заметную границу
                    if (cell.TerrainType == 8) // Port
                    {
                        rectangle.Stroke = new SolidColorBrush(Colors.Yellow);
                        rectangle.StrokeThickness = 2;
                    }
                }
                else
                {
                    // Если цвет не найден, используем серый по умолчанию
                    rectangle.Fill = new SolidColorBrush(Colors.Gray);
                    rectangle.Stroke = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                    rectangle.StrokeThickness = 0.8;
                }

                // Добавляем обработчик события
                rectangle.MouseEnter += Rectangle_MouseEnter;

                // Обновляем подсказку
                string tooltip = cell.ToolTip;
                // ИСПРАВЛЕНИЕ: Координаты в клетках поменяны местами
                if (ViewModel.Map.IsPark(cell.Y, cell.X))
                {
                    tooltip += "\nЗона: Парк";
                }
                else if (ViewModel.Map.IsBikePath(cell.Y, cell.X))
                {
                    tooltip += "\nЗона: Велодорожка";
                }
                else if (ViewModel.Map.IsRoad(cell.Y, cell.X))
                {
                    tooltip += "\nЗона: Дорога";
                }
                rectangle.ToolTip = tooltip;

                // Добавляем в ItemsControl
                MapItemsControl.Items.Add(rectangle);
            }
            
            System.Diagnostics.Debug.WriteLine($"Всего аэропортов на карте: {airportCount}");
            System.Diagnostics.Debug.WriteLine($"Аэропорт 1: [{ViewModel.Map.Airport1X}, {ViewModel.Map.Airport1Y}]");
            System.Diagnostics.Debug.WriteLine($"Аэропорт 2: [{ViewModel.Map.Airport2X}, {ViewModel.Map.Airport2Y}]");

            // Увеличиваем размер канваса пропорционально
            MapCanvas.Width = 1500; // Было 1000, стало 1500
            MapCanvas.Height = 1500; // Было 1000, стало 1500
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.CurrentYear))
            {
                YearText.Text = $"Год: {ViewModel.CurrentYear}";
            }
            else if (e.PropertyName == nameof(ViewModel.Population))
            {
                PopulationText.Text = $"Население: {ViewModel.Population}";
            }
        }


        

        #region Кнопки времени

        private void Add1YearButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddYears(1);
        }

        private void Add5YearsButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddYears(5);
        }

        private void Add10YearsButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddYears(10);
        }

        #endregion

        #region Pan Functionality

        private void CenterMap()
        {
            // Ждем пока все элементы отрендерятся
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // Центрируем по горизонтали
                    double horizontalOffset = (MapScrollViewer.ExtentWidth - MapScrollViewer.ViewportWidth) / 2;
                    MapScrollViewer.ScrollToHorizontalOffset(horizontalOffset);

                    // Центрируем по вертикали
                    double verticalOffset = (MapScrollViewer.ExtentHeight - MapScrollViewer.ViewportHeight) / 2;
                    MapScrollViewer.ScrollToVerticalOffset(verticalOffset);
                }
                catch (Exception ex)
                {
                    // Игнорируем ошибки центрирования
                    Console.WriteLine($"Centering error: {ex.Message}");
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void MapScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                _lastMousePosition = e.GetPosition(MapScrollViewer);
                MapScrollViewer.Cursor = Cursors.SizeAll;
                MapScrollViewer.CaptureMouse();
                e.Handled = true;
            }
        }

        private void MapScrollViewer_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                MapScrollViewer.Cursor = Cursors.Arrow;
                MapScrollViewer.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void MapScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning && e.MiddleButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(MapScrollViewer);
                Vector delta = currentPosition - _lastMousePosition;

                MapScrollViewer.ScrollToHorizontalOffset(MapScrollViewer.HorizontalOffset - delta.X);
                MapScrollViewer.ScrollToVerticalOffset(MapScrollViewer.VerticalOffset - delta.Y);

                _lastMousePosition = currentPosition;
                e.Handled = true;
            }
        }
        private void MapCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var point = e.GetPosition(MapCanvas);
            var gridPoint = CanvasToGrid(point);
            System.Diagnostics.Debug.WriteLine($"ПКМ на карте: Canvas=({point.X:F1}, {point.Y:F1}), Grid=({gridPoint.X}, {gridPoint.Y})");
            
            // Проверяем, какая клетка находится под курсором через HitTest
            var hitTestResult = VisualTreeHelper.HitTest(MapCanvas, point);
            DependencyObject current = hitTestResult?.VisualHit as DependencyObject;
            
            CellViewModel foundCell = null;
            while (current != null)
            {
                if (current is Rectangle rect && rect.DataContext is CellViewModel cell)
                {
                    foundCell = cell;
                    break;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            
            if (foundCell != null)
            {
                System.Diagnostics.Debug.WriteLine($"  Под курсором найдена клетка [{foundCell.X}, {foundCell.Y}]");
                // Теперь координаты в клетках уже поменяны местами, используем их напрямую
                ViewModel.EconomySimulation.SetPlacementCell(foundCell.X, foundCell.Y);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"  Клетка не найдена через HitTest, используем преобразованные координаты");
                ViewModel.EconomySimulation.SetPlacementCell((int)gridPoint.X, (int)gridPoint.Y);
            }
        }

        private void MapCanvas_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // Если идет панорамирование, блокируем контекстное меню
            if (_isPanning)
            {
                e.Handled = true;
                return;
            }
            
            // Убеждаемся, что координаты установлены
            var point = Mouse.GetPosition(MapCanvas);
            var gridPoint = CanvasToGrid(point);
            System.Diagnostics.Debug.WriteLine($"ContextMenuOpening: Canvas=({point.X:F1}, {point.Y:F1}), Grid=({gridPoint.X}, {gridPoint.Y})");
            
            // Пробуем найти клетку под курсором
            var hitTestResult = VisualTreeHelper.HitTest(MapCanvas, point);
            DependencyObject current = hitTestResult?.VisualHit as DependencyObject;
            
            CellViewModel foundCell = null;
            while (current != null)
            {
                if (current is Rectangle rect && rect.DataContext is CellViewModel cell)
                {
                    foundCell = cell;
                    break;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            
            if (foundCell != null)
            {
                System.Diagnostics.Debug.WriteLine($"  В ContextMenuOpening найдена клетка [{foundCell.X}, {foundCell.Y}]");
                // Теперь координаты в клетках уже поменяны местами, используем их напрямую
                ViewModel.EconomySimulation.SetPlacementCell(foundCell.X, foundCell.Y);
                _pendingMedicalPointPlacement = new Point(foundCell.X, foundCell.Y);
                _pendingBuildingPlacement = new Point(foundCell.X, foundCell.Y);
            }
            else
            {
                ViewModel.EconomySimulation.SetPlacementCell((int)gridPoint.X, (int)gridPoint.Y);
                _pendingMedicalPointPlacement = gridPoint;
                _pendingBuildingPlacement = gridPoint;
            }
        }

        private Point CanvasToGrid(Point point)
        {
            // Теперь координаты в клетках уже поменяны местами при создании (X=y, Y=x)
            // UniformGrid с Columns=100 размещает элементы в порядке их добавления
            // Поэтому: point.X / 15 = x (колонка), point.Y / 15 = y (строка)
            // Но в клетках X=y, Y=x, поэтому gridX = point.X / 15, gridY = point.Y / 15
            int x = Math.Clamp((int)(point.X / GridCellSize), 0, ViewModel.Map.Width - 1);
            int y = Math.Clamp((int)(point.Y / GridCellSize), 0, ViewModel.Map.Height - 1);
            System.Diagnostics.Debug.WriteLine($"CanvasToGrid: Canvas=({point.X:F1}, {point.Y:F1}) -> Grid=({x}, {y})");
            return new Point(x, y);
        }

        private void Facility_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button button && button.DataContext is EconomyFacilityViewModel facility)
            {
                // Если это логистический центр - открываем окно с ресурсами
                if (facility.IsLogisticsHub)
                {
                    e.Handled = true;
                    OpenLogisticsHubWindow(facility);
                    return;
                }

                // Для остальных объектов - обычное перетаскивание
                if (facility.IsDraggable)
                {
                    _draggedFacility = facility;
                    _dragStartPoint = e.GetPosition(MapCanvas);
                    button.CaptureMouse();
                }
            }
        }

        private void Facility_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button button && button.DataContext is EconomyFacilityViewModel facility)
            {
                // Выбираем объект для удаления
                ViewModel.EconomySimulation.SelectedFacility = facility;
                e.Handled = false; // Разрешаем показать контекстное меню
            }
        }

        private void OpenLogisticsHubWindow(EconomyFacilityViewModel hub)
        {
            var window = new Window
            {
                Title = $"Логистический центр: {hub.Name}",
                Width = 650,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 40))
            };
            
            // Таймер для обновления данных в реальном времени
            var updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var stackPanel = new StackPanel { Margin = new Thickness(15) };
            
            var title = new TextBlock
            {
                Text = $"📦 {hub.Name}",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 15)
            };
            stackPanel.Children.Add(title);

            var info = new TextBlock
            {
                Text = $"Запасы: {hub.Storage:0.#}/{hub.Capacity:0.#} т",
                FontSize = 14,
                Foreground = Brushes.LightGray,
                Margin = new Thickness(0, 0, 0, 15)
            };
            stackPanel.Children.Add(info);

            // Показываем статистику ресурсов из ResourceStats
            var statsLabel = new TextBlock
            {
                Text = "Статистика ресурсов:",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(statsLabel);

            var statsPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
            foreach (var stat in ViewModel.EconomySimulation.ResourceStats)
            {
                var statBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(43, 43, 43)),
                    Padding = new Thickness(8),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                
                var statStack = new StackPanel();
                var statName = new TextBlock
                {
                    Text = stat.DisplayName,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    FontSize = 12
                };
                statStack.Children.Add(statName);
                
                var statValue = new TextBlock
                {
                    Text = stat.TotalStockText,
                    Foreground = Brushes.LightGray,
                    FontSize = 11
                };
                statStack.Children.Add(statValue);
                
                var statTrend = new TextBlock
                {
                    Text = stat.TrendText,
                    Foreground = new SolidColorBrush(stat.Trend > 0 ? Colors.LightGreen : (stat.Trend < 0 ? Colors.LightCoral : Colors.LightGray)),
                    FontSize = 10
                };
                statStack.Children.Add(statTrend);
                
                statBorder.Child = statStack;
                statsPanel.Children.Add(statBorder);
            }
            stackPanel.Children.Add(statsPanel);

            // Показываем управление производством готовой продукции
            var productionLabel = new TextBlock
            {
                Text = "Управление производством:",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(productionLabel);

            // Получаем все типы продукции, которые могут быть произведены
            var productTypes = new Dictionary<ProductType, string>
            {
                { ProductType.Steel, "Сталь" },
                { ProductType.EngineeredWood, "Доски" },
                { ProductType.Fuel, "Топливо" },
                { ProductType.Chemicals, "Химикаты" },
                { ProductType.Conductors, "Провода" },
                { ProductType.Coolant, "Охладитель" }
            };

            var productionPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
            bool hasAnyProduction = false;

            foreach (var productKvp in productTypes)
            {
                var productType = productKvp.Key;
                var productName = productKvp.Value;
                
                // Находим все объекты, производящие этот тип продукции
                var producingFacilities = ViewModel.EconomySimulation.Facilities
                    .Where(f => f.Blueprint?.Stage == ProcessingStage.Manufacturing &&
                                ViewModel.EconomySimulation.ConvertProduct(f.Blueprint.Resource) == productType)
                    .ToList();

                if (producingFacilities.Count == 0) continue;
                
                hasAnyProduction = true;
                
                // Получаем общее количество произведенной продукции
                double totalProduced = ViewModel.EconomySimulation.GetProducedProduct(productType);
                bool isAnyActive = producingFacilities.Any(f => f.IsActive);

                var productBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(43, 43, 43)),
                    Padding = new Thickness(10),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var productGrid = new Grid();
                productGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                productGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var productInfo = new StackPanel();
                var productNameText = new TextBlock
                {
                    Text = productName,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    FontSize = 12
                };
                productInfo.Children.Add(productNameText);

                var productStatus = new TextBlock
                {
                    Text = isAnyActive ? "🟢 Производится" : "🔴 Остановлено",
                    Foreground = isAnyActive ? Brushes.LightGreen : Brushes.LightCoral,
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                productInfo.Children.Add(productStatus);

                var productAmount = new TextBlock
                {
                    Text = $"Произведено: {totalProduced:0.#} т",
                    Foreground = Brushes.LightGray,
                    FontSize = 10,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                productInfo.Children.Add(productAmount);
                
                // Проверяем наличие ресурсов для производства
                var resourceCheck = ViewModel.EconomySimulation.CheckProductionResources(productType, hub);
                if (!string.IsNullOrEmpty(resourceCheck))
                {
                    var resourceWarning = new TextBlock
                    {
                        Text = $"⚠️ {resourceCheck}",
                        Foreground = Brushes.Orange,
                        FontSize = 9,
                        Margin = new Thickness(0, 2, 0, 0),
                        TextWrapping = TextWrapping.Wrap
                    };
                    productInfo.Children.Add(resourceWarning);
                }
                
                // Обновляем данные в реальном времени
                var productTypeRef = productType;
                var amountRef = productAmount;
                var statusRef = productStatus;
                var hubRef = hub;
                var productInfoRef = productInfo;
                updateTimer.Tick += (s, e) =>
                {
                    if (amountRef != null && statusRef != null && productInfoRef != null)
                    {
                        var currentProduced = ViewModel.EconomySimulation.GetProducedProduct(productTypeRef);
                        var currentFacilities = ViewModel.EconomySimulation.Facilities
                            .Where(f => f.Blueprint?.Stage == ProcessingStage.Manufacturing &&
                                        ViewModel.EconomySimulation.ConvertProduct(f.Blueprint.Resource) == productTypeRef)
                            .ToList();
                        bool currentActive = currentFacilities.Any(f => f.IsActive);
                        
                        amountRef.Text = $"Произведено: {currentProduced:0.#} т";
                        statusRef.Text = currentActive ? "🟢 Производится" : "🔴 Остановлено";
                        statusRef.Foreground = currentActive ? Brushes.LightGreen : Brushes.LightCoral;
                        
                        // Обновляем предупреждение о ресурсах
                        // Удаляем старое предупреждение, если есть
                        var oldWarning = productInfoRef.Children.OfType<TextBlock>()
                            .FirstOrDefault(tb => tb.Foreground == Brushes.Orange && tb.Text.StartsWith("⚠️"));
                        if (oldWarning != null)
                        {
                            productInfoRef.Children.Remove(oldWarning);
                        }
                        
                        // Добавляем новое предупреждение, если нужно
                        var resourceCheck = ViewModel.EconomySimulation.CheckProductionResources(productTypeRef, hubRef);
                        if (!string.IsNullOrEmpty(resourceCheck))
                        {
                            var resourceWarning = new TextBlock
                            {
                                Text = $"⚠️ {resourceCheck}",
                                Foreground = Brushes.Orange,
                                FontSize = 9,
                                Margin = new Thickness(0, 2, 0, 0),
                                TextWrapping = TextWrapping.Wrap
                            };
                            productInfoRef.Children.Add(resourceWarning);
                        }
                    }
                };

                Grid.SetColumn(productInfo, 0);
                productGrid.Children.Add(productInfo);

                var toggleButton = new Button
                {
                    Content = isAnyActive ? "⏸ Остановить" : "▶ Запустить",
                    Width = 120,
                    Height = 30,
                    Margin = new Thickness(5, 0, 5, 0),
                    Background = isAnyActive 
                        ? new SolidColorBrush(Color.FromRgb(200, 100, 100)) 
                        : new SolidColorBrush(Color.FromRgb(100, 200, 100)),
                    Foreground = Brushes.White,
                    FontSize = 11
                };
                toggleButton.Click += (s, e) =>
                {
                    ViewModel.EconomySimulation.ToggleProductProductionCommand.Execute(productType);
                    var updatedFacilities = ViewModel.EconomySimulation.Facilities
                        .Where(f => f.Blueprint?.Stage == ProcessingStage.Manufacturing &&
                                    ViewModel.EconomySimulation.ConvertProduct(f.Blueprint.Resource) == productType)
                        .ToList();
                    bool newState = updatedFacilities.Any(f => f.IsActive);
                    
                    toggleButton.Content = newState ? "⏸ Остановить" : "▶ Запустить";
                    toggleButton.Background = newState 
                        ? new SolidColorBrush(Color.FromRgb(200, 100, 100)) 
                        : new SolidColorBrush(Color.FromRgb(100, 200, 100));
                    productStatus.Text = newState ? "🟢 Производится" : "🔴 Остановлено";
                    productStatus.Foreground = newState ? Brushes.LightGreen : Brushes.LightCoral;
                };
                Grid.SetColumn(toggleButton, 1);
                productGrid.Children.Add(toggleButton);

                productBorder.Child = productGrid;
                productionPanel.Children.Add(productBorder);
            }

            if (!hasAnyProduction)
            {
                var noProduction = new TextBlock
                {
                    Text = "Нет производственных объектов",
                    FontSize = 12,
                    Foreground = Brushes.LightGray,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                stackPanel.Children.Add(noProduction);
            }
            else
            {
                stackPanel.Children.Add(productionPanel);
            }

            // Добавляем график производства ресурсов
            var graphLabel = new TextBlock
            {
                Text = "График производства:",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 15, 0, 10)
            };
            stackPanel.Children.Add(graphLabel);

            var productionGraph = new Views.ProductionGraphView();
            productionGraph.SetEconomySimulation(ViewModel.EconomySimulation);
            productionGraph.Margin = new Thickness(0, 0, 0, 15);
            stackPanel.Children.Add(productionGraph);

            // Показываем информацию о потоках
            var flowsLabel = new TextBlock
            {
                Text = "Активные потоки:",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(flowsLabel);

            var flowsText = new TextBlock
            {
                Text = GetHubResourcesText(hub),
                FontSize = 12,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            };
            stackPanel.Children.Add(flowsText);
            
            // Обновляем отображение ресурсов в реальном времени
            var flowsTextRef = flowsText;
            var hubForResources = hub;
            updateTimer.Tick += (s, e) =>
            {
                if (flowsTextRef != null && hubForResources != null)
                {
                    flowsTextRef.Text = GetHubResourcesText(hubForResources);
                }
            };

            var closeButton = new Button
            {
                Content = "Закрыть",
                Width = 100,
                Height = 30,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            closeButton.Click += (s, e) => window.Close();
            stackPanel.Children.Add(closeButton);

            scrollViewer.Content = stackPanel;
            window.Content = scrollViewer;
            
            // Запускаем таймер обновления
            updateTimer.Start();
            
            // Останавливаем таймер при закрытии окна
            window.Closed += (s, e) => updateTimer.Stop();
            
            window.ShowDialog();
        }

        private string GetHubResourcesText(EconomyFacilityViewModel hub)
        {
            // Получаем ресурсы по типам из логистического центра
            var hubResources = ViewModel.EconomySimulation.GetHubResources(hub);
            
            if (hubResources.Count == 0)
            {
                return "Ресурсы в хранилище:\n• Ресурсы еще не доставлены";
            }
            
            var result = "Ресурсы в хранилище:\n";
            
            // Показываем ресурсы по типам
            foreach (var kvp in hubResources.OrderBy(r => r.Key.ToString()))
            {
                var resourceName = kvp.Key switch
                {
                    CoreResource.Wood => "Древесина",
                    CoreResource.Iron => "Железо",
                    CoreResource.Copper => "Медь",
                    CoreResource.Oil => "Нефть",
                    CoreResource.Coal => "Уголь",
                    CoreResource.Water => "Вода",
                    _ => kvp.Key.ToString()
                };
                result += $"• {resourceName}: {kvp.Value:0.#} т\n";
            }
            
            return result;
        }

        private void Facility_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedFacility == null || e.LeftButton != MouseButtonState.Pressed) return;

            var current = e.GetPosition(MapCanvas);
            if ((current - _dragStartPoint).Length < 5) return;

            var gridPoint = CanvasToGrid(current);
            var success = ViewModel.EconomySimulation.MoveFacility(_draggedFacility, (int)gridPoint.X, (int)gridPoint.Y);
            if (!success)
            {
                // Если перемещение не удалось, отменяем перетаскивание
                if (sender is Button button)
                {
                    button.ReleaseMouseCapture();
                }
                _draggedFacility = null;
            }
        }

        private void Facility_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button button)
            {
                button.ReleaseMouseCapture();
            }
            _draggedFacility = null;
        }


        #endregion

        private void Rectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Rectangle rectangle && rectangle.DataContext is CellViewModel cell)
            {
                // Отладка: проверяем координаты клетки и позицию мыши
                var mousePos = e.GetPosition(MapCanvas);
                var gridPos = CanvasToGrid(mousePos);
                System.Diagnostics.Debug.WriteLine($"Rectangle_MouseEnter: клетка в DataContext=[{cell.X}, {cell.Y}], мышь Canvas=({mousePos.X:F1}, {mousePos.Y:F1}), Grid=({gridPos.X}, {gridPos.Y})");
                ViewModel.SelectedCell = cell;
                ViewModel.UpdateCellInfo(cell);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.None)
            {
                CenterMap();
                e.Handled = true;
            }
            else if (e.Key == Key.Home)
            {
                CenterMap();
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            if (ViewModel.Planes != null)
            {
                ViewModel.Planes.CollectionChanged -= Planes_CollectionChanged;
            }
            if (ViewModel.Ships != null)
            {
                ViewModel.Ships.CollectionChanged -= Ships_CollectionChanged;
            }
            Loaded -= MainWindow_Loaded;
            base.OnClosed(e);
        }

        private void Planes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Обновляем UI в UI потоке
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (e.NewItems != null)
                {
                    foreach (Plane plane in e.NewItems)
                    {
                        CreatePlaneVisual(plane);
                        plane.PropertyChanged += Plane_PropertyChanged;
                    }
                }

                if (e.OldItems != null)
                {
                    foreach (Plane plane in e.OldItems)
                    {
                        RemovePlaneVisual(plane);
                        plane.PropertyChanged -= Plane_PropertyChanged;
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Normal);
        }

        private void CreatePlaneVisual(Plane plane)
        {
            // Создаем белый треугольник (самолет)
            var triangle = new Polygon
            {
                Fill = new SolidColorBrush(Colors.White),
                Stroke = new SolidColorBrush(Colors.DarkGray),
                StrokeThickness = 1,
                ToolTip = $"Самолет [{plane.FromAirportX},{plane.FromAirportY}] -> [{plane.ToAirportX},{plane.ToAirportY}]",
                Opacity = 1.0
            };

            // Размер треугольника (увеличенный)
            double size = 20;
            double halfSize = size / 2;

            // Вычисляем направление движения для поворота треугольника
            double dx = plane.ToAirportX - plane.FromAirportX;
            double dy = plane.ToAirportY - plane.FromAirportY;
            double angle = Math.Atan2(dy, dx) * 180 / Math.PI;

            // Создаем точки треугольника (направлен вправо, затем поворачиваем)
            var points = new PointCollection
            {
                new Point(halfSize, 0),                    // Вершина (нос самолета)
                new Point(-halfSize, -halfSize * 0.6),      // Левая нижняя точка
                new Point(-halfSize * 0.3, 0),              // Центральная точка хвоста
                new Point(-halfSize, halfSize * 0.6)       // Правая нижняя точка
            };

            triangle.Points = points;

            // Поворачиваем треугольник в направлении движения (центр поворота - центр треугольника)
            var transform = new RotateTransform(angle, 0, 0);
            triangle.RenderTransformOrigin = new Point(0.5, 0.5);
            triangle.RenderTransform = transform;

            // Устанавливаем Z-Index, чтобы самолеты были поверх карты
            Panel.SetZIndex(triangle, 1000);

            // Убеждаемся, что координаты в пределах Canvas
            double x = Math.Max(0, Math.Min(MapCanvas.Width - size, plane.X - halfSize));
            double y = Math.Max(0, Math.Min(MapCanvas.Height - size, plane.Y - halfSize));
            
            Canvas.SetLeft(triangle, x);
            Canvas.SetTop(triangle, y);

            MapCanvas.Children.Add(triangle);
            _planeVisuals[plane] = triangle;
            
            // Отладочный вывод
            System.Diagnostics.Debug.WriteLine($"Самолет создан: X={plane.X}, Y={plane.Y}, Canvas X={x}, Canvas Y={y}");
        }

        private void RemovePlaneVisual(Plane plane)
        {
            if (_planeVisuals.TryGetValue(plane, out var polygon))
            {
                MapCanvas.Children.Remove(polygon);
                _planeVisuals.Remove(plane);
            }
        }

        private void Plane_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Обновляем UI в UI потоке
            if (sender is Plane plane && _planeVisuals.TryGetValue(plane, out var polygon))
            {
                if (e.PropertyName == nameof(Plane.X) || e.PropertyName == nameof(Plane.Y))
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // Убеждаемся, что координаты в пределах Canvas
                        double size = 20;
                        double halfSize = size / 2;
                        double x = Math.Max(0, Math.Min(MapCanvas.Width - size, plane.X - halfSize));
                        double y = Math.Max(0, Math.Min(MapCanvas.Height - size, plane.Y - halfSize));
                        Canvas.SetLeft(polygon, x);
                        Canvas.SetTop(polygon, y);
                        
                        // Обновляем поворот треугольника в направлении движения (на основе текущей позиции)
                        double currentDx = plane.ToAirportX * 15.0 + 7.5 - plane.X;
                        double currentDy = plane.ToAirportY * 15.0 + 7.5 - plane.Y;
                        double angle = Math.Atan2(currentDy, currentDx) * 180 / Math.PI;
                        if (polygon.RenderTransform is RotateTransform rotateTransform)
                        {
                            rotateTransform.Angle = angle;
                        }
                        else
                        {
                            // Если трансформация еще не создана, создаем ее
                            polygon.RenderTransform = new RotateTransform(angle);
                            polygon.RenderTransformOrigin = new Point(0.5, 0.5);
                        }
                    }), System.Windows.Threading.DispatcherPriority.Render);
                }
            }
        }

        private void OpenThirdAppButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var financialWindow = new FinancialWindow();
                financialWindow.DataContext = ViewModel.FinancialOverview;
                financialWindow.Owner = this;
                financialWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть финансовую систему: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenEmergencyServiceButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new Views.EmergencyServiceManagementWindow(ViewModel);
            window.Show();
        }

        private void OpenForeignRelationsButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new Views.ForeignRelationsManagementWindow(ViewModel);
            window.Show();
        }

        private void OpenTrafficManagementButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Обновляем TrafficManagementViewModel с ссылкой на EconomySimulation
                if (ViewModel.TrafficManagementViewModel != null)
                {
                    // Если нужно обновить ссылку, создаем новый экземпляр
                    // Но лучше передать через конструктор при создании
                }

                var trafficView = new GigaCity_Labor3_OOP.Views.TrafficManagementView
                {
                    DataContext = ViewModel.TrafficManagementViewModel
                };

                var hostWindow = new Window
                {
                    Title = "Управление трафиком",
                    Content = trafficView,
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };

                hostWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть управление трафиком: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CellInfoView_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void SaveGameButton_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*",
                FileName = "game_save.json",
                DefaultExt = "json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var saveData = new
                    {
                        Facilities = ViewModel.EconomySimulation.Facilities.Select(f => new
                        {
                            BlueprintId = f.Blueprint?.Id,
                            GridX = f.GridX,
                            GridY = f.GridY,
                            Storage = f.Storage,
                            IsActive = f.IsActive,
                            IsDraggable = f.IsDraggable
                        }).ToList(),
                        TotalProduction = ViewModel.EconomySimulation.TotalProduction,
                        EconomicIndicator = ViewModel.EconomySimulation.EconomicIndicator
                    };

                    var json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(saveDialog.FileName, json);
                    
                    MessageBox.Show("Игра успешно сохранена!", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadGameButton_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*",
                DefaultExt = "json"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(openDialog.FileName);
                    var saveData = JsonSerializer.Deserialize<JsonElement>(json);

                    // Очищаем текущие объекты (кроме логистического центра)
                    var hub = ViewModel.EconomySimulation.Facilities.FirstOrDefault(f => f.IsLogisticsHub);
                    ViewModel.EconomySimulation.Facilities.Clear();
                    if (hub != null)
                    {
                        ViewModel.EconomySimulation.Facilities.Add(hub);
                    }

                    // Загружаем объекты
                    if (saveData.TryGetProperty("Facilities", out var facilitiesElement))
                    {
                        foreach (var facilityElement in facilitiesElement.EnumerateArray())
                        {
                            if (facilityElement.TryGetProperty("BlueprintId", out var blueprintIdElement))
                            {
                                var blueprintId = blueprintIdElement.GetString();
                                if (!string.IsNullOrEmpty(blueprintId) && blueprintId != "logistics-hub")
                                {
                                    var gridX = facilityElement.TryGetProperty("GridX", out var x) ? (int)x.GetDouble() : 0;
                                    var gridY = facilityElement.TryGetProperty("GridY", out var y) ? (int)y.GetDouble() : 0;
                                    
                                    var result = ViewModel.EconomySimulation.TryPlaceBuilding(blueprintId, gridX, gridY);
                                    if (result)
                                    {
                                        var facility = ViewModel.EconomySimulation.Facilities.LastOrDefault();
                                        if (facility != null)
                                        {
                                            if (facilityElement.TryGetProperty("Storage", out var storage))
                                            {
                                                facility.UpdateState(facility.CurrentProduction, storage.GetDouble(), facility.State, facility.Utilization);
                                            }
                                            if (facilityElement.TryGetProperty("IsActive", out var isActive))
                                            {
                                                facility.IsActive = isActive.GetBoolean();
                                            }
                                            if (facilityElement.TryGetProperty("IsDraggable", out var isDraggable))
                                            {
                                                facility.IsDraggable = isDraggable.GetBoolean();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    ViewModel.EconomySimulation.RebuildNetwork();
                    MessageBox.Show("Игра успешно загружена!", "Загрузка", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #region Medical Service Visualization

        private Point? _pendingMedicalPointPlacement;

        private void InitializeMedicalVisuals()
        {
            if (ViewModel?.MedicalService == null) return;

            // Подписываемся на изменения коллекций
            ViewModel.MedicalService.MedicalPoints.CollectionChanged += MedicalPoints_CollectionChanged;
            ViewModel.MedicalService.AllAmbulances.CollectionChanged += Ambulances_CollectionChanged;
            ViewModel.MedicalService.EmergencyCalls.CollectionChanged += EmergencyCalls_CollectionChanged;

            // Создаем визуальные элементы для существующих объектов
            foreach (var medicalPoint in ViewModel.MedicalService.MedicalPoints)
            {
                CreateMedicalPointVisual(medicalPoint);
            }

            foreach (var ambulance in ViewModel.MedicalService.AllAmbulances)
            {
                CreateAmbulanceVisual(ambulance);
            }

            foreach (var call in ViewModel.MedicalService.EmergencyCalls)
            {
                CreateEmergencyCallVisual(call);
            }

            // Создаем таймер для обновления позиций машин
            var updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            updateTimer.Tick += MedicalUpdateTimer_Tick;
            updateTimer.Start();
        }

        private void MedicalPoints_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (e.NewItems != null)
                {
                    foreach (MedicalPoint medicalPoint in e.NewItems)
                    {
                        CreateMedicalPointVisual(medicalPoint);
                        medicalPoint.PropertyChanged += MedicalPoint_PropertyChanged;
                    }
                }

                if (e.OldItems != null)
                {
                    foreach (MedicalPoint medicalPoint in e.OldItems)
                    {
                        RemoveMedicalPointVisual(medicalPoint);
                        medicalPoint.PropertyChanged -= MedicalPoint_PropertyChanged;
                    }
                }
            }), DispatcherPriority.Normal);
        }

        private void Ambulances_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (e.NewItems != null)
                {
                    foreach (AmbulanceVehicle ambulance in e.NewItems)
                    {
                        CreateAmbulanceVisual(ambulance);
                        ambulance.PropertyChanged += Ambulance_PropertyChanged;
                    }
                }

                if (e.OldItems != null)
                {
                    foreach (AmbulanceVehicle ambulance in e.OldItems)
                    {
                        RemoveAmbulanceVisual(ambulance);
                        ambulance.PropertyChanged -= Ambulance_PropertyChanged;
                    }
                }
            }), DispatcherPriority.Normal);
        }

        private void EmergencyCalls_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (e.NewItems != null)
                {
                    foreach (MedicalEmergencyCall call in e.NewItems)
                    {
                        CreateEmergencyCallVisual(call);
                    }
                }

                if (e.OldItems != null)
                {
                    foreach (MedicalEmergencyCall call in e.OldItems)
                    {
                        RemoveEmergencyCallVisual(call);
                    }
                }
            }), DispatcherPriority.Normal);
        }

        private void CreateMedicalPointVisual(MedicalPoint medicalPoint)
        {
            // Создаем контейнер для больницы
            var container = new Canvas();
            
            // Синий квадрат с белым крестом
            var square = new Rectangle
            {
                Width = 30,
                Height = 30,
                Fill = new SolidColorBrush(Color.FromRgb(0, 100, 200)), // Синий цвет
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2
            };
            Canvas.SetLeft(square, -15);
            Canvas.SetTop(square, -15);
            container.Children.Add(square);

            // Белый крест
            var cross1 = new Rectangle
            {
                Width = 4,
                Height = 20,
                Fill = new SolidColorBrush(Colors.White)
            };
            Canvas.SetLeft(cross1, -2);
            Canvas.SetTop(cross1, -10);
            container.Children.Add(cross1);

            var cross2 = new Rectangle
            {
                Width = 20,
                Height = 4,
                Fill = new SolidColorBrush(Colors.White)
            };
            Canvas.SetLeft(cross2, -10);
            Canvas.SetTop(cross2, -2);
            container.Children.Add(cross2);

            // Текст с количеством пациентов
            var patientText = new TextBlock
            {
                Text = $"{medicalPoint.OccupiedRooms}/{medicalPoint.MaxRooms}",
                FontSize = 8,
                Foreground = new SolidColorBrush(Colors.White),
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Canvas.SetLeft(patientText, -15);
            Canvas.SetTop(patientText, 18);
            patientText.Width = 30;
            patientText.TextAlignment = TextAlignment.Center;
            patientText.Name = "PatientCountText";
            container.Children.Add(patientText);

            // Желтые кружки для пациентов
            UpdatePatientVisuals(container, medicalPoint);

            Canvas.SetLeft(container, medicalPoint.XCoordinate);
            Canvas.SetTop(container, medicalPoint.YCoordinate);
            container.ToolTip = $"{medicalPoint.Name}\nПациенты: {medicalPoint.OccupiedRooms}/{medicalPoint.MaxRooms}";
            Panel.SetZIndex(container, 10);

            MapCanvas.Children.Add(container);
            _medicalPointVisuals[medicalPoint] = container;

            // Подписываемся на изменения коллекции пациентов
            medicalPoint.Patients.CollectionChanged += (s, e) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdatePatientVisuals(container, medicalPoint);
                    var textBlock = container.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Name == "PatientCountText");
                    if (textBlock != null)
                    {
                        textBlock.Text = $"{medicalPoint.OccupiedRooms}/{medicalPoint.MaxRooms}";
                    }
                    container.ToolTip = $"{medicalPoint.Name}\nПациенты: {medicalPoint.OccupiedRooms}/{medicalPoint.MaxRooms}";
                }), DispatcherPriority.Normal);
            };
        }

        private void UpdatePatientVisuals(Canvas container, MedicalPoint medicalPoint)
        {
            // Удаляем старые визуальные элементы пациентов (кружки)
            var oldCircles = container.Children.OfType<Ellipse>().ToList();
            foreach (var circle in oldCircles)
            {
                container.Children.Remove(circle);
            }

            // Добавляем желтые кружки для каждого пациента
            int patientCount = medicalPoint.Patients.Count;
            int maxPerRow = 3;
            double circleSize = 4;
            double spacing = 6;

            for (int i = 0; i < patientCount && i < medicalPoint.MaxRooms; i++)
            {
                int row = i / maxPerRow;
                int col = i % maxPerRow;
                double x = -12 + col * spacing;
                double y = -12 + row * spacing;

                var circle = new Ellipse
                {
                    Width = circleSize,
                    Height = circleSize,
                    Fill = new SolidColorBrush(Colors.Yellow),
                    Stroke = new SolidColorBrush(Colors.Orange),
                    StrokeThickness = 0.5
                };
                Canvas.SetLeft(circle, x);
                Canvas.SetTop(circle, y);
                container.Children.Add(circle);
            }
        }

        private void RemoveMedicalPointVisual(MedicalPoint medicalPoint)
        {
            if (_medicalPointVisuals.TryGetValue(medicalPoint, out var visual))
            {
                MapCanvas.Children.Remove(visual);
                _medicalPointVisuals.Remove(medicalPoint);
            }
        }

        private void MedicalPoint_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is MedicalPoint medicalPoint)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_medicalPointVisuals.TryGetValue(medicalPoint, out var container))
                    {
                        if (e.PropertyName == nameof(MedicalPoint.XCoordinate) || e.PropertyName == nameof(MedicalPoint.YCoordinate))
                        {
                            Canvas.SetLeft(container, medicalPoint.XCoordinate);
                            Canvas.SetTop(container, medicalPoint.YCoordinate);
                        }
                        else if (e.PropertyName == nameof(MedicalPoint.OccupiedRooms) || e.PropertyName == nameof(MedicalPoint.Patients))
                        {
                            UpdatePatientVisuals(container, medicalPoint);
                            var textBlock = container.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Name == "PatientCountText");
                            if (textBlock != null)
                            {
                                textBlock.Text = $"{medicalPoint.OccupiedRooms}/{medicalPoint.MaxRooms}";
                            }
                            container.ToolTip = $"{medicalPoint.Name}\nПациенты: {medicalPoint.OccupiedRooms}/{medicalPoint.MaxRooms}";
                        }
                    }
                }), DispatcherPriority.Normal);
            }
        }

        private void CreateAmbulanceVisual(AmbulanceVehicle ambulance)
        {
            // Белый прямоугольник с красным крестом
            var rectangle = new Rectangle
            {
                Width = 12,
                Height = 8,
                Fill = new SolidColorBrush(Colors.White),
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 1
            };

            // Красный крест
            var crossContainer = new Canvas();
            var cross1 = new Rectangle
            {
                Width = 1.5,
                Height = 6,
                Fill = new SolidColorBrush(Colors.Red)
            };
            Canvas.SetLeft(cross1, 5.25);
            Canvas.SetTop(cross1, 1);
            crossContainer.Children.Add(cross1);

            var cross2 = new Rectangle
            {
                Width = 6,
                Height = 1.5,
                Fill = new SolidColorBrush(Colors.Red)
            };
            Canvas.SetLeft(cross2, 3);
            Canvas.SetTop(cross2, 3.25);
            crossContainer.Children.Add(cross2);

            var container = new Canvas();
            container.Children.Add(rectangle);
            container.Children.Add(crossContainer);

            Canvas.SetLeft(container, ambulance.X - 6);
            Canvas.SetTop(container, ambulance.Y - 4);
            container.ToolTip = $"{ambulance.Name}\n{ambulance.StatusText}";
            Panel.SetZIndex(container, 15);

            MapCanvas.Children.Add(container);
            _ambulanceVisuals[ambulance] = container;
        }

        private void RemoveAmbulanceVisual(AmbulanceVehicle ambulance)
        {
            if (_ambulanceVisuals.TryGetValue(ambulance, out var visual))
            {
                MapCanvas.Children.Remove(visual);
                _ambulanceVisuals.Remove(ambulance);
            }
        }

        private void Ambulance_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is AmbulanceVehicle ambulance)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_ambulanceVisuals.TryGetValue(ambulance, out var container))
                    {
                        if (e.PropertyName == nameof(AmbulanceVehicle.X) || e.PropertyName == nameof(AmbulanceVehicle.Y))
                        {
                            Canvas.SetLeft(container, ambulance.X - 6);
                            Canvas.SetTop(container, ambulance.Y - 4);
                        }
                        else if (e.PropertyName == nameof(AmbulanceVehicle.CurrentState) || e.PropertyName == nameof(AmbulanceVehicle.StatusText))
                        {
                            container.ToolTip = $"{ambulance.Name}\n{ambulance.StatusText}";
                        }
                    }
                }), DispatcherPriority.Normal);
            }
        }

        private void CreateEmergencyCallVisual(MedicalEmergencyCall call)
        {
            // Красный кружок
            var ellipse = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(Colors.Red),
                Stroke = new SolidColorBrush(Colors.DarkRed),
                StrokeThickness = 1
            };

            Canvas.SetLeft(ellipse, call.XCoordinate - 5);
            Canvas.SetTop(ellipse, call.YCoordinate - 5);
            ellipse.ToolTip = $"Вызов скорой помощи\nВремя: {call.CallTime:HH:mm:ss}";
            Panel.SetZIndex(ellipse, 12);

            MapCanvas.Children.Add(ellipse);
            _callVisuals[call] = ellipse;
        }

        private void RemoveEmergencyCallVisual(MedicalEmergencyCall call)
        {
            if (_callVisuals.TryGetValue(call, out var visual))
            {
                MapCanvas.Children.Remove(visual);
                _callVisuals.Remove(call);
            }
        }

        private void MedicalUpdateTimer_Tick(object sender, EventArgs e)
        {
            // Обновление позиций машин уже обрабатывается через PropertyChanged
        }

        private void PlaceMedicalPoint_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!_pendingMedicalPointPlacement.HasValue)
            {
                // Пробуем получить координаты из текущей позиции мыши
                var point = Mouse.GetPosition(MapCanvas);
                var gridPoint = CanvasToGrid(point);
                _pendingMedicalPointPlacement = gridPoint;
            }

            int gridX = (int)_pendingMedicalPointPlacement.Value.X;
            int gridY = (int)_pendingMedicalPointPlacement.Value.Y;

            // Проверяем, что координаты в пределах карты
            if (gridX >= 0 && gridX < ViewModel.Map.Width && gridY >= 0 && gridY < ViewModel.Map.Height)
            {
                ViewModel.MedicalService.AddMedicalPoint(gridX, gridY);
                _pendingMedicalPointPlacement = null;
            }
            else
            {
                MessageBox.Show("Невозможно разместить больницу за пределами карты.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Residential Building Visualization

        private Point? _pendingBuildingPlacement;

        private void InitializeResidentialVisuals()
        {
            if (ViewModel?.ResidentialService == null) return;

            // Подписываемся на изменения коллекции домов
            ViewModel.ResidentialService.Buildings.CollectionChanged += Buildings_CollectionChanged;

            // Создаем визуальные элементы для существующих домов
            foreach (var building in ViewModel.ResidentialService.Buildings)
            {
                CreateBuildingVisual(building);
            }
        }

        private void Buildings_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (e.NewItems != null)
                {
                    foreach (ResidentialBuilding building in e.NewItems)
                    {
                        CreateBuildingVisual(building);
                        building.PropertyChanged += Building_PropertyChanged;
                    }
                }

                if (e.OldItems != null)
                {
                    foreach (ResidentialBuilding building in e.OldItems)
                    {
                        RemoveBuildingVisual(building);
                        building.PropertyChanged -= Building_PropertyChanged;
                    }
                }
            }), DispatcherPriority.Normal);
        }

        private void CreateBuildingVisual(ResidentialBuilding building)
        {
            // Создаем контейнер для дома
            var container = new Canvas();
            
            // Серый или коричневый прямоугольник в зависимости от размера
            var color = building.MaxCapacity <= 2 
                ? Color.FromRgb(139, 90, 43)  // Коричневый для маленьких домов
                : Color.FromRgb(105, 105, 105); // Серый для больших домов

            var rectangle = new Rectangle
            {
                Width = building.Width * 15.0,
                Height = building.Height * 15.0,
                Fill = new SolidColorBrush(color),
                Stroke = new SolidColorBrush(Colors.DarkGray),
                StrokeThickness = 1
            };
            Canvas.SetLeft(rectangle, -building.Width * 15.0 / 2.0);
            Canvas.SetTop(rectangle, -building.Height * 15.0 / 2.0);
            container.Children.Add(rectangle);

            // Текст с количеством жителей
            var residentText = new TextBlock
            {
                Text = building.OccupancyText,
                FontSize = 8,
                Foreground = new SolidColorBrush(Colors.White),
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Canvas.SetLeft(residentText, -building.Width * 15.0 / 2.0);
            Canvas.SetTop(residentText, building.Height * 15.0 / 2.0 + 2);
            residentText.Width = building.Width * 15.0;
            residentText.TextAlignment = TextAlignment.Center;
            residentText.Name = "ResidentCountText";
            container.Children.Add(residentText);

            // Красная точка для больных жителей
            UpdateSickIndicator(container, building);

            Canvas.SetLeft(container, building.XCoordinate);
            Canvas.SetTop(container, building.YCoordinate);
            container.ToolTip = $"{building.Name}\n{building.OccupancyText}";
            Panel.SetZIndex(container, 8);

            MapCanvas.Children.Add(container);
            _buildingVisuals[building] = container;

            // Подписываемся на изменения коллекции жителей
            building.Residents.CollectionChanged += (s, e) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateSickIndicator(container, building);
                    var textBlock = container.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Name == "ResidentCountText");
                    if (textBlock != null)
                    {
                        textBlock.Text = building.OccupancyText;
                    }
                    container.ToolTip = $"{building.Name}\n{building.OccupancyText}";
                }), DispatcherPriority.Normal);
            };
        }

        private void UpdateSickIndicator(Canvas container, ResidentialBuilding building)
        {
            // Удаляем старый индикатор
            var oldIndicator = container.Children.OfType<Ellipse>().FirstOrDefault();
            if (oldIndicator != null)
            {
                container.Children.Remove(oldIndicator);
            }

            // Добавляем красную точку, если есть больной житель
            if (building.HasSickResident)
            {
                var indicator = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(Colors.Red),
                    Stroke = new SolidColorBrush(Colors.DarkRed),
                    StrokeThickness = 1
                };
                Canvas.SetLeft(indicator, building.Width * 15.0 / 2.0 - 4);
                Canvas.SetTop(indicator, -building.Height * 15.0 / 2.0 - 4);
                Panel.SetZIndex(indicator, 20);
                container.Children.Add(indicator);
            }
        }

        private void RemoveBuildingVisual(ResidentialBuilding building)
        {
            if (_buildingVisuals.TryGetValue(building, out var visual))
            {
                MapCanvas.Children.Remove(visual);
                _buildingVisuals.Remove(building);
            }
        }

        private void Building_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is ResidentialBuilding building)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_buildingVisuals.TryGetValue(building, out var container))
                    {
                        if (e.PropertyName == nameof(ResidentialBuilding.XCoordinate) || 
                            e.PropertyName == nameof(ResidentialBuilding.YCoordinate))
                        {
                            Canvas.SetLeft(container, building.XCoordinate);
                            Canvas.SetTop(container, building.YCoordinate);
                        }
                        else if (e.PropertyName == nameof(ResidentialBuilding.HasSickResident) ||
                                 e.PropertyName == nameof(ResidentialBuilding.OccupancyText))
                        {
                            UpdateSickIndicator(container, building);
                            var textBlock = container.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Name == "ResidentCountText");
                            if (textBlock != null)
                            {
                                textBlock.Text = building.OccupancyText;
                            }
                            container.ToolTip = $"{building.Name}\n{building.OccupancyText}";
                        }
                    }
                }), DispatcherPriority.Normal);
            }
        }

        private void PlaceResidentialBuilding_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!_pendingBuildingPlacement.HasValue)
            {
                var point = Mouse.GetPosition(MapCanvas);
                var gridPoint = CanvasToGrid(point);
                _pendingBuildingPlacement = gridPoint;
            }

            int gridX = (int)_pendingBuildingPlacement.Value.X;
            int gridY = (int)_pendingBuildingPlacement.Value.Y;

            if (gridX >= 0 && gridX < ViewModel.Map.Width && gridY >= 0 && gridY < ViewModel.Map.Height)
            {
                // Размер дома: 1x1, 2x1, 1x2 или 2x2 (случайно)
                int width = _random.Next(1, 3);
                int height = _random.Next(1, 3);
                ViewModel.ResidentialService.AddResidentialBuilding(gridX, gridY, width, height);
                _pendingBuildingPlacement = null;
            }
            else
            {
                MessageBox.Show("Невозможно разместить дом за пределами карты.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu contextMenu)
            {
                foreach (var item in contextMenu.Items)
                {
                    if (item is MenuItem menuItem && menuItem.DataContext is BuildMenuEntry entry)
                    {
                        if (!string.IsNullOrEmpty(entry.ClickHandler))
                        {
                            menuItem.Click -= BuildMenu_ItemClick; // Убираем предыдущие подписки
                            menuItem.Click += BuildMenu_ItemClick;
                        }
                    }
                }
            }
        }

        private void BuildMenu_ItemClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is BuildMenuEntry entry)
            {
                if (!string.IsNullOrEmpty(entry.ClickHandler))
                {
                    switch (entry.ClickHandler)
                    {
                        case "PlaceMedicalPoint":
                            PlaceMedicalPoint_MenuItem_Click(sender, e);
                            break;
                        case "PlaceResidentialBuilding":
                            PlaceResidentialBuilding_MenuItem_Click(sender, e);
                            break;
                    }
                }
            }
        }
    }
}