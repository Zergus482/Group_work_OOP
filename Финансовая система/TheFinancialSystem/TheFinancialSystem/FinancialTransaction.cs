using System;
using TheFinancialSystem;

namespace TheFinancialSystem
{
    public readonly struct FinancialTransaction
    {
        public Guid Id { get; }
        public DateTime Timestamp { get; }
        public EconomicAgent? Sender { get; }
        public EconomicAgent? Receiver { get; }
        public decimal Amount { get; }
        public string Description { get; }

        public FinancialTransaction(EconomicAgent? sender, EconomicAgent? receiver, decimal amount, string description)
        {
            Id = Guid.NewGuid();
            Sender = sender;
            Receiver = receiver;
            Amount = amount;
            Description = description;
        }

        public override string ToString() => $"{Sender?.Name ?? "Внешний источник"} -> {Receiver?.Name ?? "Внешний получатель"} | {Amount:C} | {Description}";
    }
}