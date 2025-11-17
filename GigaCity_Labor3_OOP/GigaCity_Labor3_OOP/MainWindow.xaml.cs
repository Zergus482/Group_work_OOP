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
using GigaCity_Labor3_OOP.Models;
using GigaCity_Labor3_OOP.ViewModels;
using TheFinancialSystem;
using TheFinancialSystem.ViewModels;
using System.Linq;
using GigaCity_Labor3_OOP.Views;

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
                string tooltip = cell.ToolTip;
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
    }
}