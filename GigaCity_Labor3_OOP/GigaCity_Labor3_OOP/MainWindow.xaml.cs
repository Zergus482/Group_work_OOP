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
using GigaCity_Labor3_OOP.ViewModels;
using GigaCity_Labor3_OOP.ViewModels.Economy;
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
        private void OpenSecondAppButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем путь к исполняемому файлу второго проекта.
                // Замените "SecondProjectName.exe" на реальное имя вашего .exe файла.
                string pathToExe = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, 
                    "CitySimulation.exe" 
                );

                // Проверяем, существует ли файл
                if (!System.IO.File.Exists(pathToExe))
                {
                    MessageBox.Show($"Не найден исполняемый файл по пути: {pathToExe}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Создаем и запускаем новый процесс
                Process.Start(pathToExe);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось запустить приложение: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            }
            else
            {
                ViewModel.EconomySimulation.SetPlacementCell((int)gridPoint.X, (int)gridPoint.Y);
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

        private void OpenTrafficManagementButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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
    }
}