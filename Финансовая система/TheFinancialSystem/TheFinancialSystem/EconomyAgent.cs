using System;
using System.Collections.Generic;
using System.Transactions;

namespace TheFinancialSystem
{
    public abstract class EconomicAgent
    {
        public Guid Id { get; }
        public string Name { get; }
        public decimal Budget { get; private set; }
        public List<FinancialTransaction> TransactionHistory { get; } = new List<FinancialTransaction>();

        public event EventHandler<TheFinancialSystem.FinancialTransaction>? OnTransactionOccurred;

        protected EconomicAgent(string name, decimal initialBudget = 0)
        {
            Id = Guid.NewGuid();
            Name = name;
            Budget = initialBudget;
        }

        public void ReceiveMoney(decimal amount, string reason, EconomicAgent? from = null)
        {
            if (amount <= 0) return;
            Budget += amount;
            var transaction = new FinancialTransaction(from, this, amount, reason);
            TransactionHistory.Add(transaction);
            OnTransactionOccurred?.Invoke(this, transaction);
        }

        public bool SpendMoney(decimal amount, string reason, EconomicAgent? to = null)
        {
            if (amount <= 0 || Budget < amount)
            {
                return false; // Не хватает средств
            }

            Budget -= amount;
            var transaction = new FinancialTransaction(this, to, amount, reason);
            TransactionHistory.Add(transaction);
            OnTransactionOccurred?.Invoke(this, transaction);
            return true;
        }
    }
}