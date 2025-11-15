using CitySimulation.Enums;

namespace CitySimulation.Models.ForeignRelations
{
    public class Export : TradeDeal
    {
        private decimal _customsDuty;

        public decimal CustomsDuty
        {
            get => _customsDuty;
            set => SetProperty(ref _customsDuty, value);
        }

        public decimal NetProfit => TotalCost - CustomsDuty;
    }
}