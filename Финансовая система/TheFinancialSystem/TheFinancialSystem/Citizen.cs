using TheFinancialSystem;

namespace TheFinancialSystem
{
    public class Citizen : EconomicAgent
    {
        public decimal Salary { get; set; }
        public Company? Employer { get; set; }

        public Citizen(string name, decimal initialBudget) : base(name, initialBudget) { }
    }
}