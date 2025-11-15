using System.Collections.Generic;
using TheFinancialSystem;

namespace TheFinancialSystem
{
    public class Company : EconomicAgent
    {
        public decimal Revenue { get; set; }
        public decimal Costs { get; set; }
        public List<Citizen> Employees { get; } = new List<Citizen>();

        public Company(string name, decimal initialBudget) : base(name, initialBudget) { }

        /// <summary>
        /// Выплатить зарплаты всем сотрудникам
        /// </summary>
        public void PaySalaries()
        {
            foreach (var employee in Employees)
            {
                if (this.SpendMoney(employee.Salary, $"Зарплата: {employee.Name}", employee))
                {
                    employee.ReceiveMoney(employee.Salary, $"Зарплата от {this.Name}", this);
                }
            }
        }
    }
}