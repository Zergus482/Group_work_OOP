using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using GigaCity_Labor3_OOP.Models;
using TheFinancialSystem;
using TheFinancialSystem.ViewModels;

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

        public string EmployeesStats => $"Работают: {PopulationManager.University.GetEmployeeCount()}/100";

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

            // Инициализация финансовой системы
            InitializeFinancialSystem();

            // Инициализация системы самолетов
            Planes = new ObservableCollection<Plane>();
            InitializePlaneSystem();

            SelectedCell = Map.Cells.FirstOrDefault();
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
                fromX = Map.Airport1Y;  // Было Airport1X, стало Airport1Y
                fromY = Map.Airport1X;  // Было Airport1Y, стало Airport1X
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