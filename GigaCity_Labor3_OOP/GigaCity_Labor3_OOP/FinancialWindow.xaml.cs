using System.Windows;
using GigaCity_Labor3_OOP.Converters;

namespace GigaCity_Labor3_OOP
{
    public partial class FinancialWindow : Window
    {
        public FinancialWindow()
        {
            InitializeComponent();

            this.Resources.Add("ProfitLossColorConverter", new ProfitLossColorConverter());
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}