using System.Windows;
using CitySimulation.ViewModels;

namespace CitySimulation
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Создаем MainViewModel и устанавливаем как DataContext
            var mainViewModel = new MainViewModel();
            this.DataContext = mainViewModel;
        }
    }
}