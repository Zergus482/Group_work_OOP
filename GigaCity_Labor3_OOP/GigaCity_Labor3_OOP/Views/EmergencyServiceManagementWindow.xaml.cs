using System.Windows;
using GigaCity_Labor3_OOP.ViewModels;

namespace GigaCity_Labor3_OOP.Views
{
    public partial class EmergencyServiceManagementWindow : Window
    {
        public EmergencyServiceManagementWindow(MainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = mainViewModel.EmergencyService;
        }
    }
}

