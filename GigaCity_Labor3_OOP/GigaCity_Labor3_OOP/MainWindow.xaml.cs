using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.ComponentModel;
using GigaCity_Labor3_OOP.Models;
using GigaCity_Labor3_OOP.ViewModels;
using TheFinancialSystem;
using TheFinancialSystem.ViewModels;

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
            { 5, Color.FromRgb(105, 105, 105) }  // City
        };

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            DataContext = ViewModel;

            // Подписываемся на изменение свойств
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            // Инициализируем карту
            InitializeMap();

            // Обновляем тексты
            UpdateYearText();
            UpdatePopulationText();

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
                    rectangle.Fill = new SolidColorBrush(color);
                    rectangle.Stroke = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                    rectangle.StrokeThickness = 0.8; // Немного увеличили толщину границы
                }

                // Добавляем обработчик события
                rectangle.MouseEnter += Rectangle_MouseEnter;

                // Добавляем в ItemsControl
                MapItemsControl.Items.Add(rectangle);
            }

            // Увеличиваем размер канваса пропорционально
            MapCanvas.Width = 1500; // Было 1000, стало 1500
            MapCanvas.Height = 1500; // Было 1000, стало 1500
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.SelectedCell))
            {
                UpdateSelectedCellInfo();
            }
            else if (e.PropertyName == nameof(ViewModel.CurrentYear))
            {
                UpdateYearText();
            }
            else if (e.PropertyName == nameof(ViewModel.Population))
            {
                UpdatePopulationText();
            }
        }
        

        private void UpdateSelectedCellInfo()
        {
            if (ViewModel.SelectedCell != null)
            {
                PositionXText.Text = $"X: {ViewModel.SelectedCell.X}";
                PositionYText.Text = $"Y: {ViewModel.SelectedCell.Y}";
                TerrainTypeText.Text = GetTerrainName(ViewModel.SelectedCell.TerrainType);
                ResourceTypeText.Text = GetResourceName(ViewModel.SelectedCell.ResourceType);
            }
        }

        private void UpdateYearText()
        {
            YearText.Text = $"Год: {ViewModel.CurrentYear}";
        }

        private void UpdatePopulationText()
        {
            PopulationText.Text = $"Население: {ViewModel.Population}";
        }

        private string GetTerrainName(byte terrainType)
        {
            return terrainType switch
            {
                1 => "Поляна",
                2 => "Лес",
                3 => "Горы",
                4 => "Водоем",
                5 => "Город",
                _ => "Неизвестно"
            };
        }

        private string GetResourceName(byte resourceType)
        {
            return resourceType switch
            {
                0 => "Нет ресурсов",
                1 => "Металлы",
                2 => "Нефть",
                3 => "Газ",
                4 => "Деревья",
                5 => "Растения",
                _ => "Неизвестно"
            };
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
            Loaded -= MainWindow_Loaded;
            base.OnClosed(e);
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
    }
}