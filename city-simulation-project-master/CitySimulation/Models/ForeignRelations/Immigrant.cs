using CitySimulation.Enums;
using CitySimulation.Models.Base;

namespace CitySimulation.Models.ForeignRelations
{
    public class Immigrant : ObservableObject
    {
        private string _name;
        private Country _countryOfOrigin;
        private ImmigrationReason _reason;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public Country CountryOfOrigin
        {
            get => _countryOfOrigin;
            set => SetProperty(ref _countryOfOrigin, value);
        }

        public ImmigrationReason Reason
        {
            get => _reason;
            set => SetProperty(ref _reason, value);
        }
    }
}