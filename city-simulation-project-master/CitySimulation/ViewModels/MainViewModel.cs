using CitySimulation.ViewModels.Base;
using CitySimulation.ViewModels.ForeignRelations;
using CitySimulation.ViewModels.EmergencyService;

namespace CitySimulation.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public ForeignRelationsViewModel ForeignRelationsVM { get; }
        public EmergencyServiceViewModel EmergencyServiceVM { get; }

        public MainViewModel()
        {
            ForeignRelationsVM = new ForeignRelationsViewModel();
            EmergencyServiceVM = new EmergencyServiceViewModel();
        }
    }
}