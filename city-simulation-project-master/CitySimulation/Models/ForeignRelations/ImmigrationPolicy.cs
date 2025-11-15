using CitySimulation.Models.Base;

namespace CitySimulation.Models.ForeignRelations
{
    public class ImmigrationPolicy : ObservableObject
    {
        private int _annualQuota;
        private int _educationRequirement;
        private decimal _minimumCapital;

        public int AnnualQuota
        {
            get => _annualQuota;
            set => SetProperty(ref _annualQuota, value);
        }

        public int EducationRequirement
        {
            get => _educationRequirement;
            set => SetProperty(ref _educationRequirement, value);
        }

        public decimal MinimumCapital
        {
            get => _minimumCapital;
            set => SetProperty(ref _minimumCapital, value);
        }
    }
}