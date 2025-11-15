using CitySimulation.Infrastructure;
using CitySimulation.Models.EmergencyService;
using CitySimulation.ViewModels.Base;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CitySimulation.ViewModels.EmergencyService
{
    public class EmergencyServiceViewModel : ViewModelBase
    {
        private ObservableCollection<FireStation> _fireStations;
        private ObservableCollection<EmergencyCall> _emergencyCalls;
        private ObservableCollection<FireFighterVehicle> _vehicles;
        private FireStation _selectedStation;
        private EmergencyCall _selectedCall;
        private string _statusMessage;

        // Свойства для форм ввода
        private string _newStationName;
        private string _newStationDistrict;
        private int _newStationCapacity;
        private string _newVehicleName;
        private double _newVehicleWater;
        private double _newVehiclePumpPower;
        private string _newEmergencyType;
        private int _newEmergencyThreat;

        public EmergencyServiceViewModel()
        {
            FireStations = new ObservableCollection<FireStation>();
            EmergencyCalls = new ObservableCollection<EmergencyCall>();
            Vehicles = new ObservableCollection<FireFighterVehicle>();

            InitializeTestData();

            // Команды
            DispatchUnitsCommand = new RelayCommand(ExecuteDispatchUnits, CanDispatchUnits);
            AddFireStationCommand = new RelayCommand(ExecuteAddFireStation, CanAddFireStation);
            CreateEmergencyCommand = new RelayCommand(ExecuteCreateEmergency, CanCreateEmergency);
            AddVehicleCommand = new RelayCommand(ExecuteAddVehicle, CanAddVehicle);
        }

        private void InitializeTestData()
        {
            // Тестовые данные
            var station1 = new FireStation
            {
                Name = "Пожарная часть №1",
                ServedDistrict = "Центральный",
                MaxVehicles = 5
            };
            FireStations.Add(station1);
            SelectedStation = station1;

            // Значения по умолчанию для форм
            NewStationCapacity = 5;
            NewVehicleWater = 5000;
            NewVehiclePumpPower = 1000;
            NewEmergencyThreat = 3;
            NewEmergencyType = "Пожар";
        }

        // Основные коллекции
        public ObservableCollection<FireStation> FireStations { get => _fireStations; set => SetProperty(ref _fireStations, value); }
        public ObservableCollection<EmergencyCall> EmergencyCalls { get => _emergencyCalls; set => SetProperty(ref _emergencyCalls, value); }
        public ObservableCollection<FireFighterVehicle> Vehicles { get => _vehicles; set => SetProperty(ref _vehicles, value); }

        // Выбранные элементы
        public FireStation SelectedStation { get => _selectedStation; set => SetProperty(ref _selectedStation, value); }
        public EmergencyCall SelectedCall { get => _selectedCall; set => SetProperty(ref _selectedCall, value); }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        // Свойства для форм
        public string NewStationName { get => _newStationName; set => SetProperty(ref _newStationName, value); }
        public string NewStationDistrict { get => _newStationDistrict; set => SetProperty(ref _newStationDistrict, value); }
        public int NewStationCapacity { get => _newStationCapacity; set => SetProperty(ref _newStationCapacity, value); }
        public string NewVehicleName { get => _newVehicleName; set => SetProperty(ref _newVehicleName, value); }
        public double NewVehicleWater { get => _newVehicleWater; set => SetProperty(ref _newVehicleWater, value); }
        public double NewVehiclePumpPower { get => _newVehiclePumpPower; set => SetProperty(ref _newVehiclePumpPower, value); }
        public string NewEmergencyType { get => _newEmergencyType; set => SetProperty(ref _newEmergencyType, value); }
        public int NewEmergencyThreat { get => _newEmergencyThreat; set => SetProperty(ref _newEmergencyThreat, value); }

        // Команды
        public ICommand DispatchUnitsCommand { get; }
        public ICommand AddFireStationCommand { get; }
        public ICommand CreateEmergencyCommand { get; }
        public ICommand AddVehicleCommand { get; }

        private void ExecuteDispatchUnits(object parameter)
        {
            if (SelectedCall != null && SelectedStation != null)
            {
                StatusMessage = $"🚒 Отправлены подразделения из {SelectedStation.Name} на вызов: {SelectedCall.EmergencyType} (угроза: {SelectedCall.ThreatLevel})";

                // Обновляем состояние техники
                foreach (var vehicle in SelectedStation.Vehicles)
                {
                    vehicle.CurrentState = Enums.VehicleState.OnCall;
                }
            }
            else
            {
                StatusMessage = "❌ Выберите пожарную часть и экстренный вызов";
            }
        }

        private bool CanDispatchUnits(object parameter) => SelectedStation != null && SelectedCall != null;

        private void ExecuteAddFireStation(object parameter)
        {
            if (!string.IsNullOrWhiteSpace(NewStationName) && !string.IsNullOrWhiteSpace(NewStationDistrict))
            {
                var newStation = new FireStation
                {
                    Name = NewStationName.Trim(),
                    ServedDistrict = NewStationDistrict.Trim(),
                    MaxVehicles = NewStationCapacity
                };
                FireStations.Add(newStation);
                StatusMessage = $"✅ Добавлена новая пожарная часть: {newStation.Name} в районе {newStation.ServedDistrict}";

                // Сбрасываем форму
                NewStationName = "";
                NewStationDistrict = "";
            }
            else
            {
                StatusMessage = "❌ Заполните название и район пожарной части";
            }
        }

        private bool CanAddFireStation(object parameter) =>
            !string.IsNullOrWhiteSpace(NewStationName) &&
            !string.IsNullOrWhiteSpace(NewStationDistrict);

        private void ExecuteCreateEmergency(object parameter)
        {
            if (NewEmergencyThreat > 0 && NewEmergencyThreat <= 5)
            {
                var newEmergency = new EmergencyCall
                {
                    EmergencyType = ConvertStringToEmergencyType(NewEmergencyType),
                    ThreatLevel = NewEmergencyThreat,
                    CallTime = DateTime.Now
                };
                EmergencyCalls.Add(newEmergency);
                StatusMessage = $"🚨 Создан новый экстренный вызов: {newEmergency.EmergencyType} (уровень угрозы: {newEmergency.ThreatLevel})";
            }
            else
            {
                StatusMessage = "❌ Уровень угрозы должен быть от 1 до 5";
            }
        }

        private bool CanCreateEmergency(object parameter) => NewEmergencyThreat > 0 && NewEmergencyThreat <= 5;

        private void ExecuteAddVehicle(object parameter)
        {
            if (!string.IsNullOrWhiteSpace(NewVehicleName) && SelectedStation != null)
            {
                FireFighterVehicle newVehicle;

                if (NewVehicleName.ToLower().Contains("лестница"))
                {
                    newVehicle = new LadderTruck
                    {
                        Name = NewVehicleName,
                        MaxWater = NewVehicleWater,
                        CurrentWater = NewVehicleWater,
                        PumpPower = NewVehiclePumpPower,
                        LadderLength = 30,
                        CurrentState = Enums.VehicleState.InGarage
                    };
                }
                else
                {
                    newVehicle = new FireEngine
                    {
                        Name = NewVehicleName,
                        MaxWater = NewVehicleWater,
                        CurrentWater = NewVehicleWater,
                        PumpPower = NewVehiclePumpPower,
                        CurrentState = Enums.VehicleState.InGarage
                    };
                }

                Vehicles.Add(newVehicle);
                SelectedStation.Vehicles.Add(newVehicle);
                StatusMessage = $"✅ Добавлена новая техника: {newVehicle.Name} (вода: {newVehicle.MaxWater}л, насос: {newVehicle.PumpPower}л/с)";

                // Сбрасываем форму
                NewVehicleName = "";
            }
            else
            {
                StatusMessage = "❌ Заполните название техники и выберите пожарную часть";
            }
        }

        private bool CanAddVehicle(object parameter) =>
            !string.IsNullOrWhiteSpace(NewVehicleName) &&
            SelectedStation != null;

        private Enums.EmergencyType ConvertStringToEmergencyType(string emergencyType)
        {
            return emergencyType switch
            {
                "Пожар" => Enums.EmergencyType.Fire,
                "ДТП" => Enums.EmergencyType.TrafficAccident,
                "Химическая авария" => Enums.EmergencyType.ChemicalSpill,
                "Медицинская помощь" => Enums.EmergencyType.MedicalEmergency,
                "Стихийное бедствие" => Enums.EmergencyType.NaturalDisaster,
                _ => Enums.EmergencyType.Fire
            };
        }
    }
}