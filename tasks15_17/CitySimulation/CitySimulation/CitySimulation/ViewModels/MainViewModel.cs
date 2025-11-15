using System.Windows.Input;

namespace CitySimulation.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private BaseViewModel _currentViewModel;

        public MainViewModel()
        {
            ResourcesViewModel = new ResourcesViewModel();
            UtilitiesViewModel = new UtilitiesViewModel();
            CurrentViewModel = ResourcesViewModel;

            ShowResourcesCommand = new RelayCommand(() => CurrentViewModel = ResourcesViewModel);
            ShowUtilitiesCommand = new RelayCommand(() => CurrentViewModel = UtilitiesViewModel);
        }

        public ResourcesViewModel ResourcesViewModel { get; }
        public UtilitiesViewModel UtilitiesViewModel { get; }

        public BaseViewModel CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public ICommand ShowResourcesCommand { get; }
        public ICommand ShowUtilitiesCommand { get; }
    }
}
