using System.Collections.Generic;
using System.Collections.ObjectModel;
using TheFinancialSystem;
using TheFinancialSystem.ViewModels;

namespace GigaCity_Labor3_OOP.ViewModels
{
    public static class FinancialOverviewViewModelExtensions
    {
        public static IEnumerable<Citizen> GetCitizens(this FinancialOverviewViewModel viewModel)
        {
            // Используем рефлексию для доступа к приватным полям
            var citizensField = typeof(FinancialOverviewViewModel).GetField("_citizens",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            return citizensField?.GetValue(viewModel) as IEnumerable<Citizen>;
        }

        public static IEnumerable<Company> GetCompanies(this FinancialOverviewViewModel viewModel)
        {
            var companiesField = typeof(FinancialOverviewViewModel).GetField("_companies",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            return companiesField?.GetValue(viewModel) as IEnumerable<Company>;
        }

        public static void CollectTaxes(this FinancialOverviewViewModel viewModel)
        {
            viewModel.CollectTaxesCommand.Execute(null);
        }
    }
}