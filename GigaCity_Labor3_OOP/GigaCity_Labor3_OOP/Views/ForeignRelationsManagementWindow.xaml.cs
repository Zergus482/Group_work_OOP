using System.Windows;
using GigaCity_Labor3_OOP.ViewModels;

namespace GigaCity_Labor3_OOP.Views
{
    public partial class ForeignRelationsManagementWindow : Window
    {
        public ForeignRelationsManagementWindow(MainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = mainViewModel.ForeignRelations;
        }
    }
}

