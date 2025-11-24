using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using GigaCity_Labor3_OOP.Models;
using TheFinancialSystem;
using TheFinancialSystem.ViewModels;
using GigaCity_Labor3_OOP.Services;
using GigaCity_Labor3_OOP.ViewModels;
using GigaCity_Labor3_OOP.ViewModels.Economy;

namespace GigaCity_Labor3_OOP.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MapModel Map { get; private set; }
        public PopulationManager PopulationManager { get; private set; }

        // Финансовая система
        public FinancialOverviewViewModel FinancialOverview { get; private set; }
        private Budget _cityBudget;
        private Tax _taxPolicy;

        // Система самолетов
        public ObservableCollection<Plane> Planes { get; private set; }
        private DispatcherTimer _planeTimer;
        private bool _isPlaneDirectionToAirport2 = true; // Направление: true = из аэропорта 1 в аэропорт 2, false = из аэропорта 2 в аэропорт 1

        // Система кораблей
        public ObservableCollection<Ship> Ships { get; private set; }
        private DispatcherTimer _shipTimer;
        private bool _isShipDirectionToPort2 = true; // Направление: true = из порта 1 в порт 2, false = из порта 2 в порт 1

        //Дороги и транспорт
        private TrafficSimulationService _trafficSimulationService;
        private PathFindingService _pathFindingService;
        private TrafficManagementViewModel _trafficManagementViewModel;
        private CellInfoViewModel _cellInfoViewModel;
        public EconomySimulationViewModel EconomySimulation { get; }

        // Пожарные службы
        public EmergencyServiceViewModel EmergencyService { get; private set; }

        // Медицинские службы
        public MedicalServiceViewModel MedicalService { get; private set; }

        // Жилые дома
        public ResidentialServiceViewModel ResidentialService { get; private set; }

        // Внешние связи
        public ForeignRelationsViewModel ForeignRelations { get; private set; }

        public string EmployeesStats => $"Работают: {PopulationManager.University.GetEmployeeCount()}/100";

        public TrafficManagementViewModel TrafficManagementViewModel
        {
            get => _trafficManagementViewModel;
            set { _trafficManagementViewModel = value; OnPropertyChanged(); }
        }

        public CellInfoViewModel CellInfoViewModel
        {
            get => _cellInfoViewModel;
            set { _cellInfoViewModel = value; OnPropertyChanged(); }
        }

        private CellViewModel _selectedCell;
        public CellViewModel SelectedCell
        {
            get => _selectedCell;
            set
            {
                _selectedCell = value;
                OnPropertyChanged();
            }
        }

        private int _population = 0;
        public int Population
        {
            get => _population;
            set
            {
                _population = value;
                OnPropertyChanged();
            }
        }

        private int _currentYear = 0;
        public int CurrentYear
        {
            get => _currentYear;
            set
            {
                _currentYear = value;
                OnPropertyChanged();
            }
        }

        public string UniversityStats
        {
            get
            {
                var uni = PopulationManager.University;
                int totalStudents = uni.GetSchoolStudents().Count + uni.GetCollegeStudents().Count + uni.GetUniversityStudents().Count;

                return $"Студентов: {totalStudents} (Школа: {uni.GetSchoolStudents().Count}/{uni.SchoolCapacity} | Колледж: {uni.GetCollegeStudents().Count}/{uni.CollegeCapacity} | Университет: {uni.GetUniversityStudents().Count}/{uni.UniversityCapacity})";
            }
        }

        public MainViewModel()
        {
            Map = new MapModel();
            PopulationManager = new PopulationManager();
            Population = PopulationManager.PopulationCount;
            EconomySimulation = new EconomySimulationViewModel(Map.RoadCoordinates, Map.Width, Map.Height, 15, Map);

            // Инициализация финансовой системы
            InitializeFinancialSystem();

            // Инициализация системы самолетов
            Planes = new ObservableCollection<Plane>();
            InitializePlaneSystem();

            // Инициализация системы кораблей
            Ships = new ObservableCollection<Ship>();
            InitializeShipSystem();

            //Инициализация транспорта
            _trafficSimulationService = new TrafficSimulationService(Map.RoadCoordinates);

            _trafficManagementViewModel = new TrafficManagementViewModel(_trafficSimulationService, Map.RoadCoordinates);

            _cellInfoViewModel = new CellInfoViewModel();

            SelectedCell = Map.Cells.FirstOrDefault();

            PopulateInitialTraffic();

            // Инициализация пожарных служб
            EmergencyService = new EmergencyServiceViewModel(Map);

            // Инициализация медицинских служб
            MedicalService = new MedicalServiceViewModel(Map);

            // Инициализация жилых домов
            ResidentialService = new ResidentialServiceViewModel(Map);
            ResidentialService.SetMedicalService(MedicalService);
            MedicalService.SetResidentialService(ResidentialService);

            // Инициализация внешних связей
            ForeignRelations = new ForeignRelationsViewModel(PopulationManager);
        }

        private void InitializePlaneSystem()
        {
            // Создаем таймер для запуска самолетов каждые 30 секунд
            _planeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _planeTimer.Tick += PlaneTimer_Tick;
            _planeTimer.Start();

            // Создаем таймер для обновления позиций самолетов (анимация)
            var animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(30) // Обновление каждые 30мс для более плавной анимации
            };
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();

            // Запускаем первый самолет с небольшой задержкой, чтобы UI успел загрузиться
            var startTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            startTimer.Tick += (s, e) =>
            {
                startTimer.Stop();
                CreateNewPlane();
            };
            startTimer.Start();
        }

        private void PlaneTimer_Tick(object sender, EventArgs e)
        {
            // Создаем новый самолет
            CreateNewPlane();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            // Обновляем позиции всех активных самолетов
            var planesToRemove = new System.Collections.Generic.List<Plane>();
            
            foreach (var plane in Planes)
            {
                plane.Update();
                
                // Удаляем неактивные самолеты
                if (!plane.IsActive)
                {
                    planesToRemove.Add(plane);
                }
            }

            // Удаляем неактивные самолеты
            foreach (var plane in planesToRemove)
            {
                Planes.Remove(plane);
            }
        }

        private void CreateNewPlane()
        {
            int fromX, fromY, toX, toY;

            if (_isPlaneDirectionToAirport2)
            {
                // Летим из аэропорта 1 в аэропорт 2
                // Поменяли местами X и Y для точки отправления
                fromX = Map.Airport1Y;  
                fromY = Map.Airport1X;  
                toX = Map.Airport2X;
                toY = Map.Airport2Y;
            }
            else
            {
                // Летим из аэропорта 2 в аэропорт 1
                fromX = Map.Airport2X;
                fromY = Map.Airport2Y;
                // Поменяли местами X и Y для точки назначения
                toX = Map.Airport1Y;  // Было Airport1X, стало Airport1Y
                toY = Map.Airport1X;  // Было Airport1Y, стало Airport1X
            }

            var plane = new Plane(fromX, fromY, toX, toY);
            
            // Отладочный вывод
            string direction = _isPlaneDirectionToAirport2 ? "из аэропорта 1 в аэропорт 2" : "из аэропорта 2 в аэропорт 1";
            System.Diagnostics.Debug.WriteLine($"Создание самолета: {direction} [{fromX},{fromY}] -> [{toX},{toY}], позиция: X={plane.X}, Y={plane.Y}");
            
            Planes.Add(plane);

            // Меняем направление для следующего самолета
            _isPlaneDirectionToAirport2 = !_isPlaneDirectionToAirport2;
        }

        private void InitializeShipSystem()
        {
            // Создаем таймер для запуска кораблей каждые 45 секунд
            _shipTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(45)
            };
            _shipTimer.Tick += ShipTimer_Tick;
            _shipTimer.Start();

            // Создаем таймер для обновления позиций кораблей (анимация)
            var animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(30) // Обновление каждые 30мс для плавной анимации
            };
            animationTimer.Tick += ShipAnimationTimer_Tick;
            animationTimer.Start();

            // Запускаем первый корабль с небольшой задержкой
            var startTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1500)
            };
            startTimer.Tick += (s, e) =>
            {
                startTimer.Stop();
                CreateNewShip();
            };
            startTimer.Start();
        }

        private void ShipTimer_Tick(object sender, EventArgs e)
        {
            // Создаем новый корабль
            CreateNewShip();
        }

        private void ShipAnimationTimer_Tick(object sender, EventArgs e)
        {
            // Обновляем позиции всех активных кораблей
            var shipsToRemove = new System.Collections.Generic.List<Ship>();
            
            foreach (var ship in Ships)
            {
                ship.Update();
                
                // Удаляем неактивные корабли
                if (!ship.IsActive)
                {
                    shipsToRemove.Add(ship);
                }
            }

            // Удаляем неактивные корабли
            foreach (var ship in shipsToRemove)
            {
                Ships.Remove(ship);
            }
        }

        private void CreateNewShip()
        {
            int fromX, fromY, toX, toY;

            if (_isShipDirectionToPort2)
            {
                // Летим из порта 1 в порт 2
                fromX = Map.Port1Y+2;
                fromY = Map.Port1X+1;
                toX = Map.Port2Y;
                toY = Map.Port2X;
            }
            else
            {
                // Летим из порта 2 в порт 1
                fromX = Map.Port2Y;
                fromY = Map.Port2X;
                toX = Map.Port1Y+2;
                toY = Map.Port1X+1;
            }

            var ship = new Ship(fromX, fromY, toX, toY);
            
            // Отладочный вывод
            string direction = _isShipDirectionToPort2 ? "из порта 1 в порт 2" : "из порта 2 в порт 1";
            System.Diagnostics.Debug.WriteLine($"Создание корабля: {direction} [{fromX},{fromY}] -> [{toX},{toY}], позиция: X={ship.X}, Y={ship.Y}");
            
            Ships.Add(ship);

            // Меняем направление для следующего корабля
            _isShipDirectionToPort2 = !_isShipDirectionToPort2;
        }

        public void UpdateCellInfo(CellViewModel cell)
        {
            if (cell == null)
            {
                CellInfoViewModel.UpdateInfo(null, null, TrafficManagementViewModel, false, false);
                return;
            }

            RoadViewModel road = null;

            // Проверяем, является ли клетка дорогой, используя наш статический источник
            if (Map.IsRoad(cell.X, cell.Y))
            {
                // Ищем соответствующий ViewModel дороги в коллекции
                road = TrafficManagementViewModel.Roads.FirstOrDefault(r => r.X == cell.X && r.Y == cell.Y);
            }

            bool isPark = Map.IsPark(cell.X, cell.Y);
            bool isBikePath = Map.IsBikePath(cell.X, cell.Y);

            CellInfoViewModel.UpdateInfo(cell, road, TrafficManagementViewModel, isPark, isBikePath);
        }

        private void PopulateInitialTraffic()
        {
            // Уменьшено количество машин для снижения нагрузки
            // Разнообразные маршруты по разным дорогам
            var routes = new (int startX, int startY, int endX, int endY, VehicleType type)[]
            {
                // Автобусы по основным маршрутам
                (30, 50, 70, 50, VehicleType.Bus),
                (50, 30, 50, 70, VehicleType.Bus),
                // Грузовики для доставки
                (20, 20, 80, 80, VehicleType.Truck),
                (80, 20, 20, 80, VehicleType.Truck),
            };

            foreach (var route in routes)
            {
                _trafficManagementViewModel.AddVehicleOnRoute(route.type, route.startX, route.startY, route.endX, route.endY);
            }
        }

        private void InitializeFinancialSystem()
        {
            // Создаем налоговую политику
            _taxPolicy = new Tax
            {
                IncomeTaxPercent = 13.0m,  // 13% НДФЛ
                CorporateTaxPercent = 20.0m // 20% налог на прибыль
            };

            // Создаем бюджет города с начальным капиталом
            _cityBudget = new Budget(initialBudget: 1_000_000m, policy: _taxPolicy);

            // Создаем коллекции для финансовой системы
            var citizens = new ObservableCollection<Citizen>();
            var companies = new ObservableCollection<Company>();

            // Инициализируем финансовую ViewModel
            FinancialOverview = new FinancialOverviewViewModel(_cityBudget, citizens, companies);

            // Первоначальное создание финансовых агентов
            UpdateFinancialAgents();
        }

        private void UpdateFinancialAgents()
        {

            // Очищаем существующие коллекции
            var citizens = FinancialOverview.GetCitizens() as ObservableCollection<Citizen>;
            var companies = FinancialOverview.GetCompanies() as ObservableCollection<Company>;

            citizens?.Clear();
            companies?.Clear();

            // Создаем граждан для финансовой системы
            foreach (var human in PopulationManager.Population.Where(h => h.IsAlive))
            {
                var citizen = new Citizen(human._name, initialBudget: 50_000m)
                {
                    Salary = CalculateSalary(human)
                };
                citizens?.Add(citizen);
            }

            // Создаем компании (учебные заведения как компании)
            var universityCompany = new Company("Городской университет", initialBudget: 500_000m)
            {
                Revenue = CalculateUniversityRevenue(),
                Costs = CalculateUniversityCosts()
            };
            companies?.Add(universityCompany);

            // Добавляем сотрудников в компанию
            foreach (var employee in PopulationManager.University.GetAllEmployees())
            {
                var citizenEmployee = citizens?.FirstOrDefault(c => c.Name == employee._name);
                if (citizenEmployee != null)
                {
                    citizenEmployee.Employer = universityCompany;
                    universityCompany.Employees.Add(citizenEmployee);
                }
            }
        }

        private decimal CalculateSalary(Human human)
        {
            if (!human.IsWorking) return 0;

            // Зарплата в зависимости от уровня образования
            return human._educationLevel switch
            {
                "PhD" => 120_000m,
                "Master +" => 100_000m,
                "Master" => 90_000m,
                "Bachelor +" => 80_000m,
                "Bachelor" => 70_000m,
                "High School" => 50_000m,
                "School" => 40_000m,
                _ => 30_000m
            };
        }

        private decimal CalculateUniversityRevenue()
        {
            // Доходы университета (государственное финансирование, плата за обучение)
            var uni = PopulationManager.University;
            int totalStudents = uni.GetSchoolStudents().Count + uni.GetCollegeStudents().Count + uni.GetUniversityStudents().Count;
            return totalStudents * 25_000m; // 25,000 на студента в год
        }

        private decimal CalculateUniversityCosts()
        {
            // Расходы университета (зарплаты, содержание)
            var uni = PopulationManager.University;
            int employees = uni.GetEmployeeCount();
            return employees * 60_000m + 200_000m; // Зарплаты + операционные расходы
        }

        /// <summary>
        /// Симулирует указанное количество лет.
        /// </summary>
        public void AddYears(int years)
        {
            for (int i = 0; i < years; i++)
            {
                CurrentYear++;
                PopulationManager.SimulateYear();

                // Обновляем финансовых агентов
                UpdateFinancialAgents();

                // Автоматически собираем налоги и выплачиваем зарплаты/субсидии
                FinancialOverview.CollectTaxesCommand.Execute(null);

                // Собираем налоги
                FinancialOverview.CollectTaxes();

                OnPropertyChanged(nameof(EmployeesStats));
            }

            Population = PopulationManager.PopulationCount;

            // Уведомляем интерфейс об изменениях
            OnPropertyChanged(nameof(CurrentYear));
            OnPropertyChanged(nameof(Population));
            OnPropertyChanged(nameof(UniversityStats));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}