namespace CitySimulation.Models.Base
{
    public class Building : ObservableObject
    {
        private string _name;
        private string _address;
        private double _xCoordinate;
        private double _yCoordinate;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        public double XCoordinate
        {
            get => _xCoordinate;
            set => SetProperty(ref _xCoordinate, value);
        }

        public double YCoordinate
        {
            get => _yCoordinate;
            set => SetProperty(ref _yCoordinate, value);
        }
    }
}