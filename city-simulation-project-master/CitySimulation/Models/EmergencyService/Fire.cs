using CitySimulation.Enums;

namespace CitySimulation.Models.EmergencyService
{
    public class Fire : Disaster
    {
        private double _spreadRate;
        private bool _isChemical;

        public double SpreadRate
        {
            get => _spreadRate;
            set => SetProperty(ref _spreadRate, value);
        }

        public bool IsChemical
        {
            get => _isChemical;
            set => SetProperty(ref _isChemical, value);
        }
    }
}