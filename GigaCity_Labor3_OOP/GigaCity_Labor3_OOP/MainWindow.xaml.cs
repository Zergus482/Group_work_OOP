using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.ObjectModel;
using GigaCity_Labor3_OOP.Models;
using GigaCity_Labor3_OOP.ViewModels;
using TheFinancialSystem;
using TheFinancialSystem.ViewModels;
using System.Linq;
using GigaCity_Labor3_OOP.Views;
using GigacityContracts;
using CommsModel;
using EnergyModel;

namespace GigaCity_Labor3_OOP
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; private set; }

        private Point _lastMousePosition;
        private bool _isPanning = false;

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
        
        // Ссылки на модули для визуализации
        private EnergyModel.MainWindow? _energyWindow;
        private CommsModel.MainWindow? _commsWindow;
        private readonly Dictionary<(int x, int y), Rectangle> _cellRectangles = new Dictionary<(int x, int y), Rectangle>();
        
        // Режимы визуализации
        private bool _showEnergyOverlay = false;
        private bool _showCommsOverlay = false;

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

                    if (ViewModel.Map.IsRoad(cell.X, cell.Y))
                    {
                        fillColor = _roadColor;
                    }
                    else if (ViewModel.Map.IsPark(cell.X, cell.Y))
                    {
                        fillColor = _parkColor;
                    }
                    else if (ViewModel.Map.IsBikePath(cell.X, cell.Y))
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
                string tooltip = cell.ToolTip ?? "";
                if (ViewModel.Map.IsPark(cell.X, cell.Y))
                {
                    tooltip += "\nЗона: Парк";
                }
                else if (ViewModel.Map.IsBikePath(cell.X, cell.Y))
                {
                    tooltip += "\nЗона: Велодорожка";
                }
                else if (ViewModel.Map.IsRoad(cell.X, cell.Y))
                {
                    tooltip += "\nЗона: Дорога";
                }
                
                // Добавляем информацию об энергетике
                var energyService = ViewModel.GetEnergyService();
                if (energyService != null)
                {
                    var energyData = energyService.GetCellData(cell.X, cell.Y);
                    if (energyData != null)
                    {
                        tooltip += $"\n\n⚡ Энергетика:";
                        tooltip += $"\n  Генерация: {energyData.Generation:F2} МВт";
                        tooltip += $"\n  Потребление: {energyData.Consumption:F2} МВт";
                        tooltip += $"\n  Баланс: {energyData.NetEnergy:F2} МВт";
                        tooltip += $"\n  Статус: {GetEnergyStatusName(energyData.Status)}";
                    }
                }
                
                // Добавляем информацию о коммуникациях
                var commsService = ViewModel.GetCommsService();
                if (commsService != null)
                {
                    var commsData = commsService.GetCellData(cell.X, cell.Y);
                    if (commsData != null)
                    {
                        tooltip += $"\n\n📡 Коммуникации:";
                        tooltip += $"\n  Сотовая связь: {commsData.CellularCoverage:F1}%";
                        tooltip += $"\n  Интернет: {commsData.InternetCoverage:F1}%";
                        tooltip += $"\n  Задержка: {commsData.Latency} мс";
                        tooltip += $"\n  Пропускная способность: {commsData.Bandwidth:F1} Мбит/с";
                        tooltip += $"\n  Статус: {GetCommStatusName(commsData.Status)}";
                        
                        // Проверяем, есть ли вышка в этой ячейке
                        var tower = commsService.GetTowers().FirstOrDefault(t => t.X == cell.X && t.Y == cell.Y);
                        if (tower != null)
                        {
                            string towerType = tower.Type == CommsModel.TowerType.Cellular ? "Мобильная вышка" : "Интернет-провайдер";
                            tooltip += $"\n  🗼 {towerType} (радиус: {tower.Range:F1}, нагрузка: {tower.CurrentLoad}/{tower.Capacity})";
                        }
                    }
                }
                
                rectangle.ToolTip = tooltip;

                // Добавляем в ItemsControl
                MapItemsControl.Items.Add(rectangle);
                
                // Сохраняем ссылку на прямоугольник для обновления
                _cellRectangles[(cell.X, cell.Y)] = rectangle;
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
                // Обновляем tooltip при изменении года
                UpdateCellTooltips();
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
        private void MapScrollViewer_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                _lastMousePosition = e.GetPosition(MapScrollViewer);
                MapScrollViewer.Cursor = Cursors.SizeAll;
                MapScrollViewer.CaptureMouse();
                e.Handled = true;
            }
        }

        private void MapScrollViewer_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
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
            if (_isPanning && e.RightButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(MapScrollViewer);
                Vector delta = currentPosition - _lastMousePosition;

                MapScrollViewer.ScrollToHorizontalOffset(MapScrollViewer.HorizontalOffset - delta.X);
                MapScrollViewer.ScrollToVerticalOffset(MapScrollViewer.VerticalOffset - delta.Y);

                _lastMousePosition = currentPosition;
                e.Handled = true;
            }
        }

        #endregion

        private void Rectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Rectangle rectangle && rectangle.DataContext is CellViewModel cell)
            {
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
        
        private string GetEnergyStatusName(EnergyModel.EnergyStatus status)
        {
            return status switch
            {
                EnergyModel.EnergyStatus.Critical => "Критический дефицит",
                EnergyModel.EnergyStatus.Deficient => "Дефицит",
                EnergyModel.EnergyStatus.Normal => "Норма",
                EnergyModel.EnergyStatus.Surplus => "Избыток",
                _ => "Неизвестно"
            };
        }
        
        private string GetCommStatusName(CommsModel.CommStatus status)
        {
            return status switch
            {
                CommsModel.CommStatus.NoCoverage => "Нет покрытия",
                CommsModel.CommStatus.Poor => "Плохое покрытие",
                CommsModel.CommStatus.Slow => "Медленное соединение",
                CommsModel.CommStatus.Good => "Хорошее соединение",
                _ => "Неизвестно"
            };
        }
        
        private void UpdateCellTooltips()
        {
            foreach (var cell in ViewModel.Map.Cells)
            {
                if (_cellRectangles.TryGetValue((cell.X, cell.Y), out var rectangle))
                {
                    string tooltip = cell.ToolTip ?? "";
                    if (ViewModel.Map.IsPark(cell.X, cell.Y))
                    {
                        tooltip += "\nЗона: Парк";
                    }
                    else if (ViewModel.Map.IsBikePath(cell.X, cell.Y))
                    {
                        tooltip += "\nЗона: Велодорожка";
                    }
                    else if (ViewModel.Map.IsRoad(cell.X, cell.Y))
                    {
                        tooltip += "\nЗона: Дорога";
                    }
                    
                    // Добавляем информацию об энергетике
                    var energyService = ViewModel.GetEnergyService();
                    if (energyService != null)
                    {
                        var energyData = energyService.GetCellData(cell.X, cell.Y);
                        if (energyData != null)
                        {
                            tooltip += $"\n\n⚡ Энергетика:";
                            tooltip += $"\n  Генерация: {energyData.Generation:F2} МВт";
                            tooltip += $"\n  Потребление: {energyData.Consumption:F2} МВт";
                            tooltip += $"\n  Баланс: {energyData.NetEnergy:F2} МВт";
                            tooltip += $"\n  Статус: {GetEnergyStatusName(energyData.Status)}";
                        }
                    }
                    
                    // Добавляем информацию о коммуникациях
                    var commsService = ViewModel.GetCommsService();
                    if (commsService != null)
                    {
                        var commsData = commsService.GetCellData(cell.X, cell.Y);
                        if (commsData != null)
                        {
                            tooltip += $"\n\n📡 Коммуникации:";
                            tooltip += $"\n  Сотовая связь: {commsData.CellularCoverage:F1}%";
                            tooltip += $"\n  Интернет: {commsData.InternetCoverage:F1}%";
                            tooltip += $"\n  Задержка: {commsData.Latency} мс";
                            tooltip += $"\n  Пропускная способность: {commsData.Bandwidth:F1} Мбит/с";
                            tooltip += $"\n  Статус: {GetCommStatusName(commsData.Status)}";
                            
                            // Проверяем, есть ли вышка в этой ячейке
                            var tower = commsService.GetTowers().FirstOrDefault(t => t.X == cell.X && t.Y == cell.Y);
                            if (tower != null)
                            {
                                string towerType = tower.Type == CommsModel.TowerType.Cellular ? "Мобильная вышка" : "Интернет-провайдер";
                                tooltip += $"\n  🗼 {towerType} (радиус: {tower.Range:F1}, нагрузка: {tower.CurrentLoad}/{tower.Capacity})";
                            }
                        }
                    }
                    
                    rectangle.ToolTip = tooltip;
                }
            }
        }
        
        private void ShowEnergyOverlay_Checked(object sender, RoutedEventArgs e)
        {
            _showEnergyOverlay = true;
            if (_energyWindow?.GetSimulationService() != null)
            {
                UpdateEnergyVisualization(_energyWindow.GetSimulationService());
            }
        }
        
        private void ShowEnergyOverlay_Unchecked(object sender, RoutedEventArgs e)
        {
            _showEnergyOverlay = false;
            // Восстанавливаем исходные цвета - перерисовываем карту
            RefreshMapColors();
        }
        
        private void ShowCommsOverlay_Checked(object sender, RoutedEventArgs e)
        {
            _showCommsOverlay = true;
            if (_commsWindow?.GetSimulationService() != null)
            {
                UpdateCommsVisualization(_commsWindow.GetSimulationService());
            }
        }
        
        private void ShowCommsOverlay_Unchecked(object sender, RoutedEventArgs e)
        {
            _showCommsOverlay = false;
            // Восстанавливаем исходные цвета - перерисовываем карту
            RefreshMapColors();
        }
        
        private void RefreshMapColors()
        {
            // Восстанавливаем исходные цвета ячеек
            foreach (var cell in ViewModel.Map.Cells)
            {
                if (_cellRectangles.TryGetValue((cell.X, cell.Y), out var rectangle))
                {
                    Color fillColor;
                    
                    if (_terrainColors.TryGetValue(cell.TerrainType, out var color))
                    {
                        if (ViewModel.Map.IsRoad(cell.X, cell.Y))
                        {
                            fillColor = _roadColor;
                        }
                        else if (ViewModel.Map.IsPark(cell.X, cell.Y))
                        {
                            fillColor = _parkColor;
                        }
                        else if (ViewModel.Map.IsBikePath(cell.X, cell.Y))
                        {
                            fillColor = _bikePathColor;
                        }
                        else
                        {
                            fillColor = color;
                        }
                    }
                    else
                    {
                        fillColor = Colors.Gray;
                    }
                    
                    rectangle.Fill = new SolidColorBrush(fillColor);
                    
                    // Восстанавливаем границы
                    if (cell.TerrainType == 7) // Airport
                    {
                        rectangle.Stroke = new SolidColorBrush(Colors.White);
                        rectangle.StrokeThickness = 2;
                    }
                    else if (cell.TerrainType == 8) // Port
                    {
                        rectangle.Stroke = new SolidColorBrush(Colors.Yellow);
                        rectangle.StrokeThickness = 2;
                    }
                    else
                    {
                        rectangle.Stroke = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                        rectangle.StrokeThickness = 0.8;
                    }
                }
            }
        }

        private void OpenEnergyModule_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_energyWindow == null || !_energyWindow.IsVisible)
                {
                    _energyWindow = new EnergyModel.MainWindow();
                    
                    // Преобразуем CellViewModel в ICell через CellAdapter
                    var cells = new ObservableCollection<ICell>();
                    foreach (var cell in ViewModel.Map.Cells)
                    {
                        cells.Add(new CellAdapter(cell));
                    }
                    
                    _energyWindow.Initialize(cells);
                    _energyWindow.Owner = this;
                    _energyWindow.EnergyDataUpdated += EnergyWindow_EnergyDataUpdated;
                    _energyWindow.Closed += (s, args) => { _energyWindow = null; _showEnergyOverlay = false; };
                    
                    // Инициализируем сервис в ViewModel и запускаем автосимуляцию
                    var energyService = _energyWindow.GetSimulationService();
                    if (energyService != null)
                    {
                        ViewModel.InitializeEnergyService(energyService);
                        // Автозапуск симуляции
                        _energyWindow.StartAutoSimulation();
                    }
                    
                    _energyWindow.Show();
                }
                else
                {
                    _energyWindow.Activate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть энергетическую систему: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void EnergyWindow_EnergyDataUpdated(object? sender, EnergyModel.EnergyDataUpdatedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_showEnergyOverlay)
                {
                    UpdateEnergyVisualization(e.Service);
                }
                UpdateCellTooltips();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        
        private void UpdateEnergyVisualization(EnergyModel.EnergySimulationService service)
        {
            foreach (var cellData in service.GetAllCells())
            {
                if (_cellRectangles.TryGetValue((cellData.X, cellData.Y), out var rectangle))
                {
                    Color energyColor = cellData.Status switch
                    {
                        EnergyModel.EnergyStatus.Critical => Color.FromRgb(139, 0, 0),    // Темно-красный
                        EnergyModel.EnergyStatus.Deficient => Color.FromRgb(255, 69, 0),   // Оранжево-красный
                        EnergyModel.EnergyStatus.Normal => Colors.Transparent,              // Прозрачный (базовый цвет)
                        EnergyModel.EnergyStatus.Surplus => Color.FromRgb(0, 200, 0),      // Зеленый
                        _ => Colors.Transparent
                    };
                    
                    // Применяем цвет как overlay (полупрозрачный)
                    if (energyColor != Colors.Transparent)
                    {
                        var baseBrush = rectangle.Fill as SolidColorBrush;
                        if (baseBrush != null)
                        {
                            var baseColor = baseBrush.Color;
                            var blendedColor = BlendColors(baseColor, energyColor, 0.4);
                            rectangle.Fill = new SolidColorBrush(blendedColor);
                        }
                    }
                }
            }
        }
        
        private Color BlendColors(Color baseColor, Color overlayColor, double opacity)
        {
            return Color.FromArgb(
                255,
                (byte)(baseColor.R * (1 - opacity) + overlayColor.R * opacity),
                (byte)(baseColor.G * (1 - opacity) + overlayColor.G * opacity),
                (byte)(baseColor.B * (1 - opacity) + overlayColor.B * opacity)
            );
        }

        private void OpenCommsModule_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_commsWindow == null || !_commsWindow.IsVisible)
                {
                    _commsWindow = new CommsModel.MainWindow();
                    
                    // Преобразуем CellViewModel в ICell через CellAdapter
                    var cells = new ObservableCollection<ICell>();
                    foreach (var cell in ViewModel.Map.Cells)
                    {
                        cells.Add(new CellAdapter(cell));
                    }
                    
                    _commsWindow.Initialize(cells);
                    _commsWindow.Owner = this;
                    _commsWindow.CommDataUpdated += CommsWindow_CommDataUpdated;
                    _commsWindow.Closed += (s, args) => { _commsWindow = null; _showCommsOverlay = false; };
                    
                    // Инициализируем сервис в ViewModel и запускаем автосимуляцию
                    var commsService = _commsWindow.GetSimulationService();
                    if (commsService != null)
                    {
                        ViewModel.InitializeCommsService(commsService);
                        // Автозапуск симуляции
                        _commsWindow.StartAutoSimulation();
                    }
                    
                    _commsWindow.Show();
                }
                else
                {
                    _commsWindow.Activate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть коммуникационную систему: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void CommsWindow_CommDataUpdated(object? sender, CommsModel.CommDataUpdatedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_showCommsOverlay)
                {
                    UpdateCommsVisualization(e.Service);
                }
                UpdateCellTooltips();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        
        private void UpdateCommsVisualization(CommsModel.CommunicationsSimulationService service)
        {
            // Визуализируем вышки связи
            foreach (var tower in service.GetTowers())
            {
                // Рисуем радиус покрытия (упрощенно - только центральная ячейка)
                if (_cellRectangles.TryGetValue((tower.X, tower.Y), out var towerRect))
                {
                    towerRect.Stroke = new SolidColorBrush(tower.Type == CommsModel.TowerType.Cellular 
                        ? Colors.Cyan 
                        : Colors.Magenta);
                    towerRect.StrokeThickness = 2;
                }
            }
            
            // Визуализируем покрытие для каждой ячейки
            foreach (var cellData in service.GetAllCells())
            {
                if (_cellRectangles.TryGetValue((cellData.X, cellData.Y), out var rectangle))
                {
                    Color commsColor = cellData.Status switch
                    {
                        CommsModel.CommStatus.NoCoverage => Color.FromRgb(80, 80, 80),    // Серый
                        CommsModel.CommStatus.Poor => Color.FromRgb(255, 165, 0),         // Оранжевый
                        CommsModel.CommStatus.Slow => Color.FromRgb(255, 215, 0),         // Золотой
                        CommsModel.CommStatus.Good => Color.FromRgb(0, 191, 255),         // Голубой
                        _ => Colors.Transparent
                    };
                    
                    // Применяем цвет как overlay
                    if (commsColor != Colors.Transparent)
                    {
                        var baseBrush = rectangle.Fill as SolidColorBrush;
                        if (baseBrush != null)
                        {
                            var baseColor = baseBrush.Color;
                            var blendedColor = BlendColors(baseColor, commsColor, 0.3);
                            rectangle.Fill = new SolidColorBrush(blendedColor);
                        }
                    }
                }
            }
        }
    }
}