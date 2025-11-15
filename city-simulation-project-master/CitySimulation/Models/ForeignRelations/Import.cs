using CitySimulation.Enums;

namespace CitySimulation.Models.ForeignRelations
{
    public class Import : TradeDeal
    {
        private decimal _importTax;

        public decimal ImportTax
        {
            get => _importTax;
            set => SetProperty(ref _importTax, value);
        }

        public decimal TotalCostWithTax => TotalCost + ImportTax;
    }
}