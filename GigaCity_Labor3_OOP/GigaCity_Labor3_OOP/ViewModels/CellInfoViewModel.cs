using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using GigaCity_Labor3_OOP.Models;

namespace GigaCity_Labor3_OOP.ViewModels
{
    public class CellInfoViewModel : INotifyPropertyChanged
    {
        private CellViewModel _cell;
        private RoadViewModel _road;
        private ObservableCollection<VehicleViewModel> _vehicles;

        public CellViewModel Cell
        {
            get => _cell;
            set
            {
                _cell = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Coordinates));
                OnPropertyChanged(nameof(TerrainType));
                OnPropertyChanged(nameof(ResourceType));
            }
        }

        public RoadViewModel Road
        {
            get => _road;
            set
            {
                _road = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RoadInfo)); 
            }
        }

        public ObservableCollection<VehicleViewModel> Vehicles
        {
            get => _vehicles;
            set
            {
                _vehicles = value;
                OnPropertyChanged();
            }
        }

        public string Coordinates => Cell != null ? $"[{Cell.X}, {Cell.Y}]" : "";
        public string TerrainType => Cell != null ? GetTerrainName(Cell.TerrainType) : "";
        public string ResourceType => Cell != null ? GetResourceName(Cell.ResourceType) : "";

        public string RoadInfo => Road != null ? $"Дорога (Скорость: {Road.SpeedLimit} км/ч)" : "Нет дороги";

        public CellInfoViewModel()
        {
            Vehicles = new ObservableCollection<VehicleViewModel>();
        }

        public void UpdateInfo(CellViewModel cell, RoadViewModel road, TrafficManagementViewModel trafficViewModel)
        {
            // ПРОВЕРКА 1: Вызывается ли метод вообще?
            System.Diagnostics.Debug.WriteLine($"--- CellInfoViewModel.UpdateInfo вызван для клетки ({cell?.X}, {cell?.Y}) ---");

            // ПРОВЕРКА 2: Не являются ли входные данные null
            System.Diagnostics.Debug.WriteLine($"Параметр 'road' равен null: {road == null}");
            System.Diagnostics.Debug.WriteLine($"Параметр 'trafficViewModel' равен null: {trafficViewModel == null}");

            if (trafficViewModel != null)
            {
                // ПРОВЕРКА 3: Сколько всего транспорта в системе?
                System.Diagnostics.Debug.WriteLine($"Всего транспорта в TrafficManagementViewModel.Vehicles: {trafficViewModel.Vehicles.Count}");
            }

            // Устанавливаем свойства (это должно вызвать обновление UI)
            Cell = cell;
            Road = road;

            // Очищаем и заполняем локальную коллекцию транспорта
            Vehicles.Clear();

            if (cell != null && trafficViewModel != null)
            {
                // ПРОВЕРКА 4: Находим ли мы транспорт в этой клетке?
                var vehiclesInCell = trafficViewModel.Vehicles
                    .Where(v => v.CurrentX == cell.X && v.CurrentY == cell.Y)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Найдено транспорта в клетке ({cell.X}, {cell.Y}): {vehiclesInCell.Count}");

                foreach (var vehicle in vehiclesInCell)
                {
                    Vehicles.Add(vehicle);
                }
            }

            // ПРОВЕРКА 5: Сколько транспорта оказалось в локальной коллекции?
            System.Diagnostics.Debug.WriteLine($"В локальной коллекции Vehicles теперь: {Vehicles.Count} элементов.");
            System.Diagnostics.Debug.WriteLine("----------------------------------------------------");
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
                6 => "Учебное заведение",
                7 => "Аэропорт",
                8 => "Порт",
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}