using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using GigaCity_Labor3_OOP.Models;
using GigaCity_Labor3_OOP.Models.EmergencyService;

namespace GigaCity_Labor3_OOP.ViewModels
{
    public class ResidentialServiceViewModel : INotifyPropertyChanged
    {
        private readonly MapModel _map;
        private readonly Random _random = new Random();
        private DispatcherTimer _sicknessTimer;
        private DispatcherTimer _populationGrowthTimer;
        private MedicalServiceViewModel _medicalService;

        public ObservableCollection<ResidentialBuilding> Buildings { get; private set; }

        public ResidentialServiceViewModel(MapModel map)
        {
            _map = map;
            Buildings = new ObservableCollection<ResidentialBuilding>();
            StartTimers();
        }

        public void SetMedicalService(MedicalServiceViewModel medicalService)
        {
            _medicalService = medicalService;
        }

        public void AddResidentialBuilding(int gridX, int gridY, int width = 2, int height = 2)
        {
            // Определяем вместимость в зависимости от размера
            int capacity = width * height; // От 1 до 5 жителей

            var building = new ResidentialBuilding
            {
                Name = $"Дом #{Buildings.Count + 1}",
                XCoordinate = gridX * 15.0 + (width * 15.0 / 2.0),
                YCoordinate = gridY * 15.0 + (height * 15.0 / 2.0),
                Width = width,
                Height = height,
                MaxCapacity = capacity
            };

            // Заселяем начальных жителей (50-80% от вместимости)
            int initialResidents = _random.Next(capacity / 2, capacity + 1);
            for (int i = 0; i < initialResidents; i++)
            {
                var resident = new Resident();
                building.AddResident(resident);
            }

            Buildings.Add(building);
        }

        private void StartTimers()
        {
            // Таймер для генерации болезней
            _sicknessTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5) // Проверка каждые 5 секунд
            };
            _sicknessTimer.Tick += SicknessTimer_Tick;
            _sicknessTimer.Start();

            // Таймер для роста населения
            _populationGrowthTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10) // Новые жители каждые 10 секунд
            };
            _populationGrowthTimer.Tick += PopulationGrowthTimer_Tick;
            _populationGrowthTimer.Start();
        }

        private void SicknessTimer_Tick(object sender, EventArgs e)
        {
            // Генерируем болезни для здоровых жителей
            foreach (var building in Buildings)
            {
                foreach (var resident in building.Residents.Where(r => r.HealthStatus == HealthStatus.Healthy))
                {
                    // Вероятность заболеть: 2% каждые 5 секунд
                    if (_random.Next(100) < 2)
                    {
                        resident.HealthStatus = HealthStatus.Sick;
                        resident.SicknessTimer = 0;
                        resident.HasCalledAmbulance = false;

                        // Создаем вызов скорой помощи
                        CreateEmergencyCallForSickResident(building, resident);
                    }
                }

                // Обновляем таймеры болезней для больных жителей
                foreach (var resident in building.Residents.Where(r => r.IsSick))
                {
                    resident.SicknessTimer += 5.0; // Увеличиваем на 5 секунд
                }
            }
        }

        private void CreateEmergencyCallForSickResident(ResidentialBuilding building, Resident resident)
        {
            if (_medicalService == null || resident.HasCalledAmbulance) return;

            // Создаем вызов скорой помощи у дома
            var call = new MedicalEmergencyCall
            {
                XCoordinate = building.XCoordinate,
                YCoordinate = building.YCoordinate
            };

            _medicalService.EmergencyCalls.Add(call);
            resident.HasCalledAmbulance = true;

            // Автоматически отправляем ближайшую машину
            _medicalService.DispatchNearestAmbulance(call);
        }

        private void PopulationGrowthTimer_Tick(object sender, EventArgs e)
        {
            // Заселяем новых жителей в дома с свободными местами
            var buildingsWithSpace = Buildings.Where(b => b.HasFreeSpace).ToList();
            
            if (buildingsWithSpace.Count == 0) return;

            // Заселяем 1-2 новых жителя
            int newResidentsCount = _random.Next(1, 3);
            
            for (int i = 0; i < newResidentsCount && buildingsWithSpace.Count > 0; i++)
            {
                var building = buildingsWithSpace[_random.Next(buildingsWithSpace.Count)];
                
                if (building.HasFreeSpace)
                {
                    var resident = new Resident();
                    building.AddResident(resident);
                }

                // Удаляем дом из списка, если он заполнен
                if (!building.HasFreeSpace)
                {
                    buildingsWithSpace.Remove(building);
                }
            }
        }

        public void RemoveResidentFromBuilding(ResidentialBuilding building, Resident resident)
        {
            building.RemoveResident(resident);
        }

        public int GetTotalPopulation()
        {
            return Buildings.Sum(b => b.ResidentCount);
        }

        public int GetSickPopulation()
        {
            return Buildings.Sum(b => b.Residents.Count(r => r.IsSick));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

