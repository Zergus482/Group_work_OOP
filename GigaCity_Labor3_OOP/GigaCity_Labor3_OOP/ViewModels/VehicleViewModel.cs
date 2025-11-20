using GigaCity_Labor3_OOP.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace GigaCity_Labor3_OOP.ViewModels
{
    public class VehicleViewModel : INotifyPropertyChanged
    {
        private VehicleModel _vehicle;

        public VehicleModel Vehicle
        {
            get => _vehicle;
            set
            {
                _vehicle = value;
                OnPropertyChanged();
            }
        }

        public int Id => Vehicle.Id;
        public VehicleType Type => Vehicle.Type;
        public int MaxSpeed => Vehicle.MaxSpeed;
        public int CurrentSpeed
        {
            get => Vehicle.CurrentSpeed;
            set
            {
                Vehicle.CurrentSpeed = value;
                OnPropertyChanged();
            }
        }
        public bool IsMoving
        {
            get => Vehicle.IsMoving;
            set
            {
                Vehicle.IsMoving = value;
                OnPropertyChanged();
            }
        }
        public int CurrentX
        {
            get => Vehicle.CurrentX;
            set
            {
                Vehicle.CurrentX = value;
                OnPropertyChanged();
            }
        }
        public int CurrentY
        {
            get => Vehicle.CurrentY;
            set
            {
                Vehicle.CurrentY = value;
                OnPropertyChanged();
            }
        }
        public int DestinationX => Vehicle.DestinationX;
        public int DestinationY => Vehicle.DestinationY;
        public int MoveProgress
        {
            get => Vehicle.MoveProgress;
            set
            {
                Vehicle.MoveProgress = value;
                OnPropertyChanged();
            }
        }

        public Brush VehicleColor
        {
            get
            {
                return Vehicle.Type switch
                {
                    VehicleType.Car => Brushes.Blue,
                    VehicleType.Bus => Brushes.Green,
                    VehicleType.Truck => Brushes.Orange,
                    VehicleType.Taxi => Brushes.Yellow,
                    VehicleType.Emergency => Brushes.Red,
                    VehicleType.Delivery => Brushes.Purple,
                    _ => Brushes.Gray
                };
            }
        }

        public VehicleViewModel(VehicleModel vehicle)
        {
            Vehicle = vehicle;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }
    }
}