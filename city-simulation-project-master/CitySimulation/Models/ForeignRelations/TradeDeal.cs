using CitySimulation.Enums;
using CitySimulation.Models.Base;

namespace CitySimulation.Models.ForeignRelations
{
    public class TradeDeal : ObservableObject
    {
        private ResourceType _resource;
        private int _quantity;
        private decimal _pricePerUnit;

        public ResourceType Resource
        {
            get => _resource;
            set => SetProperty(ref _resource, value);
        }

        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public decimal PricePerUnit
        {
            get => _pricePerUnit;
            set => SetProperty(ref _pricePerUnit, value);
        }

        public decimal TotalCost => Quantity * PricePerUnit;
    }
}