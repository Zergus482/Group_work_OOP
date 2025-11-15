using System;
using System.Collections.Generic;
using System.Linq;
using TheFinancialSystem;

namespace TheFinancialSystem
{
    public class Organization : EconomicAgent
    {
        public string Type { get; set; }  // Тип организации (университет, завод и т.д.)
        public decimal Revenue { get; set; }
        public decimal Costs { get; set; }
        public List<Citizen> Employees { get; } = new List<Citizen>();
        public decimal GovernmentFunding { get; set; }  // Государственное финансирование

        public Organization(string name, string type, decimal initialBudget) : base(name, initialBudget)
        {
            Type = type;
        }

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

        /// <summary>
        /// Получить государственное финансирование
        /// </summary>
        public void ReceiveGovernmentFunding(decimal amount, Budget budget)
        {
            if (budget.SpendMoney(amount, $"Финансирование: {this.Name}", this))
            {
                this.ReceiveMoney(amount, $"Государственное финансирование", budget);
                GovernmentFunding += amount;
            }
        }

        /// <summary>
        /// Рассчитать прибыль
        /// </summary>
        public decimal GetProfit()
        {
            return Revenue + GovernmentFunding - Costs;
        }
    }
}