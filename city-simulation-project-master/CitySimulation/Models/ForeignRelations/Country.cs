using CitySimulation.Models.Base;

namespace CitySimulation.Models.ForeignRelations
{
    public class Country : ObservableObject
    {
        private string _name;
        private int _relationsLevel;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public int RelationsLevel
        {
            get => _relationsLevel;
            set => SetProperty(ref _relationsLevel, value);
        }
    }
}