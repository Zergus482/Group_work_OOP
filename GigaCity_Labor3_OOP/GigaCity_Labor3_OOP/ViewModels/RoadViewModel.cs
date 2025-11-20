using System.ComponentModel;
using System.Runtime.CompilerServices;
using GigaCity_Labor3_OOP.Models;

namespace GigaCity_Labor3_OOP.ViewModels
{
    public class RoadViewModel : INotifyPropertyChanged
    {
        private RoadModel _road;

        public RoadModel Road
        {
            get => _road;
            set
            {
                _road = value;
                OnPropertyChanged();
            }
        }

        public int X => Road.X;
        public int Y => Road.Y;
        public int SpeedLimit => Road.SpeedLimit;
        public bool IsOneWay => Road.IsOneWay;
        public decimal MaintenanceCost => Road.MaintenanceCost;
        public int TrafficLevel
        {
            get => Road.TrafficLevel;
            set
            {
                Road.TrafficLevel = value;
                OnPropertyChanged();
            }
        }

        public RoadViewModel(RoadModel road)
        {
            Road = road;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }
    }
}