using CitySimulation.Models.Base;

namespace CitySimulation.ViewModels.Base
{
    public abstract class ViewModelBase : ObservableObject
    {
        protected virtual string DisplayName { get; }
    }
}