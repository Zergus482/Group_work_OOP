using System.Collections.Generic;
using System.Linq;
using TheFinancialSystem;

namespace TheFinancialSystem
{
    public class Budget : EconomicAgent
    {
        public Tax TaxPolicy { get; }
        public decimal TotalIncomeTaxCollected { get; private set; }
        public decimal TotalCorporateTaxCollected { get; private set; }

        public decimal TotalSalariesPaid { get; private set; }
        public decimal TotalSubsidiesPaid { get; private set; }

        public void PaySalaries(IEnumerable<Citizen> citizens, IEnumerable<Company> companies)
        {
            TotalSalariesPaid = 0;

            foreach (var company in companies)
            {
                // Предположим, мы выплачиваем зарплаты только для бюджетных организаций
                // Например, для университета. В реальной системе нужен флаг IsStateOwned.
                if (company.Name.Contains("университет", System.StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var employee in company.Employees)
                    {
                        // Бюджет выплачивает 100% зарплаты для бюджетников
                        var salaryAmount = employee.Salary;
                        if (this.SpendMoney(salaryAmount, "Выплата зарплаты (бюджет)", employee))
                        {
                            employee.ReceiveMoney(salaryAmount, "Зарплата от бюджета", this);
                            TotalSalariesPaid += salaryAmount;
                        }
                    }
                }
            }
        }

        public void PaySubsidies(IEnumerable<Company> companies)
        {
            TotalSubsidiesPaid = 0;

            foreach (var company in companies)
            {
                // Бюджет выплачивает субсидию компании (например, 5% от её расходов)
                var subsidyAmount = company.Costs * 0.05m;
                if (this.SpendMoney(subsidyAmount, "Субсидия", company))
                {
                    company.ReceiveMoney(subsidyAmount, "Субсидия от бюджета", this);
                    TotalSubsidiesPaid += subsidyAmount;
                }
            }
        }

        public Budget(decimal initialBudget, Tax policy)
        : base("Городская казна", initialBudget)
        {
            TaxPolicy = policy;
        }

        public void CollectTaxes(IEnumerable<Citizen> citizens, IEnumerable<Company> companies)
        {
            TotalIncomeTaxCollected = 0;
            TotalCorporateTaxCollected = 0;

            foreach (var citizen in citizens.Where(c => c.Salary > 0))
            {
                var taxAmount = citizen.Salary * (TaxPolicy.IncomeTaxPercent / 100.0m);
                if (citizen.SpendMoney(taxAmount, "Налог на доходы", this))
                {
                    this.ReceiveMoney(taxAmount, "Сбор НДФЛ", citizen);
                    TotalIncomeTaxCollected += taxAmount;
                }
            }

            foreach (var company in companies)
            {
                var profit = company.Revenue - company.Costs;
                if (profit > 0)
                {
                    var taxAmount = profit * (TaxPolicy.CorporateTaxPercent / 100.0m);
                    if (company.SpendMoney(taxAmount, "Налог на прибыль", this))
                    {
                        this.ReceiveMoney(taxAmount, "Сбор налога на прибыль", company);
                        TotalCorporateTaxCollected += taxAmount;
                    }
                }
            }
        }
    }
}
