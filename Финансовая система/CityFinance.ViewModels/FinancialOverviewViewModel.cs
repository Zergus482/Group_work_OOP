using TheFinancialSystem;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CityFinance.ViewModels;

namespace TheFinancialSystem.ViewModels
{
    public class FinancialOverviewViewModel : ViewModelBase
    {
        private readonly Budget _budget;
        private readonly IEnumerable<Citizen> _citizens;
        private readonly IEnumerable<Company> _companies;

        public string CityBudget => _budget.Budget.ToString("C");
        public string TotalIncomeTaxCollected => _budget.TotalIncomeTaxCollected.ToString("C");
        public string TotalCorporateTaxCollected => _budget.TotalCorporateTaxCollected.ToString("C");

        // НОВЫЕ свойства для отображения расходов бюджета
        public string TotalSalariesPaid => _budget.TotalSalariesPaid.ToString("C");
        public string TotalSubsidiesPaid => _budget.TotalSubsidiesPaid.ToString("C");

        // Явно указываем, что это TheFinancialSystem.Core.Transaction
        public ObservableCollection<TheFinancialSystem.FinancialTransaction> TransactionHistory { get; } = new();
        public ICommand CollectTaxesCommand { get; }

        public FinancialOverviewViewModel(Budget treasury, IEnumerable<Citizen> citizens, IEnumerable<Company> companies)
        {
            _budget = treasury;
            _citizens = citizens;
            _companies = companies;

            _budget.OnTransactionOccurred += OnNewTransaction;

            CollectTaxesCommand = new RelayCommand(_ => CollectTaxes());
        }

        private void CollectTaxes()
        {
            // 1. Собираем налоги с граждан и компаний
            _budget.CollectTaxes(_citizens, _companies);

            // 2. Выплачиваем зарплаты (например, бюджетным организациям)
            _budget.PaySalaries(_citizens, _companies);

            // 3. Выплачиваем субсидии компаниям
            _budget.PaySubsidies(_companies);

            // 4. Уведомляем интерфейс об изменениях
            OnPropertyChanged(nameof(CityBudget));
            OnPropertyChanged(nameof(TotalIncomeTaxCollected));
            OnPropertyChanged(nameof(TotalCorporateTaxCollected));
            OnPropertyChanged(nameof(TotalSalariesPaid));
            OnPropertyChanged(nameof(TotalSubsidiesPaid));
        }

        private void OnNewTransaction(object? sender, TheFinancialSystem.FinancialTransaction transaction)
        {
            TransactionHistory.Insert(0, transaction);
            if (TransactionHistory.Count > 100)
            {
                TransactionHistory.RemoveAt(TransactionHistory.Count - 1);
            }
        }
    }
}