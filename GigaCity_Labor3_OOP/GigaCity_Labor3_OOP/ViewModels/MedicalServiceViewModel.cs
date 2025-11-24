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
    public class MedicalServiceViewModel : INotifyPropertyChanged
    {
        private readonly MapModel _map;
        private readonly Random _random = new Random();
        private DispatcherTimer _callGenerationTimer;
        private DispatcherTimer _updateTimer;
        private DispatcherTimer _treatmentTimer;
        private string _statusMessage;
        private ResidentialServiceViewModel _residentialService;

        public ObservableCollection<MedicalPoint> MedicalPoints { get; private set; }
        public ObservableCollection<MedicalEmergencyCall> EmergencyCalls { get; private set; }
        public ObservableCollection<AmbulanceVehicle> AllAmbulances { get; private set; }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public MedicalServiceViewModel(MapModel map)
        {
            _map = map;
            MedicalPoints = new ObservableCollection<MedicalPoint>();
            EmergencyCalls = new ObservableCollection<MedicalEmergencyCall>();
            AllAmbulances = new ObservableCollection<AmbulanceVehicle>();

            StartTimers();
        }

        public void SetResidentialService(ResidentialServiceViewModel residentialService)
        {
            _residentialService = residentialService;
        }

        public void AddMedicalPoint(int gridX, int gridY)
        {
            var medicalPoint = new MedicalPoint
            {
                Name = $"Больница #{MedicalPoints.Count + 1}",
                XCoordinate = gridX * 15.0 + 7.5, // Центр клетки
                YCoordinate = gridY * 15.0 + 7.5,
                MaxRooms = 5
            };

            // Добавляем 2 машины скорой помощи по умолчанию
            for (int i = 0; i < 2; i++)
            {
                var ambulance = new AmbulanceVehicle
                {
                    Name = $"Скорая #{MedicalPoints.Count + 1}-{i + 1}",
                    AssignedMedicalPoint = medicalPoint,
                    X = medicalPoint.XCoordinate,
                    Y = medicalPoint.YCoordinate,
                    CurrentState = VehicleState.Idle
                };
                medicalPoint.Ambulances.Add(ambulance);
                AllAmbulances.Add(ambulance);
            }

            MedicalPoints.Add(medicalPoint);
            StatusMessage = $"Добавлена {medicalPoint.Name} на координатах ({gridX}, {gridY})";
        }

        private void StartTimers()
        {
            // Таймер для генерации случайных вызовов
            _callGenerationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(8)
            };
            _callGenerationTimer.Tick += CallGenerationTimer_Tick;
            _callGenerationTimer.Start();

            // Таймер для обновления позиций машин и обработки вызовов
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50) // Обновление каждые 50мс для плавного движения
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();

            // Таймер для лечения пациентов
            _treatmentTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _treatmentTimer.Tick += TreatmentTimer_Tick;
            _treatmentTimer.Start();
        }

        private void CallGenerationTimer_Tick(object sender, EventArgs e)
        {
            // Вызовы теперь генерируются только от больных жителей в домах
            // Этот таймер можно оставить для совместимости или отключить
            // if (_random.Next(100) < 30 && MedicalPoints.Count > 0)
            // {
            //     CreateRandomEmergencyCall();
            // }
        }

        private void CreateRandomEmergencyCall()
        {
            // Выбираем случайную позицию на карте
            int gridX = _random.Next(_map.Width);
            int gridY = _random.Next(_map.Height);
            double x = gridX * 15.0 + 7.5;
            double y = gridY * 15.0 + 7.5;

            var call = new MedicalEmergencyCall
            {
                XCoordinate = x,
                YCoordinate = y
            };

            EmergencyCalls.Add(call);
            StatusMessage = $"Новый вызов скорой помощи на координатах ({gridX}, {gridY})";

            // Автоматически находим и отправляем ближайшую свободную машину
            DispatchNearestAmbulance(call);
        }

        public void DispatchNearestAmbulance(MedicalEmergencyCall call)
        {
            // Оптимизированный поиск ближайшей свободной машины
            AmbulanceVehicle nearestAmbulance = null;
            double minDistanceSquared = double.MaxValue; // Используем квадрат расстояния для оптимизации

            // Фильтруем только свободные машины один раз
            var idleAmbulances = AllAmbulances.Where(a => a.CurrentState == VehicleState.Idle).ToList();
            
            if (idleAmbulances.Count == 0)
            {
                StatusMessage = "Нет свободных машин скорой помощи";
                return;
            }

            double callX = call.XCoordinate;
            double callY = call.YCoordinate;

            // Ищем ближайшую машину без вычисления квадратного корня (оптимизация)
            foreach (var ambulance in idleAmbulances)
            {
                double dx = ambulance.X - callX;
                double dy = ambulance.Y - callY;
                double distanceSquared = dx * dx + dy * dy; // Избегаем Math.Sqrt для производительности

                if (distanceSquared < minDistanceSquared)
                {
                    minDistanceSquared = distanceSquared;
                    nearestAmbulance = ambulance;
                }
            }

            if (nearestAmbulance != null)
            {
                nearestAmbulance.CurrentState = VehicleState.OnMission;
                nearestAmbulance.CurrentCall = call;
                nearestAmbulance.SetTarget(call.XCoordinate, call.YCoordinate);
                call.IsResponded = true;
                call.AssignedAmbulance = nearestAmbulance;
                StatusMessage = $"Машина {nearestAmbulance.Name} отправлена на вызов";
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            // Оптимизированное обновление - обрабатываем только активные машины
            var activeAmbulances = AllAmbulances.Where(a => 
                a.CurrentState != VehicleState.Idle).ToList();

            foreach (var ambulance in activeAmbulances)
            {
                ambulance.UpdatePosition();

                // Проверяем, достигла ли машина цели (оптимизированная проверка)
                if (!ambulance.IsAtTarget()) continue;

                // Обрабатываем достижение цели в зависимости от состояния
                switch (ambulance.CurrentState)
                {
                    case VehicleState.OnMission when ambulance.CurrentCall != null:
                        HandleCallArrival(ambulance);
                        break;
                    case VehicleState.Transporting when ambulance.AssignedMedicalPoint != null:
                        HandleHospitalArrival(ambulance);
                        break;
                    case VehicleState.Returning when ambulance.AssignedMedicalPoint != null:
                        ambulance.CurrentState = VehicleState.Idle;
                        ambulance.HasPatient = false;
                        ambulance.CurrentCall = null;
                        break;
                }
            }
        }

        private void HandleCallArrival(AmbulanceVehicle ambulance)
        {
            if (ambulance.CurrentCall == null) return;

            var call = ambulance.CurrentCall;
            var medicalPoint = ambulance.AssignedMedicalPoint;

            // Проверяем, есть ли свободная палата в родной больнице
            if (medicalPoint != null && medicalPoint.HasFreeRoom)
            {
                // Удаляем больного жителя из дома (если вызов от жителя)
                if (_residentialService != null)
                {
                    var building = _residentialService.Buildings.FirstOrDefault(b =>
                        Math.Abs(b.XCoordinate - call.XCoordinate) < 10 &&
                        Math.Abs(b.YCoordinate - call.YCoordinate) < 10);

                    if (building != null)
                    {
                        var sickResident = building.GetSickResident();
                        if (sickResident != null)
                        {
                            _residentialService.RemoveResidentFromBuilding(building, sickResident);
                        }
                    }
                }

                // Забираем пациента, вызов исчезает
                EmergencyCalls.Remove(call);
                
                // Машина забирает пациента и везет в больницу
                ambulance.HasPatient = true;
                ambulance.CurrentState = VehicleState.Transporting;
                ambulance.SetTarget(
                    medicalPoint.XCoordinate,
                    medicalPoint.YCoordinate);
                
                ambulance.CurrentCall = null;
                StatusMessage = $"Машина {ambulance.Name} забрала пациента, везет в больницу";
            }
            else
            {
                // Нет свободных палат - вызов остается, машина возвращается
                ambulance.CurrentState = VehicleState.Returning;
                ambulance.CurrentCall = null;
                if (medicalPoint != null)
                {
                    ambulance.SetTarget(
                        medicalPoint.XCoordinate,
                        medicalPoint.YCoordinate);
                }
                StatusMessage = $"Нет свободных палат в {medicalPoint?.Name ?? "больнице"}. Машина возвращается.";
            }
        }

        private void HandleHospitalArrival(AmbulanceVehicle ambulance)
        {
            if (ambulance.AssignedMedicalPoint == null) return;

            var medicalPoint = ambulance.AssignedMedicalPoint;

            // Помещаем пациента в палату
            if (ambulance.HasPatient && medicalPoint.HasFreeRoom)
            {
                var patient = new Patient
                {
                    RoomNumber = medicalPoint.OccupiedRooms + 1
                };
                medicalPoint.Patients.Add(patient);
                medicalPoint.OccupiedRooms++;

                // Освобождаем машину
                ambulance.HasPatient = false;
                ambulance.CurrentState = VehicleState.Idle;
                StatusMessage = $"Пациент доставлен в {medicalPoint.Name}. Пациенты: {medicalPoint.OccupiedRooms}/{medicalPoint.MaxRooms}";
            }
            else
            {
                // Не должно произойти, но на всякий случай
                ambulance.CurrentState = VehicleState.Idle;
                ambulance.HasPatient = false;
            }
        }

        private void TreatmentTimer_Tick(object sender, EventArgs e)
        {
            // Оптимизированное обновление лечения - обрабатываем только больницы с пациентами
            const double progressIncrement = 10.0; // 10% за секунду (10 секунд = 100%)

            foreach (var medicalPoint in MedicalPoints)
            {
                if (medicalPoint.Patients.Count == 0) continue; // Пропускаем пустые больницы

                var patientsToDischarge = new System.Collections.Generic.List<Patient>();

                foreach (var patient in medicalPoint.Patients)
                {
                    // Увеличиваем прогресс лечения
                    patient.TreatmentProgress += progressIncrement;

                    if (patient.IsReadyForDischarge && !patient.IsDischarged)
                    {
                        patientsToDischarge.Add(patient);
                    }
                }

                // Выписываем готовых пациентов (оптимизация: один раз обновляем статус)
                if (patientsToDischarge.Count > 0)
                {
                    foreach (var patient in patientsToDischarge)
                    {
                        medicalPoint.Patients.Remove(patient);
                        medicalPoint.OccupiedRooms--;
                    }
                    StatusMessage = $"Выписано {patientsToDischarge.Count} пациентов из {medicalPoint.Name}. Пациенты: {medicalPoint.OccupiedRooms}/{medicalPoint.MaxRooms}";
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

