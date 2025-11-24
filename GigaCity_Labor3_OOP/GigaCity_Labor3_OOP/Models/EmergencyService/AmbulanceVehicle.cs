using System;
using System.ComponentModel;

namespace GigaCity_Labor3_OOP.Models.EmergencyService
{
    public class AmbulanceVehicle : INotifyPropertyChanged
    {
        private VehicleState _currentState;
        private double _x;
        private double _y;
        private double _targetX;
        private double _targetY;
        private MedicalPoint _assignedMedicalPoint;
        private MedicalEmergencyCall _currentCall;
        private bool _hasPatient;

        public string Name { get; set; }
        public MedicalPoint AssignedMedicalPoint
        {
            get => _assignedMedicalPoint;
            set
            {
                _assignedMedicalPoint = value;
                OnPropertyChanged(nameof(AssignedMedicalPoint));
            }
        }

        public VehicleState CurrentState
        {
            get => _currentState;
            set
            {
                _currentState = value;
                OnPropertyChanged(nameof(CurrentState));
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public double X
        {
            get => _x;
            set
            {
                _x = value;
                OnPropertyChanged(nameof(X));
            }
        }

        public double Y
        {
            get => _y;
            set
            {
                _y = value;
                OnPropertyChanged(nameof(Y));
            }
        }

        public double TargetX
        {
            get => _targetX;
            set
            {
                _targetX = value;
                OnPropertyChanged(nameof(TargetX));
                OnPropertyChanged(nameof(TargetGridX));
            }
        }

        public double TargetY
        {
            get => _targetY;
            set
            {
                _targetY = value;
                OnPropertyChanged(nameof(TargetY));
                OnPropertyChanged(nameof(TargetGridY));
            }
        }

        public MedicalEmergencyCall CurrentCall
        {
            get => _currentCall;
            set
            {
                _currentCall = value;
                OnPropertyChanged(nameof(CurrentCall));
            }
        }

        public bool HasPatient
        {
            get => _hasPatient;
            set
            {
                _hasPatient = value;
                OnPropertyChanged(nameof(HasPatient));
            }
        }

        public double Speed { get; set; } = 2.5; // Скорость движения (пикселей за обновление)

        public string StatusText
        {
            get
            {
                return CurrentState switch
                {
                    VehicleState.Idle => "Ожидание",
                    VehicleState.OnMission => "На вызове",
                    VehicleState.Transporting => "Перевозка пациента",
                    VehicleState.Returning => "Возвращается",
                    _ => "Неизвестно"
                };
            }
        }

        // Grid координаты для отображения (целые числа)
        public int TargetGridX => (int)(TargetX / 15.0);
        public int TargetGridY => (int)(TargetY / 15.0);

        public AmbulanceVehicle()
        {
            CurrentState = VehicleState.Idle;
        }

        public void SetTarget(double targetX, double targetY)
        {
            TargetX = targetX;
            TargetY = targetY;
        }

        public bool IsAtTarget(double threshold = 5.0)
        {
            // Оптимизация: используем квадрат расстояния вместо вычисления корня
            double dx = TargetX - X;
            double dy = TargetY - Y;
            double thresholdSquared = threshold * threshold;
            return (dx * dx + dy * dy) < thresholdSquared;
        }

        public void UpdatePosition()
        {
            if (CurrentState == VehicleState.Idle)
            {
                // Если в состоянии idle, возвращаемся к больнице
                if (AssignedMedicalPoint != null)
                {
                    TargetX = AssignedMedicalPoint.XCoordinate;
                    TargetY = AssignedMedicalPoint.YCoordinate;
                }
            }

            double dx = TargetX - X;
            double dy = TargetY - Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance > Speed)
            {
                // Двигаемся к цели
                double moveX = (dx / distance) * Speed;
                double moveY = (dy / distance) * Speed;
                X += moveX;
                Y += moveY;
            }
            else
            {
                // Достигли цели
                X = TargetX;
                Y = TargetY;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

