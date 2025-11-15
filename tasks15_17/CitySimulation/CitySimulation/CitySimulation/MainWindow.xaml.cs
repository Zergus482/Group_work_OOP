using System.Windows;
using System.Windows.Controls;

namespace CitySimulation
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ActivateResourcesTab(); // По умолчанию активна вкладка ресурсов
        }

        private void ResourcesTab_Click(object sender, RoutedEventArgs e)
        {
            ActivateResourcesTab();
        }

        private void UtilitiesTab_Click(object sender, RoutedEventArgs e)
        {
            ActivateUtilitiesTab();
        }

        private void ActivateResourcesTab()
        {
            // Показываем вкладку ресурсов
            ResourcesContent.Visibility = Visibility.Visible;
            UtilitiesContent.Visibility = Visibility.Collapsed;
        }

        private void ActivateUtilitiesTab()
        {
            // Показываем вкладку коммунальных услуг
            ResourcesContent.Visibility = Visibility.Collapsed;
            UtilitiesContent.Visibility = Visibility.Visible;
        }
    }
}