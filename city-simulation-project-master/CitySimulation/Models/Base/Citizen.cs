namespace CitySimulation.Models.Base
{
    public class Citizen : ObservableObject
    {
        private string _name;
        private int _age;
        private string _profession;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public int Age
        {
            get => _age;
            set => SetProperty(ref _age, value);
        }

        public string Profession
        {
            get => _profession;
            set => SetProperty(ref _profession, value);
        }
    }
}